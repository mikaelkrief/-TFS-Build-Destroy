using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsBuildDestroy
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IBuildServer _bs;
        private string _selectedTeamProject;
        private TfsTeamProjectCollection _tfs;
        private VersionControlServer _vcs;
        private IBuildDefinition _builddef;
        private string _tfsCollectionUri;

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool ConnectToTfs()
        {
            bool isSelected = false;
            var tfsPp = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
            tfsPp.ShowDialog();
            _tfs = tfsPp.SelectedTeamProjectCollection;

            _tfsCollectionUri = _tfs.Uri.ToString();

            if (tfsPp.SelectedProjects.Count() > 0)
            {
                _selectedTeamProject = tfsPp.SelectedProjects[0].Name;
                isSelected = true;
            }
            return isSelected;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectToTfs())
            {
                cbBuildDef.IsEnabled = true;
                _vcs = _tfs.GetService<VersionControlServer>();
                cbBuildDef.ItemsSource = GetAllBuildDefinitionsFromTheTeamProject();
                cbBuildDef.DisplayMemberPath = "Name";
                cbBuildDef.SelectedValuePath = "Uri";
                cbBuildDef.SelectedIndex = 0;
            }
        }

        // Grab all build definitions
        private IBuildDefinition[] GetAllBuildDefinitionsFromTheTeamProject()
        {
            _bs = _tfs.GetService<IBuildServer>();
            return _bs.QueryBuildDefinitions(_selectedTeamProject);
        }

        // Get All Deleted Builds for the selected build definition 
        private void cbBuildDef_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbBuilds.Items.Clear();


            // bdef is the selected build definition
            var bdef = (((ComboBox) sender).SelectedItem) as IBuildDefinition;
            _builddef = bdef;
            if (bdef != null)
            {
                // _bs = IBuildServer service, create a new query
                IBuildDetailSpec def = _bs.CreateBuildDetailSpec(_selectedTeamProject);
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
                IBuildDetail[] builds = _bs.QueryBuilds(def).Builds;


                foreach (IBuildDetail build in builds)
                {
                    cbBuilds.Items.Add(build.BuildNumber);
                }

                cbBuilds.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
        }


        private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (cbBuilds.SelectedItem != null)
            {
                string builddefinition = _builddef.FullPath.Substring(1).Replace("\\", @"/");


                MessageBoxResult messageBoxResult = MessageBox.Show(
                    "You want to delete the build : " + cbBuilds.SelectedItem, "Destroy confirmation?",
                    MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    

                    string ExecutableFilePath =
                        @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\tfsbuild.exe";
                    string Arguments =
                        string.Format(
                            @"destroy /collection:{0} /builddefinition:""{1}"" ""{2}""",
                            _tfsCollectionUri,builddefinition, cbBuilds.SelectedItem);


                    //Create Process Start information
                    var processStartInfo =
                        new ProcessStartInfo(ExecutableFilePath, Arguments);
                    processStartInfo.ErrorDialog = false;
                    processStartInfo.UseShellExecute = false;
                    processStartInfo.RedirectStandardError = true;
                    processStartInfo.RedirectStandardInput = true;
                    processStartInfo.RedirectStandardOutput = true;

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
                        string displayText = "Output" + Environment.NewLine + "==============" + Environment.NewLine;
                        displayText += outputReader.ReadToEnd();
                        //if error display error output
                        if (!string.IsNullOrEmpty(errorReader.ReadToEnd()))
                        {
                            displayText += Environment.NewLine + "Error" + Environment.NewLine + "==============" +
                                           Environment.NewLine;
                            displayText += errorReader.ReadToEnd();
                        }
                        MessageBox.Show(displayText);
                    }
                }
            }
            else
            {
                MessageBox.Show("Choose a build number !!");
            }
        }

    }
}