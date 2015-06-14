/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using StoredProcScaf;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.Samples.VisualStudio.MenuCommands
{
    /// <summary>
    /// This is the class that implements the package. This is the class that Visual Studio will create
    /// when one of the commands will be selected by the user, and so it can be considered the main
    /// entry point for the integration with the IDE.
    /// Notice that this implementation derives from Microsoft.VisualStudio.Shell.Package that is the
    /// basic implementation of a package provided by the Managed Package Framework (MPF).
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]

    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidsList.guidMenuAndCommandsPkg_string)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ComVisible(true)]
    public sealed class MenuCommandsPackage : Package
    {


        /// <summary>
        /// Initialization of the package; this is the place where you can put all the initialization
        /// code that relies on services provided by Visual Studio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Now get the OleCommandService object provided by the MPF; this object is the one
            // responsible for handling the collection of commands implemented by the package.
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Now create one object derived from MenuCommand for each command defined in
                // the VSCT file and add it to the command service.

                // For each command we have to define its id that is a unique Guid/integer pair.
                var id = new CommandID(GuidsList.guidMenuAndCommandsCmdSet, PkgCmdIDList.cmdidMyCommand);
                // Now create the OleMenuCommand object for this command. The EventHandler object is the
                // function that will be called when the user will select the command.
                var command = new OleMenuCommand(MenuCommandCallback, id);
                command.BeforeQueryStatus += command_BeforeQueryStatus;

                // Add the command to the command service.
                mcs.AddCommand(command);
            }
        }

        void command_BeforeQueryStatus(object sender, EventArgs e)
        {
            var dte = (DTE)GetGlobalService(typeof(DTE));

            var menuCommand = sender as OleMenuCommand;

            if (menuCommand != null)
            {
                menuCommand.Visible = menuCommand.Enabled = false;
                var activeDocumentName = dte.ActiveDocument.Name;
                var activeDocumentFullPath = dte.ActiveDocument.FullName;
                var activeDocumentPath = dte.ActiveDocument.Path;

                var dbInspector =
                    new DBObjectInspector(new TargetInfo(activeDocumentName, activeDocumentPath, activeDocumentFullPath));
                menuCommand.Visible = menuCommand.Enabled = dbInspector.CanScaffoldObject();
            }
        }

        #region Commands Actions


        /// <summary>
        /// Event handler called when the user selects the Sample command.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.MenuCommands.MenuCommandsPackage.OutputCommandString(System.String)")]
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            var dte = (DTE)GetGlobalService(typeof(DTE));
            var dataConnectionManager = (IVsDataExplorerConnectionManager)GetService(typeof(IVsDataExplorerConnectionManager));
             

            if (dataConnectionManager != null)
            {
                var connections = dataConnectionManager.Connections;

                var connectionNode = connections.First().Value.SelectedNodes;
                var connectionString = connections.First().Value.Connection.DisplayConnectionString;
            }
            var activeDocumentName = dte.ActiveDocument.Name;
            var activeDocumentFullPath = dte.ActiveDocument.FullName;
            var activeDocumentPath = dte.ActiveDocument.Path;

            var dbInspector = new DBObjectInspector(new TargetInfo(activeDocumentName, activeDocumentPath, activeDocumentFullPath));
            if (dbInspector.CanScaffoldObject())
            {
                dbInspector.Generate();
                MessageBox.Show(VSPackage.MenuCommandsPackage_MenuCommandCallback_Scaffolding_successfuly_generated, 
                    VSPackage.MenuCommandsPackage_MenuCommandCallback_Scaffold, 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }



        #endregion
    }

    public class TypeHelper2
    {
        private readonly DTE _dte;

        public TypeHelper2(DTE dte)
        {
            _dte = dte;
        }

        public IEnumerable<string> FindSolutionTypes()
        {
            List<string> types = new List<string>();


            foreach (Project project in _dte.Solution.Projects)
            {
                
                if (project.CodeModel != null && project.CodeModel.CodeElements != null)
                    GetSolutionTypes(project.CodeModel.CodeElements, types);
            }

            return types;
        }

        private void GetSolutionTypes(CodeElements codeElements, List<string> types)
        {

            foreach (CodeElement codeElement in codeElements)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                {
                    var members = ((CodeNamespace)codeElement).Members;
                    if (members != null)
                        GetSolutionTypes(members, types);
                }
                if (codeElement.IsCodeType)
                {
                    var element = codeElement as CodeClass;
                   
                    if (element != null)
                    {
                        var codeClass = element;
                        if (codeClass.Access == vsCMAccess.vsCMAccessPublic)
                        {
                            var members = element.Members;
                            try
                            {
                                
                                if (codeClass.ProjectItem != null && codeClass.ProjectItem.ContainingProject != null)
                                    types.Add(codeClass.FullName);
                            }
                            finally 
                            {
                                GetSolutionTypes(members, types);
                            }
                            
                        }
                        try
                        {
                            if (codeClass.ProjectItem != null && codeClass.ProjectItem.ContainingProject != null)
                                types.Add(codeClass.FullName);
                        }
                        finally { }
                    }
                }
            }
        }

    }
}
