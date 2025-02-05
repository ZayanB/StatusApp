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
        public string SelectedBackupPath { get; private set; } = string.Empty;
        private int backupFolderCount = 0;
        private int backupFileCount = 0;
        private int replacedFolderCount = 0;
        private int replacedFileCount = 0;
        private int createdFileCount = 0;
        private int createdFolderCount = 0;

        //private static readonly string configPath = "C:\\Users\\Zayan Breiche\\Projects\\StatusApp\\StatusApp\\config.json";
        private static readonly string configPath = "C:\\Users\\Zayan\\Source\\Repos\\StatusApp\\StatusApp\\ConfigZ.json";

        public Config ConfigData { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            string jsonString = File.ReadAllText(configPath);

            ConfigData = JsonSerializer.Deserialize<Config>(jsonString);

            DataContext = this;
            string backupFolderPath = ConfigData.backupFolder;
            AddDestinationLabels();

            InitializeBackupFolderWatcher();

            LoadBackupOptions();
            this.Loaded += MainWindow_Loaded;

        }

        //run button method
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckFolders())
                {
                    return;
                }

                DateTime timestamp = CreateBackupInstance();

                BackupDestination(timestamp);

                CopySourceToDestinations();

                CreateBackupSource(timestamp);

                backupFolderCount = 0;
                backupFileCount = 0;
                replacedFolderCount = 0;
                replacedFileCount = 0;
                createdFolderCount = 0;
                createdFileCount = 0;
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
            string backupFolder = ConfigData.backupFolder;

            if (!Directory.Exists(backupFolder))
            {
                throw new DirectoryNotFoundException($"Backup Folder does not exist: {backupFolder}");
            }

            DateTime currentTime = DateTime.Now;
            string backupFolderSub = "Backup_" + currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolderPath = Path.Combine(backupFolder, backupFolderSub);

            Directory.CreateDirectory(backupFolderPath);

            return currentTime;
        }

        //method to check folders
        private bool CheckFolders()
        {
            string sourceFolder = ConfigData.sourceFolder;
            string backupFolder = ConfigData.backupFolder;

            if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
            {
                MessageBox.Show("Source Folder is Empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Directory.Exists(sourceFolder))
            {
                MessageBox.Show($"Source Folder is not existing at: {sourceFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Directory.Exists(backupFolder))
            {
                MessageBox.Show($"Backup Folder is not existing at: {backupFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    MessageBox.Show($"Destination Folder is not existing at: {destinationPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        //method to compare source and destination
        private List<string> CompareDirectoryPath(string path1, string path2)
        {
            List<string> itemsInPath1 = Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)).ToList();

            List<string> itemsInPath2 = Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)).ToList();

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            return commonItems;
        }

        //methods to backup destination if same as source & create backup log
        private void BackupDestination(DateTime backupStamp)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;
            string backupPath = Path.Combine(backupFolder, "Backup_" + backupStamp.ToString("yyyy-MM-dd_HH-mm-ss"));

            string destinationBackupFolder = Path.Combine(backupPath, "Destination");
            
            //Compare source with all destinations to check for common files 
            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                var commonFiles = CompareDirectoryPath(sourceFolder, destinationPath);

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
                    ReplaceItems(destinationPath, specificBackupFolder, commonFiles, backupStamp);
                }
                else
                {

                    txtBackupCount.Content = $" Nothing backedup. all are different";
                    txtReplacedCount.Content = $" Nothing replaced.";

                }
            }
        }

        private void ReplaceItems(string sourceDir, string destDir, List<string> commonFiles, DateTime backupStamp)
        {

            string backupPath = Path.Combine(ConfigData.backupFolder, "Backup_" + backupStamp.ToString("yyyy-MM-dd_HH-mm-ss"));
            string backupDateTime = "Backup: " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n -----------------------------------------------------------------------------------------------------\n");
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
                        string logEntry = $"Backed up Folder {item} from {sourceDir} to {destDir} \n";
                        File.AppendAllText(backupLogFile, logEntry);
                        backupFolderCount++;
                        replacedFolderCount++;
                    }
                }
                else if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    replacedFileCount++;
                    backupFileCount++;
                    string logEntry = $"Backed up File {item} from {sourceDir} to {destDir} \n";
                    File.AppendAllText(backupLogFile, logEntry);
                }
            }

            txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
            txtReplacedCount.Content = $" Replaced {replacedFolderCount} Folders & {replacedFileCount} Files";

        }

        //methods to copy from source to destination
        private void CopySourceToDestinations()
        {
            string sourceFolder = ConfigData.sourceFolder;

            foreach (var destination in ConfigData.destinationFolders)
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
                createdFolderCount++;
            }


            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationDir, fileName);
                if (!File.Exists(destFile)) { createdFileCount++; }
                File.Copy(file, destFile, overwrite: true);

            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                    createdFolderCount++;
                }
                CopyDirectory(subDir, destSubDir);

            }
            if (createdFileCount > 0 || createdFolderCount > 0)
            {
                txtCopyCount.Content = $" Created {createdFolderCount} Folders & {createdFileCount} Files ";
            }
            else
            { txtCopyCount.Content = " All files are similar. No new files to create "; }

        }

        //methods to backup source (empty it)
        private void CreateBackupSource(DateTime backupStamp)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;
            string backupPath = Path.Combine(backupFolder, "Backup_" + backupStamp.ToString("yyyy-MM-dd_HH-mm-ss"));

            string backupSubFolder = Path.Combine(backupFolder, backupPath, Path.GetFileName(sourceFolder));

            // Backup the source 

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
            string backupPath = Path.Combine(backupFolder, "Destination");
            string backupFolderName = Path.GetFileName(backupFolder);

            foreach (var destination in ConfigData.destinationFolders)
            {
                string rollbackPath = Path.Combine(backupPath, destination.name);
                string destPath = destination.path;

                var commonItems = CompareDirectoryPath(rollbackPath, destPath);

                RollBackItems(rollbackPath, destPath, commonItems);
            }

            MessageBox.Show($" Rolled backup {backupFolderName} back to destination", "Rollback Success", MessageBoxButton.OK);
        }

        private void RollBackItems(string sourceDir, string destDir, List<string> commonFiles)
        {
            string logPath = Path.Combine(ConfigData.backupFolder, "Rollback Log.txt");
            string rollbackDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");
            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, "Rollback Log: \n ----------------------------------------------------------------------------------\n");
            }


            foreach (var item in commonFiles)
            {
                if (Path.HasExtension(item))
                {
                    string sourceItemPath = Path.Combine(sourceDir, item);
                    string destItemPath = Path.Combine(destDir, item);
                    File.Copy(sourceItemPath, destItemPath, overwrite: true);
                    string logEntry = $"Copied {item} from {sourceDir} to {destDir} on {rollbackDateTime} \n";
                    File.AppendAllText(logPath, logEntry);
                }
            }

            File.AppendAllText(logPath, " ----------------------------------------------------------------------------------\n");
        }

        //roll back btn method
        private void rollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string backUpFolder = SelectedBackupPath;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {Path.GetFileName(backUpFolder)} back to destination?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Rollback(backUpFolder);
            }

        }

        //drodown menu methods
        private void LoadBackupOptions()
        {
            var backups = Directory.GetDirectories(ConfigData.backupFolder).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count == 0)
            {
                BackupDropdown.ItemsSource = new List<string> { "No backups found" };
                BackupDropdown.SelectedIndex = 0;
                SelectedBackupPath = null;
                return;
            }

            BackupDropdown.ItemsSource = backups.Select(path => Path.GetFileName(path)).ToList();
            // Select the most recent backup by default
            BackupDropdown.SelectedIndex = 0;
            SelectedBackupPath = backups[0];
        }

        private void BackupDropdown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            if (BackupDropdown.SelectedIndex >= 0)
            {
                string selectedFolderName = BackupDropdown.SelectedItem.ToString();
                SelectedBackupPath = Path.Combine(ConfigData.backupFolder, selectedFolderName);
            }
        }

        private FileSystemWatcher _backupFolderWatcher;

        private void InitializeBackupFolderWatcher()
        {
            _backupFolderWatcher = new FileSystemWatcher
            {
                Path = ConfigData.backupFolder,
                NotifyFilter = NotifyFilters.DirectoryName, // Watch for directory changes, create,delet,or rename
                EnableRaisingEvents = true
            };

            _backupFolderWatcher.Created += OnBackupFolderChanged;
            _backupFolderWatcher.Deleted += OnBackupFolderChanged;
            _backupFolderWatcher.Renamed += OnBackupFolderChanged;
        }

        private void OnBackupFolderChanged(object sender, FileSystemEventArgs e)
        {
            // Refresh the dropdown when a change is detected
            Dispatcher.Invoke(LoadBackupOptions);
        }

        //Cleanup Method
        private void CleanupBackups()
        {
            int cleanupValue = ConfigData.cleanupValue;

            var backups = Directory.GetDirectories(ConfigData.backupFolder).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count > cleanupValue)
            {

                MessageBoxResult result = MessageBox.Show("Do you want to cleanup backups?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBoxResult result2 = MessageBox.Show($"This will keep the most recent {cleanupValue} backups. Are you sure you want to proceed?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result2 == MessageBoxResult.Yes)
                    {
                        backups = backups.Skip(cleanupValue).ToList();
                        foreach (var backup in backups)
                        {
                            Directory.Delete(backup, true);
                        }
                        MessageBox.Show($"Deleted {backups.Count} backups", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

        }

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CleanupBackups();
        }

        private void AddDestinationLabels()
        {
            foreach (var destination in ConfigData.destinationFolders)
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

    }
}
