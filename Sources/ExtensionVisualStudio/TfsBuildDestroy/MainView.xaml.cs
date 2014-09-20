using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;


namespace TfsBuildDestroy
{
    /// <summary>
    ///     Interaction logic for MainView
    /// </summary>
    public partial class MainView : UserControl
    {

        private IBuildDefinition _builddef;
        private IBuildServer bs;
        private string IdePath;
        private ITeamFoundationContext _teamFoundationContext;
        private ITeamExplorer itfs;
        public TfsTeamProjectCollection tfs
        {
            get { return _teamFoundationContext.TeamProjectCollection; }
        }

        private IBuildDefinition[] BuildDefinitions
        {
            get { return bs.QueryBuildDefinitions(_teamFoundationContext.TeamProjectName); }
        }

        public MainView(ITeamFoundationContext tfsContext, ITeamExplorer iTeamExplorer)
        {
            InitializeComponent();
            _teamFoundationContext = tfsContext;
            itfs = iTeamExplorer;

            bs = tfs.GetService<IBuildServer>();

            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte != null)
            {
                var fi = new FileInfo(dte.FileName);
                IdePath = fi.DirectoryName;
            }

            LoadcbBuildDef();
        }


        private void LoadcbBuildDef()
        {
            cbBuildDef.ItemsSource = BuildDefinitions;
            cbBuildDef.DisplayMemberPath = "Name";
            cbBuildDef.SelectedValuePath = "Uri";
            cbBuildDef.SelectedIndex = 0;
        }

        private void cbBuildDef_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCbBuilds(sender);
        }

        private void LoadCbBuilds(object sender)
        {
            cbBuilds.Items.Clear();


            // bdef is the selected build definition
            var bdef = (((ComboBox) sender).SelectedItem) as IBuildDefinition;

            _builddef = bdef;
            if (bdef != null)
            {
                // _bs = IBuildServer service, create a new query
                IBuildDetailSpec def = bs.CreateBuildDetailSpec(_teamFoundationContext.TeamProjectName);
                // only bring back the last 100 deleted builds
                def.MaxBuildsPerDefinition = 100;
                // query for only deleted builds
                def.QueryDeletedOption = QueryDeletedOption.OnlyDeleted;
                // Last deleted should be returned 1st
                def.QueryOrder = BuildQueryOrder.FinishTimeDescending;
                // Only look for deleted builds in the chosen build definition
                def.DefinitionSpec.Name = bdef.Name;
                // Bring back deleted builds from any state
                def.Status = BuildStatus.All;
                // Pass this query for processing to the build service
                IBuildDetail[] builds = tfs.GetService<IBuildServer>().QueryBuilds(def).Builds;


                foreach (IBuildDetail build in builds)
                {
                    cbBuilds.Items.Add(build.BuildNumber);
                }

                cbBuilds.IsEnabled = true;
            }
        }


        private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (cbBuilds.SelectedItem != null)
            {
                string builddefinition = _builddef.FullPath.Substring(1).Replace("\\", @"/");


                MessageBoxResult messageBoxResult = MessageBox.Show(
                    "You are sur to delete the build : " + cbBuilds.SelectedItem, "Destroy confirmation?",
                    MessageBoxButton.YesNo);



                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    FileInfo tfsbuildfile = new FileInfo(String.Concat(IdePath, "\\tfsbuild.exe"));
                    if (tfsbuildfile.Exists)
                    {
                        string ExecutableFilePath = tfsbuildfile.FullName;
                        string Arguments =
                            string.Format(
                                @"destroy /collection:{0} /builddefinition:""{1}"" ""{2}""",
                                tfs.Uri, builddefinition, cbBuilds.SelectedItem);


                        //Create Process Start information
                        var processStartInfo = new ProcessStartInfo(ExecutableFilePath, Arguments);
                        processStartInfo.ErrorDialog = false;
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.RedirectStandardError = true;
                        processStartInfo.RedirectStandardInput = true;
                        processStartInfo.RedirectStandardOutput = true;
                        processStartInfo.CreateNoWindow = true;


                        //Execute the process
                        var process = new Process();
                        process.StartInfo = processStartInfo;

                        bool processStarted = process.Start();
                        if (processStarted)
                        {
                            StreamReader outputReader = null;
                            StreamReader errorReader = null;

                            //Get the output stream
                            outputReader = process.StandardOutput;
                            errorReader = process.StandardError;
                            process.WaitForExit();

                            //Display the result

                            string displayText = string.Empty;


                            //if error display error output
                            if (!string.IsNullOrEmpty(errorReader.ReadToEnd()))
                            {
                                displayText += Environment.NewLine + "Error" + Environment.NewLine + "==============" +
                                               Environment.NewLine;
                                displayText += errorReader.ReadToEnd();
                            }
                            else
                            {
                                displayText = string.Concat("The build ", cbBuilds.SelectedItem,
                                    " was deleted successfully");

                                //refrech list
                                LoadCbBuilds(cbBuildDef);

                                //disable btn delete
                                btnDelete.IsEnabled = false;
                            }
                            ShowNotification(displayText, NotificationType.Information);
                        }
                    }
                    else
                    {
                        ShowNotification("The tfsbuild.exe command not found", NotificationType.Error);
                    }
                }
            }
            else
            {
                ShowNotification("Choose a build number !!", NotificationType.Error);
            }
        }

        private void CbBuilds_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnDelete.IsEnabled = true;
        }


        protected Guid ShowNotification(string message, NotificationType type)
        {
            if (itfs != null)
            {
                Guid guid = Guid.NewGuid();
                itfs.ShowNotification(message, type, NotificationFlags.All, null, guid);

                return guid;
            }

            return Guid.Empty;
        }

    }
}