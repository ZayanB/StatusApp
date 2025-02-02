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

namespace StatusApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int backupFolderCount = 0;
        private int backupFileCount = 0;
        private int copyFolderCount = 0;
        private int copyFileCount = 0;
        public Config ConfigData { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            string configPath = "C:\\Users\\Zayan Breiche\\Projects\\StatusApp\\StatusApp\\config.json";

            string jsonString = File.ReadAllText(configPath);

            ConfigData = JsonSerializer.Deserialize<Config>(jsonString);

            DataContext = this;

            AddDestinationLabels();
        }
        private void AddDestinationLabels()
        {
            foreach (var destination in ConfigData.destinationFolders)
            {
                var label = new Label
                {
                    Content = $"Name: {destination.name}\nPath: {destination.path}",
                    FontSize = 15,
                };
                DestinationLabelsPanel.Children.Add(label);
            }
        }

        //method to create backup folder with timestamp
        private string CreateBackupFolder()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolder = ConfigData.backupFolder;

            string newBackupFolder = Path.Combine(backupFolder, $"Backup_{timestamp}");

            return newBackupFolder;
        }

        //method to compare source and destination
        public static List<string> ComparePaths(string path1, string path2)
        {
            if (!Directory.Exists(path1) || !Directory.Exists(path2))
            {
                throw new DirectoryNotFoundException("One or both of the specified paths do not exist.");
            }

            HashSet<string> itemsInPath1 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(Path.GetFileName));
            HashSet<string> itemsInPath2 = new HashSet<string>(Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(Path.GetFileName));

            return itemsInPath1.Intersect(itemsInPath2).ToList();
        }

        //methods to backup destination if same as source
        private void Backup(out bool success, string backupPath)
        {
            success = false;

            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            backupFolderCount = 0;
            backupFileCount = 0;

            Dispatcher.Invoke(() =>
            {
                txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
            });


            string destinationBackupFolder = Path.Combine(backupFolder, backupPath, "Destination");

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (Directory.Exists(destinationPath))
                {
                    var commonFiles = ComparePaths(sourceFolder, destinationPath);

                    if (commonFiles.Count() > 0)
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

                        BackupCommonItems(sourceFolder, destinationSpecificBackupFolder, commonFiles);
                    }
                    else
                    {
                        Console.WriteLine($"No changes in destination: {destinationPath}. Skipping back up");
                    }
                }
                else
                {
                    MessageBox.Show($"Destination folder does not exist: {destinationPath}");
                }

            }


            success = true;
        }

        private void BackupCommonItems(string sourceDir, string destinationDir, List<string> commonItems)
        {
   
            if (!Directory.Exists(destinationDir))
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
                        backupFolderCount++;
                    }

                    // Recursion
                    BackupCommonItems(sourceItemPath, destItemPath, commonItems);
                }
                else if (File.Exists(sourceItemPath)) 
                {
                    // Ensure the parent directory exists
                    string parentDir = Path.GetDirectoryName(destItemPath);
                    if (!Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                        backupFolderCount++;
                    }

                    // Copy the file to the backup folder
                    File.Copy(sourceItemPath, destItemPath, overwrite: true);
                    backupFileCount++;

                    Dispatcher.Invoke(() =>
                    {
                        txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
                    });
                }
            }
        }
      
        //methods to copy from source to destination
        private void CopySourceToDestinations(out bool success)
        {
            success = false;
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

                        Console.WriteLine($"Copied from: {sourceFolder} to {destinationPath}");
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
            success = true;
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

        //methods to backup (empty) source
        private void BackupSource(string sourceFolder, string destinationFolder)
        {
            try
            {
                // Move all files
                foreach (string file in Directory.GetFiles(sourceFolder))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationFolder, fileName);
                    File.Move(file, destFile);
                    Console.WriteLine($"Moved file: {fileName}");
                    backupFileCount++;
                }

                // Move all directories
                foreach (string dir in Directory.GetDirectories(sourceFolder))
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    string destDir = Path.Combine(destinationFolder, dirName);
                    Directory.Move(dir, destDir);
                    Console.WriteLine($"Moved directory: {dirName}");
                    backupFolderCount++;
                }

                // Update the UI with the counts
                Dispatcher.Invoke(() =>
                {
                    txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while moving files/folders: {ex.Message}");
            }
        }

        private void CreateBackupSource(string backupPath)
        {
            string backupFolder = ConfigData.backupFolder;
            string sourceFolder = ConfigData.sourceFolder;

            string backupSubFolder = Path.Combine(backupFolder, backupPath);
            string sourceBackupFolder = Path.Combine(backupSubFolder, "Source");

            // Backup the source 
            if (Directory.Exists(sourceFolder))
            {
                if (!Directory.Exists(sourceBackupFolder))
                {
                    Directory.CreateDirectory(sourceBackupFolder);
                }

                BackupSource(sourceFolder, sourceBackupFolder);
            }
            else
            {
                MessageBox.Show($"Source folder does not exist: {sourceFolder}");
                return;
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
        //            Console.WriteLine(commonFile);
        //        }
        //    }

        //}

        //run button method
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {

            //TestCompare();
            //TestCompareNew();

            bool isSuccessful;
            bool isCopySuccessful;

            string backupPath = CreateBackupFolder();

            Backup(out isSuccessful, backupPath);

            CopySourceToDestinations(out isCopySuccessful);

            if (isCopySuccessful)


            {
                copyStatusText.Content = "Copied Successfully";
            }
            else
            {
                copyStatusText.Content = "Copy UnSuccessfull";
            }


            if (isSuccessful)
            {
                statusText.Content = "Backed Up Successfully";
            }
            else
            {
                statusText.Content = "Back Up Failed";
                statusText.Content = "Back Up Failed";
            }

            CreateBackupSource(backupPath);
        }

    }
}
