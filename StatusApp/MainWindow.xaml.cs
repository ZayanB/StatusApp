using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System;
using System.Windows.Media.TextFormatting;
using System.Diagnostics;
using System.Reflection.Metadata;





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
        private int copyFolderCount = 0;
        private int copyFileCount = 0;
        private int replacedFolderCount = 0;
        private int replacedFileCount = 0;
        public Config ConfigData { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            string configPath = "C:\\Users\\Zayan Breiche\\Projects\\StatusApp\\StatusApp\\config.json";

            string jsonString = File.ReadAllText(configPath);

            ConfigData = JsonSerializer.Deserialize<Config>(jsonString);

            DataContext = this;

            AddDestinationLabels();

            InitializeBackupFolderWatcher();

            LoadBackupOptions();
            this.Loaded += MainWindow_Loaded;

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

        //method to create backup folder with timestamp
        private string CreateBackupFolder()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolder = ConfigData.backupFolder;

            if (!Directory.Exists(backupFolder))
            {
                //throw new Exception($"Backup Folder does not exist: {backupFolder}");
                throw new DirectoryNotFoundException($"Backup Folder does not exist: {backupFolder}");
            }

            string newBackupFolder = Path.Combine(backupFolder, $"Backup_{timestamp}");

            return newBackupFolder;
        }

        //method to compare source and destination name only
        private List<string> ComparePaths(string path1, string path2)
        {

            HashSet<string> itemsInPath1 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(Path.GetFileName));
            HashSet<string> itemsInPath2 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(Path.GetFileName));

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            if (itemsInPath1.Count == 0)
            {
                throw new Exception($"Source Folder is Empty");
            }

            return commonItems;
        }

        //method to compare source and destination full path
        private List<string> CompareDirectory(string path1, string path2)
        {
            HashSet<string> itemsInPath1 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)));

            HashSet<string> itemsInPath2 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)));

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            if (itemsInPath1.Count == 0)
            {
                throw new Exception($"Source Folder is Empty");
            }

            return commonItems;
        }

        //methods to backup destination if same as source
        private void Backup(string backupPath)
        {

            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
            }

            if (!Directory.Exists(backupFolder))
            {
                throw new DirectoryNotFoundException($"BackUp folder does not exist: {backupFolder}");
            }

            string destinationBackupFolder = Path.Combine(backupFolder, backupPath, "Destination");

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    throw new DirectoryNotFoundException($"Destination folder does not exist: {destinationPath}");
                }

            }

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                var commonFiles = ComparePaths(sourceFolder, destinationPath);

                if (commonFiles.Count > 0)
                {
                    if (!Directory.Exists(destinationBackupFolder))
                    {
                        Directory.CreateDirectory(destinationBackupFolder);
                    }
                    string destinationSpecificBackupFolder = Path.Combine(destinationBackupFolder, destination.name);
                    if (!Directory.Exists(destinationSpecificBackupFolder))
                    {
                        Directory.CreateDirectory(destinationSpecificBackupFolder);
                    }
                    bool rollback = false;

                    BackupCommonItems(destinationPath, destinationSpecificBackupFolder, commonFiles, rollback);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtReplacedCount.Content = $" No similar files to be replaced ";
                    });
                }
            }
        }

        private void BackupCommonItems(string sourceDir, string destinationDir, List<string> commonItems, bool rollback)
        {

            if (!Directory.Exists(destinationDir) && !rollback)
            {
                Directory.CreateDirectory(destinationDir);
                backupFolderCount++;
            }

            // Copy common folders and files
            foreach (string itemName in commonItems)
            {
                string sourceItemPath = Path.Combine(sourceDir, itemName);
                string destItemPath = Path.Combine(destinationDir, itemName);

                if (Directory.Exists(sourceItemPath)) // If it's a folder
                {
                    // Copy the folder (even if empty)
                    if (!Directory.Exists(destItemPath))
                    {
                        Directory.CreateDirectory(destItemPath);
                        if (!rollback)
                        {
                            backupFolderCount++;
                            replacedFolderCount++;
                        }
                    }

                    // Recursion
                    BackupCommonItems(sourceItemPath, destItemPath, commonItems, rollback);
                }
                else if (File.Exists(sourceItemPath))
                {
                    // Ensure the parent directory exists
                    string parentDir = Path.GetDirectoryName(destItemPath);
                    if (!Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                        if (!rollback)
                        {
                            backupFolderCount++;
                            replacedFolderCount++;
                        }
                    }

                    // Copy the file to the backup folder
                    File.Copy(sourceItemPath, destItemPath, overwrite: true);
                    if (!rollback)
                    {
                        backupFileCount++;
                        replacedFileCount++;
                    }

                }

            }
            if (!rollback)
            {
                Dispatcher.Invoke(() =>
                {
                    txtReplacedCount.Content = $" Replaced {replacedFileCount} Files & {replacedFolderCount} Folders ";
                });
            }


        }

        //methods to copy from source to destination
        private void CopySourceToDestinations()
        {

            string sourceFolder = ConfigData.sourceFolder;

            copyFolderCount = 0;
            copyFileCount = 0;

            Dispatcher.Invoke(() =>
            {
                txtCopyCount.Content = $" Copied {copyFolderCount} Folders & {copyFileCount} Files ";
            });


            if (Directory.Exists(sourceFolder))
            {
                foreach (var destination in ConfigData.destinationFolders)
                {
                    string destinationPath = destination.path;

                    if (Directory.Exists(destinationPath))
                    {
                        CopyDirectory(sourceFolder, destinationPath);

                        //Console.WriteLine($"Copied from: {sourceFolder} to {destinationPath}");
                    }
                    else
                    {
                        MessageBox.Show($"Destination folder does not exist: {destinationPath}");
                    }
                }
            }
            else
            {
                MessageBox.Show($"Source folder does not exist: {sourceFolder}");
            }

        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationDir, fileName);
                File.Copy(file, destFile, overwrite: true);
                copyFileCount++;

            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);
                CopyDirectory(subDir, destSubDir);

                copyFolderCount++;
            }

            Dispatcher.Invoke(() =>
            {
                txtCopyCount.Content = $" Copied {copyFolderCount} Folders & {copyFileCount} Files ";
            });
        }

        //methods to backup source (empty it)
        private void CreateBackupSource(string backupPath)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            string backupSubFolder = Path.Combine(backupFolder, backupPath);
            string sourceBackupFolder = Path.Combine(backupSubFolder, "Source");

            // Backup the source 

            if (!Directory.Exists(sourceBackupFolder))
            {
                Directory.CreateDirectory(sourceBackupFolder);
            }
            BackupSource(sourceFolder, sourceBackupFolder);
        }
        private void BackupSource(string sourceFolder, string destinationFolder)
        {

            // Move all files
            foreach (string file in Directory.GetFiles(sourceFolder))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationFolder, fileName);
                File.Move(file, destFile);
                //Console.WriteLine($"Moved file: {fileName}");
                backupFileCount++;
            }

            // Move all directories
            foreach (string dir in Directory.GetDirectories(sourceFolder))
            {
                string dirName = new DirectoryInfo(dir).Name;
                string destDir = Path.Combine(destinationFolder, dirName);
                Directory.Move(dir, destDir);
                //Console.WriteLine($"Moved directory: {dirName}");
                backupFolderCount++;
            }

            // Update the UI with the counts
            Dispatcher.Invoke(() =>
            {
                txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
            });
        }

        //method for creating log file
        private void CreateLogFile(string backupPath)
        {
            string sourceFolder = ConfigData.sourceFolder;
            string logBackupFile = Path.Combine(ConfigData.backupFolder, backupPath, "Backup Log.txt");

            int lastUnderScioreIndex = backupPath.LastIndexOf("Backup_") + 7;
            string timestamp = backupPath.Substring(lastUnderScioreIndex);

            List<string> logEntries = new List<string>
            {
                    $"BACKUP LOG {timestamp}",
                    "===================================",
                    ""
            };
            HashSet<string> sourceFolderItems = new HashSet<string>(Directory.EnumerateFileSystemEntries(sourceFolder, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(sourceFolder, path)));

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;
                var commonFiles = CompareDirectory(sourceFolder, destinationPath);

                logEntries.Add($"From: {sourceFolder}");
                logEntries.Add($"To: {destinationPath}");
                logEntries.Add("-------------------------------------------------------");

                if (commonFiles.Count > 0)
                {
                    logEntries.Add("Replaced Files:");


                    foreach (var commonFile in commonFiles)
                    {
                        //Console.WriteLine(commonFile);
                        logEntries.Add(commonFile);
                    }

                    logEntries.Add("");
                }
                else
                {
                    logEntries.Add("No common items to be replaced.");
                    logEntries.Add("");
                }

                var copiedFiles = sourceFolderItems.Except(commonFiles);

                if (copiedFiles.Any())
                {
                    logEntries.Add("Added Files:");
                    foreach (var copiedFile in copiedFiles)
                    {
                        logEntries.Add(copiedFile);
                    }
                    logEntries.Add("");
                }
                else
                {
                    logEntries.Add("No new files copied.");
                    logEntries.Add("");
                }
                logEntries.Add("-------------------------------------------------------");

            }

            Directory.CreateDirectory(Path.GetDirectoryName(logBackupFile));


            //if (logEntries.Count > 3)
            //{
            File.WriteAllLines(logBackupFile, logEntries);
            //}

        }

        //method for rollback 
        private void Rollback(string backupFolder)
        {
            string backupPath = Path.Combine(backupFolder, "Destination");
            bool rollback = true;

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;
                var commonFiles = ComparePaths(backupPath, destinationPath);

                if (commonFiles.Any())
                {
                    string destinationSpecificBackupFolder = Path.Combine(backupPath, destination.name);
                    BackupCommonItems(destinationSpecificBackupFolder, destinationPath, commonFiles, rollback);
                }
            }

            int lastUnderScioreIndex = backupFolder.LastIndexOf("Backup_") + 7;
            string timestamp = backupFolder.Substring(lastUnderScioreIndex);

            Dispatcher.Invoke(() =>
            {
                txtRollbackStatus.Content = $" Rolled backup: {timestamp} back to destination";
            });

        }

        //run button method
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {

            //TestCompare();
            //TestCompareNew();

            try
            {

                string backupPath = CreateBackupFolder();

                Backup(backupPath);

                CreateLogFile(backupPath);

                CopySourceToDestinations();

                //CreateBackupSource(backupPath);

                backupFolderCount = 0;
                backupFileCount = 0;
                copyFolderCount = 0;
                copyFileCount = 0;
                replacedFolderCount = 0;
                replacedFileCount = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed. {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        //roll back btn method
        private void rollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string backUpFolder = SelectedBackupPath;
            Rollback(backUpFolder);
        }

        //drodown menu methods
        private void LoadBackupOptions()
        {
            var backups = Directory.GetDirectories(ConfigData.backupFolder).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count == 0)
            {
                MessageBox.Show("No backups found.");
                return;
            }

            BackupDropdown.ItemsSource = backups.Select(path => Path.GetFileName(path)).ToList();

            // Select the most recent backup by default
            BackupDropdown.SelectedIndex = 0;
            SelectedBackupPath = backups[0]; // Store the default selection
        }

        private void BackupDropdown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            if (BackupDropdown.SelectedIndex >= 0)
            {
                string selectedFolderName = BackupDropdown.SelectedItem.ToString();
                SelectedBackupPath = Path.Combine(ConfigData.backupFolder, selectedFolderName);
                //Console.WriteLine(SelectedBackupPath);
            }
        }

        private FileSystemWatcher _backupFolderWatcher;

        private void InitializeBackupFolderWatcher()
        {
            _backupFolderWatcher = new FileSystemWatcher
            {
                Path = ConfigData.backupFolder,
                NotifyFilter = NotifyFilters.DirectoryName, // Watch for directory changes
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

            var backups = Directory.GetDirectories(ConfigData.backupFolder).OrderBy(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count > cleanupValue)
            {

                MessageBoxResult result = MessageBox.Show("Do you want to cleanup backups?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBoxResult result2 = MessageBox.Show("This will keep the last 10 backups. Are you sure you want to proceed?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result2 == MessageBoxResult.Yes)
                    {
                        backups = backups.Skip(cleanupValue).ToList();
                        foreach (var backup in backups)
                        {
                            Directory.Delete(backup, true);
                        }
                        MessageBox.Show($"Deleted {backups.Count} backups","Success",MessageBoxButton.OK,MessageBoxImage.Information);
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

        //testing methods
        //private void TestCompare()
        //{
        //    string sourceFolder = ConfigData.sourceFolder;

        //    foreach (var destination in ConfigData.destinationFolders)
        //    {
        //        string destinationPath = destination.path;
        //        var commonFiles = CompareDirectoriesNew(sourceFolder, destinationPath);
        //        foreach (var commonFile in commonFiles)
        //        {
        //            Console.WriteLine(commonFile);
        //        }
        //    }

        //}



        //private void TestCompareNew()
        //{
        //    string sourceFolder = ConfigData.sourceFolder;


        //    foreach (var destination in ConfigData.destinationFolders)
        //    {
        //        string destinationPath = destination.path;
        //        var commonFiles = ComparePaths(sourceFolder, destinationPath);
        //        foreach (var commonFile in commonFiles)
        //        {
        //            replacedItems++;
        //        }
        //    }

        //    Console.WriteLine($"the replaced items are: {replacedItems}");
        //    Dispatcher.Invoke(() =>
        //    {
        //        txtReplacedCount.Content = $" Replaced {replacedItems} Files ";
        //    });

        //}


    }
}
