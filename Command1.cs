using System;
using System.Linq;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace XmlToCSharp
{
    /// <summary>
    /// TestDialogWindow handler
    /// </summary>

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        public string[] genericNamedEles = { "Page", "Navigation", "TaxonomyNavigation", "Navigation" };
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7adc4e15-b473-4fd4-a817-15b4d09af0c7");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command1(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.StartNotepad, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command1 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command1(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void StartNotepad(object sender, EventArgs e)
        {
            string inputText = System.Windows.Forms.Clipboard.GetText();
            System.Windows.Forms.Clipboard.SetText(TransformXML(inputText));
        }

        private string TransformXML(string xml)
        {
            List<string> csLineList = new List<string>();
            string elementName;
            foreach (Match tag in Regex.Matches(xml, "<[^/]*>"))
            {
                elementName = "";
                try
                {
                    string csType = "";
                    string getEleMethod;
                    elementName = tag.Value.Substring(1, tag.Value.IndexOf(' ') - 1);
                    string elementType = Regex.Match(tag.Value, "type=\"([^\"]*)\"").Groups[1].Value;
                    string elementAttrName = Regex.Match(tag.Value, "Name=\"([^\"]*)\"").Groups[1].Value;
                    csType = GetCsTypeFromXmlType(elementType, elementName, out getEleMethod);
                    elementName = genericNamedEles.Contains(elementName) ? elementAttrName : elementName;
                    string variableName = !string.IsNullOrWhiteSpace(elementName) && char.IsUpper(elementName, 0) ? elementName.Replace(elementName[0], char.ToLower(elementName[0])) : elementName;
                    variableName = FixVariableName(variableName);

                    csLineList.Add(csType + " " + variableName + " = Model." + getEleMethod + "(\"" + elementName + "\");");
                }
                catch
                {
                    if (elementName != "")
                    {
                        csLineList.Add("Error on Element: " + elementName);
                    }
                    else
                    {
                        csLineList.Add("Error on Element #: " + tag.Index);
                    }
                }
            }


            return string.Join(Environment.NewLine, csLineList);
        }

        private string GetCsTypeFromXmlType(string elementType, string elementName, out string elementGetMethod)
        {
            string csType = "";
            if (genericNamedEles.Contains(elementName))
            {
                switch (elementName)
                {
                    case "Page":
                        csType = "ICMSLinkItem";
                        elementGetMethod = "GetLinkItem";
                        break;
                    case "Navigation":
                        csType = "ICMSNavigationElement";
                        elementGetMethod = "GetNavigation";
                        break;
                    case "TaxonomyNavigation":
                        csType = "ICMSTaxonomyNavigationElement";
                        elementGetMethod = "GetTaxonomyNavigation";
                        break;
                    default:
                        csType = "ICMSElement";
                        elementGetMethod = "Element";
                        break;
                }
            }
            else
            {
                if (elementType == "string")
                {
                    csType = "ICMSElement";
                    elementGetMethod = "Element";
                }

                else
                {
                    csType = "ICMSElement";
                    elementGetMethod = "Element";
                }
            }
            return csType;
        }

        private string FixVariableName(string name)
        {
            switch (name)
            {
                case "abstract":
                    name = "abstractText";
                    break;
                default:
                    break;
            }

            return name;
        }


    }

    //class MyWindow : DialogWindow
    //{
    //    internal MyWindow(string inputText)
    //    {
    //        this.AddText(inputText);
    //        //this.SizeToContent = SizeToContent.Height;
    //        this.Height = 500;
    //        this.ForceCursor = true;
    //        this.Focusable = true;
    //        this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy));
    //        this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll));
    //        this.IsManipulationEnabled = true;

    //    }
    //}
}
