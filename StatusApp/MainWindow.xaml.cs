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
        private string createBackupFolder()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolder = ConfigData.backupFolder;

            string newBackupFolder = Path.Combine(backupFolder, $"Backup_{timestamp}");

            return newBackupFolder;
        }

        //method to compare directories
        private bool CompareDirectories(string sourceDir, string destinationDir)
        {
            var sourceFiles = Directory.GetFiles(sourceDir).Select(Path.GetFileName).ToHashSet();
            var destinationFiles = Directory.GetFiles(destinationDir).Select(Path.GetFileName).ToHashSet();

            var sourceSubDirs = Directory.GetDirectories(sourceDir).Select(Path.GetFileName).ToHashSet();
            var destinationSubDirs = Directory.GetDirectories(destinationDir).Select(Path.GetFileName).ToHashSet();

            if (!sourceFiles.SetEquals(destinationFiles) || !sourceSubDirs.SetEquals(destinationSubDirs))
            {
                return false;
            }

            foreach (var subDir in sourceSubDirs)
            {
                string sourceSubDirPath = Path.Combine(sourceDir, subDir);
                string destSubDirPath = Path.Combine(destinationDir, subDir);

                if (!CompareDirectories(sourceSubDirPath, destSubDirPath))
                {
                    return false;
                }

            }

            return true;
        }

        //methods to backup destination if same as source
        private void backup(out bool success,string backupPath)
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

            
            string backupSubFolder = Path.Combine(backupFolder, backupPath);
           
            string destinationBackupFolder = Path.Combine(backupSubFolder, "Destination");

            if (!Directory.Exists(backupSubFolder))
            {
                Directory.CreateDirectory(backupSubFolder);
            }

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (Directory.Exists(destinationPath))
                {
                    if (CompareDirectories(sourceFolder, destinationPath))
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

                        backUpDirectory(destinationPath, destinationSpecificBackupFolder);
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

        private void backUpDirectory(string sourceDir, string destinationDir)
        {

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                backupFolderCount++;
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string filename = Path.GetFileName(file);
                string destFile = Path.Combine(destinationDir, filename);
                File.Copy(file, destFile, overwrite: true);
                backupFileCount++;

                Dispatcher.Invoke(() =>
                {
                    txtBackupCount.Content = $" Backed Up {backupFolderCount} Folders & {backupFileCount} Files ";
                });
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);

                backUpDirectory(subDir, destSubDir);
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
        private void backupSource(string sourceFolder, string destinationFolder)
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

        private void createBackupSource(string backupPath)
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

                backupSource(sourceFolder, sourceBackupFolder);
            }
            else
            {
                MessageBox.Show($"Source folder does not exist: {sourceFolder}");
                return;
            }

        }

        //run button method
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSuccessful;
            bool isCopySuccessful;

            string backupPath = createBackupFolder();

            backup(out isSuccessful,backupPath);

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

            createBackupSource(backupPath);
        }      

    }
}
