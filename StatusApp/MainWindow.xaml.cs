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

        private static readonly string configPath = "C:\\Users\\Zayan Breiche\\Projects\\StatusApp\\StatusApp\\config.json";

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

                string backupPath = CreateBackupFolderPath();

                BackupDestination(backupPath);

                //CreateLogFile(backupPath);

                CopySourceToDestinations();

                //CreateBackupSource(backupPath);

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
        private string CreateBackupFolderPath()
        {
            var currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolder = ConfigData.backupFolder;

            if (!Directory.Exists(backupFolder))
            {
                throw new DirectoryNotFoundException($"Backup Folder does not exist: {backupFolder}");
            }

            string newBackupFolder = Path.Combine(backupFolder, $"Backup_{timestamp}");
            //Console.WriteLine(newBackupFolder);
            Directory.CreateDirectory(newBackupFolder);
            return newBackupFolder;
        }

        //method to compare source and destination
        private List<string> CompareDirectoryPath(string path1, string path2)
        {
            List<string> itemsInPath1 = Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)).ToList();

            List<string> itemsInPath2 = Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)).ToList();

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            //if (itemsInPath1.Count == 0)
            //{
            //    throw new Exception($"Source Folder is Empty");
            //}

            return commonItems;
        }

        //methods to backup destination if same as source
        private void BackupDestination(string backupPath)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            //Check for all path existence before proceeding
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
            }

            if (!Directory.Exists(backupFolder))
            {
                throw new DirectoryNotFoundException($"BackUp folder does not exist: {backupFolder}");
            }

            string destinationBackupFolder = Path.Combine(backupPath, "Destination");
            //Console.WriteLine(destinationBackupFolder);

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    throw new DirectoryNotFoundException($"Destination folder does not exist: {destinationPath}");
                }

            }

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
                    ReplaceItems(destinationPath, specificBackupFolder, commonFiles, false, backupPath);
                }
                else
                {

                    txtBackupCount.Content = $" Nothing backedup. all are different";
                    txtReplacedCount.Content = $" Nothing replaced.";


                }
            }
        }

        private void ReplaceItems(string sourceDir, string destDir, List<string> commonFiles, bool rollback, string backupPath = "")
        {

            if (rollback)
            {
                string logPath = Path.Combine(ConfigData.backupFolder, "Rollback Log.txt");
                if (!File.Exists(logPath))
                {
                    File.WriteAllText(logPath, "Rollback Log \n ----------------------------------------------------------------------------------\n");
                }
                foreach (var item in commonFiles)
                {
                    if (Path.HasExtension(item))
                    {
                        string sourceItemPath = Path.Combine(sourceDir, item);
                        string destItemPath = Path.Combine(destDir, item);
                        File.Copy(sourceItemPath, destItemPath, overwrite: true);
                        string logEntry = $"Copied {item} from {sourceDir} to {destDir} \n";
                        File.AppendAllText(logPath, logEntry);
                    }
                }

                File.AppendAllText(logPath, " ----------------------------------------------------------------------------------\n");
            }
            else
            {
                string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

                string folderName = Path.GetFileName(backupPath);
                int splitIndex = folderName.LastIndexOf('_') + 1;
                string backupDateTime = folderName[..splitIndex] + folderName[splitIndex..].Replace('-', ':');

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
                            string logEntry = $"Replaced Folder {item} from {sourceDir} to {destDir} \n";
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
                        string logEntry = $"Replaced File {item} from {sourceDir} to {destDir} \n";
                        File.AppendAllText(backupLogFile, logEntry);
                    }
                }

                txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
                txtReplacedCount.Content = $" Replaced {replacedFolderCount} Folders & {replacedFileCount} Files";


            }


        }

        //private void CreateLogFile(string backupPath)
        //{
        //    string sourceFolder = ConfigData.sourceFolder;
        //    string logBackupFile = Path.Combine(ConfigData.backupFolder, backupPath, "Backup Log.txt");

        //    int lastUnderScioreIndex = backupPath.LastIndexOf("Backup_") + 7;
        //    string timestamp = backupPath.Substring(lastUnderScioreIndex);
        //    string[] timestampNew = timestamp.Split('_');
        //    string formattedTime = timestampNew[1].Replace("-", ":");
        //    timestamp = $"{timestampNew[0]}_{formattedTime}";

        //    List<string> logEntries = new List<string>
        //    {
        //            $"BACKUP LOG {timestamp}",
        //            "===================================",
        //            ""
        //    };

        //    List<string> sourceFolderItems = Directory.EnumerateFileSystemEntries(sourceFolder, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(sourceFolder, path)).ToList();

        //    foreach (var destination in ConfigData.destinationFolders)
        //    {
        //        string destinationPath = destination.path;
        //        var commonFiles = CompareDirectoryPath(sourceFolder, destinationPath);

        //        logEntries.Add($"From: {sourceFolder}");
        //        logEntries.Add($"To: {destinationPath}");
        //        logEntries.Add("-------------------------------------------------------");

        //        if (commonFiles.Count > 0)
        //        {
        //            logEntries.Add("Replaced Files:");


        //            foreach (var commonFile in commonFiles)
        //            {
        //                logEntries.Add(commonFile);
        //            }

        //            logEntries.Add("");
        //        }
        //        else
        //        {
        //            logEntries.Add("No common items to be replaced.");
        //            logEntries.Add("");
        //        }

        //        var newFiles = sourceFolderItems.Except(commonFiles);

        //        if (newFiles.Any())
        //        {
        //            logEntries.Add("Added Files:");
        //            foreach (var newFile in newFiles)
        //            {
        //                logEntries.Add(newFile);
        //            }
        //            logEntries.Add("");
        //        }
        //        else
        //        {
        //            logEntries.Add("No new files added.");
        //            logEntries.Add("");
        //        }
        //        logEntries.Add("-------------------------------------------------------");

        //    }

        //    Directory.CreateDirectory(Path.GetDirectoryName(logBackupFile));

        //    File.WriteAllLines(logBackupFile, logEntries);

        //}

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
        private void CreateBackupSource(string backupPath)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            string backupSubFolder = Path.Combine(backupFolder, backupPath, Path.GetFileName(sourceFolder));
            Console.WriteLine(backupSubFolder);

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

            //var backupFolderDirectories = Directory.GetDirectories(backupPath);

            foreach (var destination in ConfigData.destinationFolders)
            {
                string rollbackPath = Path.Combine(backupPath, destination.name);
                string destPath = destination.path;

                var commonItems = CompareDirectoryPath(rollbackPath, destPath);
                ReplaceItems(rollbackPath, destPath, commonItems, true);
            }
            //Console.WriteLine(backupFolder);
            int lastUnderScioreIndex = backupFolder.LastIndexOf("Backup_") + 7;
            string timestamp = backupFolder.Substring(lastUnderScioreIndex);

            MessageBox.Show($" Rolled backup {timestamp} back to destination", "Rollback Success", MessageBoxButton.OK);
        }

        //method for rollback log
        private void CreateRollbackLog(string timestamp)
        {
            string logPath = Path.Combine(ConfigData.backupFolder, "Rollback Log.txt");
            string rollbackTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

            string[] timestampNew = timestamp.Split('_');
            string formattedTime = timestampNew[1].Replace("-", ":");
            timestamp = $"{timestampNew[0]}_{formattedTime}";

            List<string> logs = new List<string>
            {
                $"Rolled Backup {timestamp} back to destination on {rollbackTime}"
            };

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, "Rollback Log\n");
                File.AppendAllText(logPath, "-----------------------------------------------------------------------------\n");
            }

            File.AppendAllLines(logPath, logs);

        }

        //roll back btn method
        private void rollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string backUpFolder = SelectedBackupPath;
            string formattedTimesStamp = string.Empty;

            int lastUnderScioreIndex = backUpFolder.LastIndexOf("Backup_") + 7;
            string timestamp = backUpFolder.Substring(lastUnderScioreIndex);
            string[] timestampMsg = timestamp.Split('_');

            if (timestampMsg.Length == 2)
            {
                string formattedTime = timestampMsg[1].Replace('-', ':');
                formattedTimesStamp = $"{timestampMsg[0]}_{formattedTime}";
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {formattedTimesStamp} back to destination?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
