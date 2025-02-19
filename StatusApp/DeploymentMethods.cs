﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace StatusApp
{
    public class DeploymentMethods
    {
        public static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string RollbackFile = "Rollback Log.txt";

        //method to check if directories exist at paths
        public bool CheckFolders(string sourceFolder, string backupFolder, List<Destinations> destinationFolders)
        {
            if (!Directory.Exists(sourceFolder))
            {
                MessageBox.Show($"Source  is not existing at: {sourceFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!Directory.Exists(backupFolder))
            {
                MessageBox.Show($"backup is not existing at: {backupFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            foreach (var destination in destinationFolders)
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

        //method to check if a directory is empty
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

        //method to create backup directory with specific date & time
        public DateTime CreateBackupInstance(string backupFolder)
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
        public void CopySourceToDestination(string sourceFolder, List<Destinations> destinationFolders, Label label, ref int createdFolderCount, ref int createdFileCount)
        {
            bool isFirstIteration = true;

            foreach (var destination in destinationFolders)
            {
                string destinationPath = destination.path;


                label.Content = CopyDirectory(sourceFolder, destinationPath, ref createdFolderCount, ref createdFileCount, isFirstIteration);

                isFirstIteration = false;

            }
        }

        public string CopyDirectory(string originDir, string targetDir, ref int folderCount, ref int fileCount, bool isFirstIteration)
        {

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                if (isFirstIteration)
                {
                    folderCount++;
                }
            }

            foreach (string file in Directory.GetFiles(originDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                if (!File.Exists(destFile)) { if (isFirstIteration) { fileCount++; } }
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
                        folderCount++;
                    }
                }
                CopyDirectory(subDir, destSubDir, ref folderCount, ref fileCount, isFirstIteration);
            }

            return fileCount > 0 || folderCount > 0 ? $" Created {folderCount} Folders & {fileCount} Files" : " All files are similar. No new files to create ";
        }

        //method that backup destination to backup folders and update counts
        public void BackupFiles(string sourceDir, string destDir, string item, string backupLogFile, ref int backupFolderCount, ref int backupFileCount, bool isFirstIteration)
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
        }

        //method that appends text to log files
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
            MessageBox.Show($" Rolled backup {backupFolderName} back to {DestinationFolderName}", "Rollback Success", MessageBoxButton.OK);
        }

        private void RollBackItems(string bckUpFldrPth, string sourceDir, string destDir, List<string> commonFiles)
        {
            string logPath = Path.Combine(bckUpFldrPth, RollbackFile);

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
