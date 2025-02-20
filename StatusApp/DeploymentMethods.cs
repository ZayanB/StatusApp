using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace StatusApp
{
    public class DeploymentMethods
    {
        public static readonly string BackupFolderName = "Backup";
        public static readonly string DestinationFolderName = "Destination";
        private static readonly string RollbackFile = "Rollback Log.txt";
        public static readonly string SourceFolderName = "Source";

        //method to check if directories exist at paths
        public bool CheckFolders(string sourceFolder, string backupFolder, List<Destinations> destinationFolders)
        {
            if (!Directory.Exists(sourceFolder))
            {
                ShowMessageBox($"Source  is not existing at: {sourceFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Directory.Exists(backupFolder))
            {
                ShowMessageBox($"backup is not existing at: {backupFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            foreach (var destination in destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    ShowMessageBox($"destination is not existing at: {destinationPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        //method to check if a directory is empty
        public bool IsDirectoryEmpty(string directoryPath)
        {
            string directoryName = Path.GetFileName(directoryPath);

            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                ShowMessageBox($"{directoryName} is Empty.Operation can't be completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            return false;
        }

        //method to create backup directory with specific date & time
        public DateTime CreateBackupInstance(string backupFolder) //COULD BE TESTED
        {
            DateTime currentTime = DateTime.Now;
            string backupFolderSub = "Backup_" + currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolderPath = Path.Combine(backupFolder, backupFolderSub);

            Directory.CreateDirectory(backupFolderPath);

            return currentTime;
        }

        //method that compares two directories and return the paths of common items
        public List<string> CompareDirectoryPath(string path1, string path2)
        {
            List<string> itemsInPath1 = Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)).ToList();

            List<string> itemsInPath2 = Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)).ToList();

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            return commonItems;
        }

        //method that loads backup folders to the rollback combobox
        public void LoadBackupOptions(string backupFolder, ComboBox comboBox, Button btn)
        {
            var backups = Directory.GetDirectories(backupFolder)
            .OrderByDescending(Directory.GetCreationTime)
            .Select(dir => new { Name = Path.GetFileName(dir), Path = dir })
            .ToList();

            if (backups.Count == 0)
            {
                comboBox.ItemsSource = new List<string> { "No backups found" };
                comboBox.SelectedIndex = 0;
                btn.IsEnabled = false;
                return;
            }

            comboBox.ItemsSource = backups;
            comboBox.DisplayMemberPath = "Name";
            comboBox.SelectedValuePath = "Path";
            comboBox.SelectedIndex = 0;

        }

        //method that perfom backup directory clean up
        public void CleanupBackups(string backupFolderPath, int keepBackupsCount, string applicationType)
        {
            var backups = Directory.GetDirectories(backupFolderPath).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count > keepBackupsCount)
            {
                MessageBoxResult result = GetMessageBoxResult($"Do you want to cleanup backups for {applicationType}?");
                if (result == MessageBoxResult.Yes)
                {

                    MessageBoxResult result2 = GetMessageBoxResult($"This will keep the most recent {keepBackupsCount} backups");

                    if (result2 == MessageBoxResult.Yes)
                    {

                        var backupsToDelete = GetUnwantedBackups(backups, keepBackupsCount);
                        List<string> backupToDeletePaths = backupsToDelete.Item1;
                        int backupToDeleteCount = backupsToDelete.Item2;

                        DeleteItems(backupToDeletePaths);

                        ShowMessageBox($"Deleted {backupToDeleteCount} backups", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }

            }

        }

        public (List<string>, int backupsTodelete) GetUnwantedBackups(List<string> backups, int keepBackupsCount)//1
        {
            backups = backups.Skip(keepBackupsCount).ToList();

            int backupsToDelete = backups.Count;

            return (backups, backupsToDelete);
        }

        public void ShowMessageBox(string msgBoxText, string msgBoxCaption, MessageBoxButton msgBoxBtn, MessageBoxImage msgBoxImage)
        {
            MessageBox.Show(msgBoxText, msgBoxCaption, msgBoxBtn, msgBoxImage);
        }

        public MessageBoxResult GetMessageBoxResult(string messageBoxText)
        {
            MessageBoxResult result = MessageBox.Show(messageBoxText, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result;
        }

        //method that adds labels for destinations on the UI
        public void AddDestinationLabels(List<Destinations> destinationFolders, StackPanel stackPanel)
        {
            stackPanel.Children.Clear();

            foreach (var destination in destinationFolders)
            {
                var label = new Label
                {
                    Content = $"Name: {destination.name}\nPath: {destination.path}",
                    FontSize = 15,
                };
                stackPanel.Children.Add(label);
            }
        }

        //method that clears labels on UI
        public void ClearLabels(Label txtCopyCount, Label txtBackupCount, Label txtReplacedCount)
        {
            txtCopyCount.Content = string.Empty;
            txtBackupCount.Content = string.Empty;
            txtReplacedCount.Content = string.Empty;
        }

        //methods that returns the path of specific backup
        public string GetBackupPath(string backupFolderPath, DateTime backupStamp)
        {
            string backupPath = Path.Combine(backupFolderPath, BackupFolderName + backupStamp.ToString("_yyyy-MM-dd_HH-mm-ss"));
            return backupPath;
        }

        //methods that copy source to destination and update counts
        public (int createdFolderCount, int createdFileCount) CopySourceToDestination(string sourceFolder, List<Destinations> destinationFolders)//1
        {
            int createdFolderCount = 0;
            int createdFileCount = 0;

            bool isFirstIteration = true;

            foreach (var destination in destinationFolders)
            {
                string destinationPath = destination.path;
                var copyCounts = CopyDirectory(sourceFolder, destinationPath, isFirstIteration);
                if (isFirstIteration)
                {
                    createdFolderCount = copyCounts.createdFolderCount;
                    createdFileCount = copyCounts.createdFileCount;
                }
                isFirstIteration = false;
            }

            return (createdFolderCount, createdFileCount);
        }

        public (int createdFolderCount, int createdFileCount) CopyDirectory(string originDir, string targetDir, bool isFirstIteration)//2
        {
            int createdFolderCount = 0, createdFileCount = 0;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                if (isFirstIteration)
                {
                    createdFolderCount++;
                }
            }

            foreach (string file in Directory.GetFiles(originDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                if (!File.Exists(destFile)) { if (isFirstIteration) { createdFileCount++; } }
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(originDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(targetDir, subDirName);
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                    if (isFirstIteration)
                    {
                        createdFolderCount++;
                    }
                }
                var result = CopyDirectory(subDir, destSubDir, isFirstIteration);
                createdFolderCount += result.createdFolderCount;
                createdFileCount += result.createdFileCount;
            }

            return (createdFolderCount, createdFileCount);
        }

        public (int backupFolderCount, int backupFileCount) BackupFiles(string sourceDir, string destDir, string item, string backupLogFile, bool isFirstIteration)//3
        {
            int backupFolderCount = 0, backupFileCount = 0;

            string sourcePath = Path.Combine(sourceDir, item);

            string destPath = Path.Combine(destDir, item);

            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                    string logEntry = $"Backed up Folder {item} from {sourceDir} to {destDir} \n\n";
                    Log(backupLogFile, logEntry);
                    if (isFirstIteration)
                    {
                        backupFolderCount++;
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                string logEntry = $"Backed up File {item} from {sourceDir} to {destDir} \n\n";
                Log(backupLogFile, logEntry);
                if (isFirstIteration)
                {
                    backupFileCount++;
                }
            }

            return (backupFolderCount, backupFileCount);
        }

        private void Log(string logFilePath, string logEntry)
        {
            File.AppendAllText(logFilePath, logEntry);
        }

        //methods that backup and empty the source folder
        public void CreateBackupSource(string sourceFolder, string backupFolder, DateTime backupStamp)
        {
            string backupPath = GetBackupPath(backupFolder, backupStamp);

            string backupSubFolder = Path.Combine(backupFolder, backupPath, Path.GetFileName(sourceFolder));

            if (!Directory.Exists(backupSubFolder))
            {
                Directory.CreateDirectory(backupSubFolder);
            }
            BackupSource(sourceFolder, backupSubFolder);
        }

        public void BackupSource(string sourceFolder, string destinationFolder)
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

        //methods that rollback backups to destination folders
        public void Rollback(string backupFolderPath, string backupFolder, List<Destinations> destinationFolders)
        {
            string backupPath = Path.Combine(backupFolder, DestinationFolderName);
            string backupFolderName = Path.GetFileName(backupFolder);
            string logPath = Path.Combine(backupFolderPath, RollbackFile);
            string rollbackDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, "ROLLBACK LOG:\n\n-----------------------------------------------------------------\n");
            }
            Log(logPath, $"Rollback of {backupFolderName} on {rollbackDateTime}: \n-----------------------------------------------------------------\n\n");

            foreach (var destination in destinationFolders)
            {
                string rollbackPath = Path.Combine(backupPath, destination.name);
                string destPath = destination.path;

                if (Directory.Exists(rollbackPath))
                {
                    var commonItems = CompareDirectoryPath(rollbackPath, destPath);
                    RollBackItems(backupFolderPath, rollbackPath, destPath, commonItems);
                }
            }
            Log(logPath, $"\n-----------------------------------------------------------------\n");
            ShowMessageBox($" Rolled backup {backupFolderName} back to {DestinationFolderName}", "Rollback Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void RollBackItems(string backUpFldrPth, string sourceDir, string destDir, List<string> commonFiles)
        {
            string logPath = Path.Combine(backUpFldrPth, RollbackFile);

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

        public (int deletedFolderCount, int deletedFileCount) DeleteItems(List<string> itemsToDelete)//4
        {

            int deletedFolderCount = 0, deletedFileCount = 0;

            foreach (var item in itemsToDelete)
            {

                if (File.Exists(item))
                {
                    File.Delete(item);
                    deletedFileCount++;
                }
                else if (Directory.Exists(item))
                {
                    (int folderDeletedFiles, int folderDeletedFolders) = DeleteDirectory(item);
                    deletedFileCount += folderDeletedFiles;
                    deletedFolderCount += folderDeletedFolders + 1;
                }

            }

            return (deletedFolderCount, deletedFileCount);
        }

        private (int deletedFileCount, int deletedFolderCount) DeleteDirectory(string directoryPath)//5
        {
            int fileCount = 0, folderCount = 0;

            foreach (var file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
                fileCount++;
            }

            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                (int subFileCount, int subFolderCount) = DeleteDirectory(dir);
                fileCount += subFileCount;
                folderCount += subFolderCount + 1;
            }

            Directory.Delete(directoryPath); 
            return (fileCount, folderCount);
        }
    }
}
