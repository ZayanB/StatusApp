using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace StatusApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int BackupFolderCount = 0;
        private int BackupFileCount = 0;
        private int ReplacedFolderCount = 0;
        private int ReplacedFileCount = 0;
        private int CreatedFileCount = 0;
        private int CreatedFolderCount = 0;

        private static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string SourceFolderName = "Source";
        private static readonly string RollbackFile = "Rollback Log.txt";

        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config.json");
        //private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config3.json");

        private static string ApplicationChoice;

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;
        private DeploymentMethods deploymentMethods = new DeploymentMethods();
        private ConfigManager configManager = new ConfigManager();
        private dynamic ConfigData;
        private DeployWithDelete window2 = new DeployWithDelete();

        public static bool isTabClicked { get; set; }

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                var deploymentWithDeleteWindow = new DeployWithDelete();
                DeploymentWithDeleteFrame.Content = deploymentWithDeleteWindow.Content;

                if (File.Exists(ConfigFilePath))
                {
                    configManager.LoadConfig(ConfigFilePath);

                    LoadApplicationOptions();

                    IsAppLoaded = true;

                    this.Loaded += MainWindow_Loaded;

                    isTabClicked = false;
                }
                else
                {
                    MessageBox.Show($"Config file not found at {ConfigFilePath}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //method to load applications to dropdown
        private void LoadApplicationOptions()
        {
            var applicationOptions = configManager.Config.Applications.Keys.ToList();
            applicationDropdown.ItemsSource = applicationOptions;
            applicationDropdown.SelectedIndex = 0;
        }

        //method to dynamically update ui and check folders whenever application is changed
        private void applicationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationChoice = applicationDropdown.SelectedItem.ToString();

            SkipInitialChange = false;

            ConfigData = configManager.Config.Applications[ApplicationChoice];

            deploymentMethods.ClearLabels(txtCopyCount, txtBackupCount, txtReplacedCount);
            SourceFolderLabel.Content = ConfigData.sourceFolder;
            BackupFolderLabel.Content = ConfigData.backupFolder;
            deploymentMethods.AddDestinationLabels(ConfigData.destinationFolders, DestinationLabelsPanel);


            bool checkFolders = deploymentMethods.CheckFolders(ConfigData.sourceFolder, ConfigData.backupFolder, ConfigData.destinationFolders);
            if (!checkFolders) { Application.Current.Shutdown(); }

            if (IsAppLoaded) { deploymentMethods.CleanupBackups(ConfigData.backupFolder, ConfigData.keepBackupsCount, ApplicationChoice); }
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!deploymentMethods.IsDirectoryEmpty(ConfigData.sourceFolder))
                {
                    DateTime timestamp = deploymentMethods.CreateBackupInstance(ConfigData.backupFolder);

                    BackupDestination(timestamp);

                    var copyCounts = deploymentMethods.CopySourceToDestination(ConfigData.sourceFolder, ConfigData.destinationFolders);

                    CreatedFileCount = copyCounts.Item2;
                    CreatedFolderCount = copyCounts.Item1;

                    string labelContent = CreatedFileCount > 0 || CreatedFolderCount > 0 ? $" Created {CreatedFolderCount} Folders & {CreatedFileCount} Files" : " All files are similar. No new files to create ";

                    txtCopyCount.Content = labelContent;

                    //deploymentMethods.CreateBackupSource(ConfigData.sourceFolder, ConfigData.backupFolder, timestamp); //Comment to not empty source

                    BackupFolderCount = 0;
                    BackupFileCount = 0;
                    ReplacedFolderCount = 0;
                    ReplacedFileCount = 0;
                    CreatedFolderCount = 0;
                    CreatedFileCount = 0;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed. {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        //methods to backup destination if same as source & create backup log
        private void BackupDestination(DateTime backupStamp)
        {
            string backupPath = deploymentMethods.GetBackupPath(ConfigData.backupFolder, backupStamp);

            string destinationBackupFolder = Path.Combine(backupPath, DestinationFolderName);

            string sourcePath = ConfigData.sourceFolder;

            bool isFirstIteration = true;

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                var commonFiles = deploymentMethods.CompareDirectoryPath(sourcePath, destinationPath);

                if (commonFiles.Count > 0)
                {
                    if (!Directory.Exists(destinationBackupFolder))
                    {
                        Directory.CreateDirectory(destinationBackupFolder);
                    }
                    string specificBackupFolder = Path.Combine(destinationBackupFolder, destination.name);
                    if (!Directory.Exists(specificBackupFolder))
                    {
                        Directory.CreateDirectory(specificBackupFolder);
                    }
                    BackupItems(destinationPath, specificBackupFolder, commonFiles, backupStamp, isFirstIteration);

                    isFirstIteration = false;
                }
                else
                {
                    txtBackupCount.Content = $" No common files between {SourceFolderName} and {DestinationFolderName} to backup. Backed up only {SourceFolderName}";
                    txtReplacedCount.Content = $"Nothing to replace between {SourceFolderName} and {DestinationFolderName}";
                }
            }

        }

        private void BackupItems(string sourceDir, string destDir, List<string> commonFiles, DateTime backupStamp, bool isFirstIteration)
        {
            string backupPath = deploymentMethods.GetBackupPath(ConfigData.backupFolder, backupStamp);

            string backupDateTime = "Backup " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n------------------------------------------------------------------------------------------------------------------\n\n");
            }
            foreach (var item in commonFiles)
            {
                var backupResult = deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, isFirstIteration);
                if (isFirstIteration)
                {
                    BackupFolderCount += backupResult.Item1;
                    BackupFileCount += backupResult.Item2;
                }
            }
            ReplacedFileCount = BackupFileCount;
            ReplacedFolderCount = BackupFolderCount;
            txtBackupCount.Content = $" Backed Up {BackupFolderCount} Folders & {BackupFileCount} Files ";
            txtReplacedCount.Content = $" Replaced {ReplacedFolderCount} Folders & {ReplacedFileCount} Files";
        }

        private void rollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string rollbackPath = BackupDropdown.SelectedValue.ToString();

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {Path.GetFileName(rollbackPath)} back to {DestinationFolderName}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                //deploymentMethods.Rollback(ConfigData, rollbackPath);
                deploymentMethods.Rollback(ConfigData.backupFolder, rollbackPath, ConfigData.destinationFolders);
            }

        }

        //drodown menu methods

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;

            deploymentMethods.LoadBackupOptions(ConfigData.backupFolder, BackupDropdown, rollbackBtn);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SkipInitialChange)
            {
                deploymentMethods.CleanupBackups(ConfigData.backupFolder, ConfigData.keepBackupsCount, ApplicationChoice);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Header.ToString() == "Deployment With Delete")
                {
                    window2.PerformCleanUpForDeployDelete();
                    isTabClicked = true;
                }
            }
        }
    }
}
