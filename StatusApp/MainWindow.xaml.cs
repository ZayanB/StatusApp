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
        public int CreatedFileCount = 0;
        public int CreatedFolderCount = 0;

        private static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string SourceFolderName = "Source";
        private static readonly string RollbackFile = "Rollback Log.txt";

        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config.json");

        private static string ApplicationChoice;

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;
        private DeploymentMethods deploymentMethods = new DeploymentMethods();
        private dynamic FolderPaths;


        public MainWindow()
        {
            try
            {
                InitializeComponent();

                if (File.Exists(ConfigFilePath))
                {
                    ConfigManager.LoadConfig(ConfigFilePath);

                    LoadApplicationOptions();

                    IsAppLoaded = true;

                    this.Loaded += MainWindow_Loaded;
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
            var applicationOptions = ConfigManager.Config.Applications.Keys.ToList();
            applicationDropdown.ItemsSource = applicationOptions;
            applicationDropdown.SelectedIndex = 0;
        }

        //method to dynamically update ui and check folders whenever application is changed
        private void applicationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationChoice = applicationDropdown.SelectedItem.ToString();
            SkipInitialChange = false;

            FolderPaths = ConfigManager.Config.Applications[ApplicationChoice];

            deploymentMethods.ClearLabels(txtCopyCount, txtBackupCount, txtReplacedCount);
            SourceFolderLabel.Content = FolderPaths.sourceFolder;
            BackupFolderLabel.Content = FolderPaths.backupFolder;
            deploymentMethods.AddDestinationLabels(FolderPaths, DestinationLabelsPanel);


            bool checkFolders = deploymentMethods.CheckFolders(FolderPaths);
            if (!checkFolders) { Application.Current.Shutdown(); }

            if (IsAppLoaded) { deploymentMethods.CleanupBackups(FolderPaths, ApplicationChoice); }
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (deploymentMethods.CheckSourceFolder(FolderPaths))
                if (!deploymentMethods.IsDirectoryEmpty(FolderPaths.sourceFolder))
                {
                    DateTime timestamp = deploymentMethods.CreateBackupInstance(FolderPaths);

                    BackupDestination(timestamp);

                    deploymentMethods.CopySourceToDestination(FolderPaths, txtCopyCount, ref CreatedFolderCount, ref CreatedFileCount);

                    deploymentMethods.CreateBackupSource(FolderPaths, timestamp);

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
            string backupPath = deploymentMethods.GetBackupName(FolderPaths, backupStamp);

            string destinationBackupFolder = Path.Combine(backupPath, DestinationFolderName);

            //Compare source with all destinations to check for common files

            string sourcePath = FolderPaths.sourceFolder;

            foreach (var destination in FolderPaths.destinationFolders)
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
                    BackupItems(destinationPath, specificBackupFolder, commonFiles, backupStamp);
                }
                else
                {
                    txtBackupCount.Content = $" No common files between {SourceFolderName} and {DestinationFolderName} to backup. Backed up only {SourceFolderName}";
                    txtReplacedCount.Content = $"Nothing to replace between {SourceFolderName} and {DestinationFolderName}";
                }
            }

        }

        private void BackupItems(string sourceDir, string destDir, List<string> commonFiles, DateTime backupStamp)
        {
            string backupPath = deploymentMethods.GetBackupName(FolderPaths, backupStamp);

            string backupDateTime = "Backup " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n------------------------------------------------------------------------------------------------------------------\n\n");
            }
            foreach (var item in commonFiles)
            {
                deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, ref BackupFolderCount, ref BackupFileCount);
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
                deploymentMethods.Rollback(FolderPaths, rollbackPath);
            }

        }

        //drodown menu methods

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;

            deploymentMethods.LoadBackupOptions(FolderPaths, BackupDropdown, rollbackBtn);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SkipInitialChange)
            {
                deploymentMethods.CleanupBackups(FolderPaths, ApplicationChoice);
            }
        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window2 secondWindow = new Window2();
            secondWindow.Show();
        }
    }
}
