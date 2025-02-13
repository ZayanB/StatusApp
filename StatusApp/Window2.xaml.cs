using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.Json;




namespace StatusApp
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config2.json");

        private static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string RollbackFile = "Rollback Log.txt";


        private int BackupFolderCount = 0;
        private int BackupFileCount = 0;
        public int CreatedFileCount = 0;
        public int CreatedFolderCount = 0;

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;

        private static string ApplicationChoice;

        private DeploymentMethods deploymentMethods = new DeploymentMethods();
        private dynamic FolderPaths;

        public Config2 ConfigData { get; set; }
        public Window2()
        {
            try
            {
                InitializeComponent();

                if (File.Exists(ConfigFilePath))
                {
                    ConfigManager.LoadConfig(ConfigFilePath);
                    LoadApplicationOptions();
                    IsAppLoaded = true;
                    this.Loaded += Window2_Loaded;
                }
                else
                {
                    MessageBox.Show($"Configuration File not found at {ConfigFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadApplicationOptions()
        {
            var applicationOptions = ConfigManager.Config.Applications.Keys.ToList();
            applicationDropdown.ItemsSource = applicationOptions;
            applicationDropdown.SelectedIndex = 0;
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!deploymentMethods.IsDirectoryEmpty(FolderPaths.sourceFolder))
                {
                    DateTime timestamp = deploymentMethods.CreateBackupInstance(FolderPaths);

                    BackupDestination(timestamp);

                    KeepWantedItems();

                    deploymentMethods.CopySourceToDestination(FolderPaths, txtCopyCount, ref CreatedFolderCount, ref CreatedFileCount);

                    //deploymentMethods.CreateBackupSource(FolderPaths, timestamp);

                    BackupFolderCount = 0;
                    BackupFileCount = 0;
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

        private void BackupDestination(DateTime timestamp)
        {
            string backupPath = deploymentMethods.GetBackupName(FolderPaths, timestamp);

            string destinationBackupFolder = Path.Combine(backupPath, DestinationFolderName);

            foreach (var destination in FolderPaths.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationBackupFolder))
                {
                    Directory.CreateDirectory(destinationBackupFolder);
                }
                string specificBackupFolder = Path.Combine(destinationBackupFolder, destination.name);
                if (!Directory.Exists(specificBackupFolder))
                {
                    Directory.CreateDirectory(specificBackupFolder);
                }
                BackupAllItems(destinationPath, specificBackupFolder, timestamp);
            }
        }

        private void KeepWantedItems()
        {
            foreach (var destination in FolderPaths.destinationFolders)
            {
                string destinationPath = destination.path;
                List<string> unwantedItems = GetItemsToDelete(destinationPath);

                DeleteItems(unwantedItems);

            }
        }

        private List<string> GetItemsToDelete(string directoryPath)
        {
            List<string> unwantedItems = new List<string>();

            List<string> filesToKeep = FolderPaths.filesToKeep;
            List<string> foldersTokeep = FolderPaths.foldersToKeep;

            var allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);

            var allDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in allFiles)
            {
                string fileExtension = Path.GetExtension(file).TrimStart('.').ToLower();
                if (!filesToKeep.Contains(fileExtension))
                {
                    unwantedItems.Add(file);
                }
            }

            foreach (var directory in allDirectories)
            {
                string folderName = new DirectoryInfo(directory).Name;
                if (!foldersTokeep.Contains(folderName))
                {
                    unwantedItems.Add(directory);
                }
            }

            return unwantedItems;
        }

        private void DeleteItems(List<string> itemsToDelete)
        {
            foreach (var item in itemsToDelete)
            {
                try
                {
                    if (File.Exists(item))
                    {
                        File.Delete(item);
                    }
                    else if (Directory.Exists(item))
                    {
                        Directory.Delete(item, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting {item}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private void BackupAllItems(string sourceDir, string destDir, DateTime backupStamp)
        {
            string backupPath = deploymentMethods.GetBackupName(FolderPaths, backupStamp);

            string backupDateTime = "Backup " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n------------------------------------------------------------------------------------------------------------------\n\n");
            }

            foreach (var destination in FolderPaths.destinationFolders)
            {
                string destinationPath = destination.path;

                List<string> allDestinationItems = Directory.EnumerateFileSystemEntries(destinationPath, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(destinationPath, path)).ToList();

                foreach (var item in allDestinationItems)
                {
                    deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, ref BackupFolderCount, ref BackupFileCount);
                }
            }
            txtBackupCount.Content = $" Backed Up {BackupFolderCount} Folders & {BackupFileCount} Files ";

        }

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;

            deploymentMethods.LoadBackupOptions(FolderPaths, BackupDropdown, performRollbackBtn);
        }

        private void performRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string rollbackPath = BackupDropdown.SelectedValue.ToString();

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {Path.GetFileName(rollbackPath)} back to {DestinationFolderName}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                deploymentMethods.Rollback(FolderPaths, rollbackPath);
            }
        }

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

        private void Window2_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SkipInitialChange)
            {
                deploymentMethods.CleanupBackups(FolderPaths, ApplicationChoice);
            }
        }
    }
}
