using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StatusApp
{
    public class DeploymentMethods
    {
        private static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string SourceFolderName = "Source";
        private static readonly string RollbackFile = "Rollback Log.txt";

        public bool CheckFolders(ApplicationConfig appConfig)
        {
            if (!Directory.Exists(appConfig.sourceFolder))
            {
                MessageBox.Show($"Source  is not existing at: {appConfig.sourceFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Directory.Exists(appConfig.backupFolder))
            {
                MessageBox.Show($"backup is not existing at: {appConfig.backupFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            foreach (var destination in appConfig.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationPath))
                {
                    MessageBox.Show($"destination is not existing at: {destinationPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        public bool IsDirectoryEmpty(string directoryPath)
        {
            string directoryName = Path.GetFileName(directoryPath);
      
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                MessageBox.Show($"{directoryName} is Empty.Operation can't be completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            return false;
        }

        public DateTime CreateBackupInstance(ApplicationConfig appConfig)
        {
            string backupFolder = appConfig.backupFolder;

            DateTime currentTime = DateTime.Now;
            string backupFolderSub = "Backup_" + currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFolderPath = Path.Combine(backupFolder, backupFolderSub);

            Directory.CreateDirectory(backupFolderPath);

            return currentTime;
        }

        public List<string> CompareDirectoryPath(string path1, string path2)
        {
            List<string> itemsInPath1 = Directory.EnumerateFileSystemEntries(path1, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path1, path)).ToList();

            List<string> itemsInPath2 = Directory.EnumerateFileSystemEntries(path2, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(path2, path)).ToList();

            List<string> commonItems = itemsInPath1.Intersect(itemsInPath2).ToList();

            return commonItems;
        }

        public void LoadBackupOptions(ApplicationConfig appConfig, ComboBox comboBox, Button btn)
        {
            var backups = Directory.GetDirectories(appConfig.backupFolder)
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

        public void CleanupBackups(ApplicationConfig appConfig, string applicationType)
        {
            string backupFolderPath = appConfig.backupFolder;

            int keepBackupsCount = appConfig.keepBackupsCount;

            var backups = Directory.GetDirectories(backupFolderPath).OrderByDescending(dir => Directory.GetCreationTime(dir)).ToList();

            if (backups.Count > keepBackupsCount)
            {
                MessageBoxResult result = MessageBox.Show($"Do you want to cleanup backups for {applicationType}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

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

        public void AddDestinationLabels(ApplicationConfig appConfig, StackPanel stackPanel)
        {
            stackPanel.Children.Clear();

            foreach (var destination in appConfig.destinationFolders)
            {
                var label = new Label
                {
                    Content = $"Name: {destination.name}\nPath: {destination.path}",
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                stackPanel.Children.Add(label);
            }
        }

        public void ClearLabels(Label txtCopyCount, Label txtBackupCount, Label txtReplacedCount)
        {
            txtCopyCount.Content = string.Empty;
            txtBackupCount.Content = string.Empty;
            txtReplacedCount.Content = string.Empty;
        }

        public string GetBackupName(ApplicationConfig appConfig, DateTime backupStamp)
        {
            string backupFolderPath = appConfig.backupFolder;
            string backupPath = Path.Combine(backupFolderPath, BackupFolderName + backupStamp.ToString("_yyyy-MM-dd_HH-mm-ss"));
            return backupPath;
        }

        public void CopySourceToDestination(ApplicationConfig appConfig, Label label, ref int createdFolderCount, ref int createdFileCount)
        {
            string sourceFolder = appConfig.sourceFolder;

            foreach (var destination in appConfig.destinationFolders)
            {
                string destinationPath = destination.path;

                CopyDirectory(sourceFolder, destinationPath, label, ref createdFolderCount, ref createdFileCount);

            }
        }

        public void CopyDirectory(string originDir, string targetDir, Label label, ref int folderCount, ref int fileCount)
        {

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                folderCount++;
            }

            foreach (string file in Directory.GetFiles(originDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                if (!File.Exists(destFile)) { fileCount++; }
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(originDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(targetDir, subDirName);
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                    folderCount++;
                }
                CopyDirectory(subDir, destSubDir, label, ref folderCount, ref fileCount);
            }


            if (fileCount > 0 || folderCount > 0)
            {
                label.Content = $" Created {folderCount} Folders & {fileCount} Files ";
            }
            else
            { label.Content = " All files are similar. No new files to create "; }

        }

        public void BackupFiles(string sourceDir, string destDir, string item, string backupLogFile, ref int backupFolderCount, ref int backupFileCount)
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
                    backupFolderCount++;
                }
            }
            else if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                string logEntry = $"Backed up File {item} from {sourceDir} to {destDir} \n\n";
                Log(backupLogFile, logEntry);
                backupFileCount++;
            }
        }

        private void Log(string logFilePath, string logEntry)
        {
            File.AppendAllText(logFilePath, logEntry);
        }

        public void CreateBackupSource(ApplicationConfig appConfig, DateTime backupStamp)
        {
            string backupFolder = appConfig.backupFolder;

            string backupPath = GetBackupName(appConfig, backupStamp);

            string sourceFolder = appConfig.sourceFolder;

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

        public void Rollback(ApplicationConfig appConfig, string backupFolder)
        {
            string backupFolderPath = appConfig.backupFolder;
            string backupPath = Path.Combine(backupFolder, DestinationFolderName);
            string backupFolderName = Path.GetFileName(backupFolder);
            string logPath = Path.Combine(backupFolderPath, RollbackFile);
            string rollbackDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, "ROLLBACK LOG:\n\n-----------------------------------------------------------------\n");
            }
            Log(logPath, $"Rollback of {backupFolderName} on {rollbackDateTime}: \n-----------------------------------------------------------------\n\n");

            foreach (var destination in appConfig.destinationFolders)
            {
                string rollbackPath = Path.Combine(backupPath, destination.name);
                string destPath = destination.path;

                if (Directory.Exists(rollbackPath))
                {
                    var commonItems = CompareDirectoryPath(rollbackPath, destPath);
                    RollBackItems(appConfig, rollbackPath, destPath, commonItems);
                }
            }
            Log(logPath, $"\n-----------------------------------------------------------------\n");
            MessageBox.Show($" Rolled backup {backupFolderName} back to {DestinationFolderName}", "Rollback Success", MessageBoxButton.OK);
        }

        private void RollBackItems(ApplicationConfig appConfig, string sourceDir, string destDir, List<string> commonFiles)
        {
            string logPath = Path.Combine(appConfig.backupFolder, RollbackFile);

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


    }
}
