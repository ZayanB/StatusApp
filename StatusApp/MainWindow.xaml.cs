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

        private static string ApplicationChoice;

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;

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

            ClearLabels();
            SourceFolderLabel.Content = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;
            BackupFolderLabel.Content = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;
            AddDestinationLabels();

            if (!CheckFolders()) { Application.Current.Shutdown(); }

            if (IsAppLoaded) { CleanupBackups(); }
        }

        //method to check folders
        private bool CheckFolders()
        {
            string sourcePath = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;

            if (!Directory.Exists(sourcePath))
            {
                MessageBox.Show($"{SourceFolderName} for {ApplicationChoice} is not existing at: {sourcePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string backupFolder = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;

            if (!Directory.Exists(backupFolder))
            {
                MessageBox.Show($"{BackupFolderName} for {ApplicationChoice} is not existing at: {backupFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            foreach (var destination in ConfigManager.Config.Applications[ApplicationChoice].destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    MessageBox.Show($"{DestinationFolderName} for {ApplicationChoice} is not existing at: {destinationPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        // method to check source folder
        private bool CheckSourceFolder()
        {
            string sourcePath = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;
            if (!Directory.EnumerateFileSystemEntries(sourcePath).Any())
            {
                MessageBox.Show($"{SourceFolderName} is Empty. Operation can't be completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        //run button method
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckSourceFolder())
                {
                    DateTime timestamp = CreateBackupInstance();

                    BackupDestination(timestamp);

                    CopySourceToDestinations();

                    //CreateBackupSource(timestamp);

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

        //method to create backup folder with timestamp
        private DateTime CreateBackupInstance()
        {
            string backupFolder = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;

            DateTime currentTime = DateTime.Now;
            string backupFolderSub = "Backup_" + currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolderPath = Path.Combine(backupFolder, backupFolderSub);

            Directory.CreateDirectory(backupFolderPath);

            return currentTime;
        }

        //method to compare source and destination
        private List<string> CompareDirectoryPath(string path1, string path2)
        {
            List<string> itemsInPath1 = Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)).ToList();

            List<string> itemsInPath2 = Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)).ToList();

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            return commonItems;
        }

        //method to get backup folder name
        private string GetBackupName(DateTime backupStamp)
        {
            string backupFolderPath = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;
            string backupPath = Path.Combine(backupFolderPath, BackupFolderName + backupStamp.ToString("_yyyy-MM-dd_HH-mm-ss"));
            return backupPath;
        }

        //methods to backup destination if same as source & create backup log
        private void BackupDestination(DateTime backupStamp)
        {
            string backupPath = GetBackupName(backupStamp);

            string destinationBackupFolder = Path.Combine(backupPath, DestinationFolderName);

            //Compare source with all destinations to check for common files

            string sourcePath = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;

            foreach (var destination in ConfigManager.Config.Applications[ApplicationChoice].destinationFolders)
            {
                string destinationPath = destination.path;

                var commonFiles = CompareDirectoryPath(sourcePath, destinationPath);

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
            string backupPath = GetBackupName(backupStamp);
            string backupDateTime = "Backup " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n------------------------------------------------------------------------------------------------------------------\n\n");
            }
            foreach (var item in commonFiles)
            {
                string sourcePath = Path.Combine(sourceDir, item);

                string destPath = Path.Combine(destDir, item);

                if (Directory.Exists(sourcePath))
                {
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                        string logEntry = $"Backed up Folder {item} from {sourceDir} to {destDir} \n\n";
                        Log(backupLogFile, logEntry);
                        BackupFolderCount++;
                        ReplacedFolderCount++;
                    }
                }
                else if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    string logEntry = $"Backed up File {item} from {sourceDir} to {destDir} \n\n";
                    Log(backupLogFile, logEntry);
                    ReplacedFileCount++;
                    BackupFileCount++;
                }
            }

            txtBackupCount.Content = $" Backed Up {BackupFolderCount} Folders & {BackupFileCount} Files ";
            txtReplacedCount.Content = $" Replaced {ReplacedFolderCount} Folders & {ReplacedFileCount} Files";

        }

        //method for appending lines to logs
        private void Log(string logFilePath, string logEntry)
        {
            File.AppendAllText(logFilePath, logEntry);
        }

        //methods to copy from source to destination
        private void CopySourceToDestinations()
        {
            string sourceFolder = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;

            foreach (var destination in ConfigManager.Config.Applications[ApplicationChoice].destinationFolders)
            {
                string destinationPath = destination.path;
                CopyDirectory(sourceFolder, destinationPath);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                CreatedFolderCount++;
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationDir, fileName);
                if (!File.Exists(destFile)) { CreatedFileCount++; }
                File.Copy(file, destFile, true);

            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                    CreatedFolderCount++;
                }
                CopyDirectory(subDir, destSubDir);

            }

            if (CreatedFileCount > 0 || CreatedFolderCount > 0)
            {
                txtCopyCount.Content = $" Created {CreatedFolderCount} Folders & {CreatedFileCount} Files ";
            }
            else
            { txtCopyCount.Content = " All files are similar. No new files to create "; }
        }

        //methods to backup source(empty it)
        private void CreateBackupSource(DateTime backupStamp)
        {
            string backupFolder = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;
            string backupPath = GetBackupName(backupStamp);

            string sourceFolder = ConfigManager.Config.Applications[ApplicationChoice].sourceFolder;

            string backupSubFolder = Path.Combine(backupFolder, backupPath, Path.GetFileName(sourceFolder));

            if (!Directory.Exists(backupSubFolder))
            {
                Directory.CreateDirectory(backupSubFolder);
            }
            BackupSource(sourceFolder, backupSubFolder);
        }

        private void BackupSource(string sourceFolder, string destinationFolder)
        {
            // Move all files
            foreach (string file in Directory.GetFiles(sourceFolder))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationFolder, fileName);
                File.Move(file, destFile);
            }

            // Move all directories
            foreach (string dir in Directory.GetDirectories(sourceFolder))
            {
                string dirName = new DirectoryInfo(dir).Name;
                string destDir = Path.Combine(destinationFolder, dirName);
                Directory.Move(dir, destDir);

            }
        }

        //method for rollback
        private void Rollback(string backupFolder)
        {
            string backupFolderPath = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;
            string backupPath = Path.Combine(backupFolder, DestinationFolderName);
            string backupFolderName = Path.GetFileName(backupFolder);
            string logPath = Path.Combine(backupFolderPath, RollbackFile);
            string rollbackDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, "ROLLBACK LOG:\n\n-----------------------------------------------------------------\n");
            }
            Log(logPath, $"Rollback of {backupFolderName} on {rollbackDateTime}: \n-----------------------------------------------------------------\n\n");

            foreach (var destination in ConfigManager.Config.Applications[ApplicationChoice].destinationFolders)
            {
                string rollbackPath = Path.Combine(backupPath, destination.name);
                string destPath = destination.path;

                var commonItems = CompareDirectoryPath(rollbackPath, destPath);

                RollBackItems(rollbackPath, destPath, commonItems);
            }
            Log(logPath, $"\n-----------------------------------------------------------------\n");
            MessageBox.Show($" Rolled backup {backupFolderName} back to {DestinationFolderName}", "Rollback Success", MessageBoxButton.OK);
        }

        private void RollBackItems(string sourceDir, string destDir, List<string> commonFiles)
        {
            string logPath = Path.Combine(ConfigManager.Config.Applications[ApplicationChoice].backupFolder, RollbackFile);

            foreach (var item in commonFiles)
            {
                if (Path.HasExtension(item))
                {
                    string sourceItemPath = Path.Combine(sourceDir, item);
                    string destItemPath = Path.Combine(destDir, item);
                    File.Copy(sourceItemPath, destItemPath, overwrite: true);
                    string logEntry = $"Copied {item} from {sourceDir} to {destDir}\n";
                    Log(logPath, $"{logEntry} \n");
                }
            }

        }

        //roll back btn method
        private void rollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string rollbackPath = BackupDropdown.SelectedValue.ToString();

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {Path.GetFileName(rollbackPath)} back to {DestinationFolderName}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Rollback(rollbackPath);
            }

        }

        //drodown menu methods
        private void LoadBackupOptions()
        {
            var backups = Directory.GetDirectories(ConfigManager.Config.Applications[ApplicationChoice].backupFolder)
            .OrderByDescending(Directory.GetCreationTime)
            .Select(dir => new { Name = Path.GetFileName(dir), Path = dir })
            .ToList();

            if (backups.Count == 0)
            {
                BackupDropdown.ItemsSource = new List<string> { "No backups found" };
                BackupDropdown.SelectedIndex = 0;
                rollbackBtn.IsEnabled = false;
                return;
            }

            BackupDropdown.ItemsSource = backups;
            BackupDropdown.DisplayMemberPath = "Name";
            BackupDropdown.SelectedValuePath = "Path";
            BackupDropdown.SelectedIndex = 0;

        }

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;
            LoadBackupOptions();
        }

        //Cleanup Method
        private void CleanupBackups()
        {
            string backupFolderPath = ConfigManager.Config.Applications[ApplicationChoice].backupFolder;

            int keepBackupsCount = ConfigManager.Config.Applications[ApplicationChoice].keepBackupsCount;

            var backups = Directory.GetDirectories(backupFolderPath).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count > keepBackupsCount)
            {

                MessageBoxResult result = MessageBox.Show($"Do you want to cleanup backups for {ApplicationChoice}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBoxResult result2 = MessageBox.Show($"This will keep the most recent {keepBackupsCount} backups. Are you sure you want to proceed?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result2 == MessageBoxResult.Yes)
                    {
                        backups = backups.Skip(keepBackupsCount).ToList();
                        foreach (var backup in backups)
                        {
                            Directory.Delete(backup, true);
                        }
                        MessageBox.Show($"Deleted {backups.Count} backups", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }

            }

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SkipInitialChange)
            {
                CleanupBackups();
            }
        }

        private void AddDestinationLabels()
        {
            DestinationLabelsPanel.Children.Clear();

            foreach (var destination in ConfigManager.Config.Applications[ApplicationChoice].destinationFolders)
            {
                var label = new Label
                {
                    Content = $"Name: {destination.name}\nPath: {destination.path}",
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                DestinationLabelsPanel.Children.Add(label);
            }
        }

        private void ClearLabels()
        {
            txtCopyCount.Content = string.Empty;
            txtBackupCount.Content = string.Empty;
            txtReplacedCount.Content = string.Empty;
        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window2 secondWindow = new Window2();
            secondWindow.Show();
        }
    }
}
