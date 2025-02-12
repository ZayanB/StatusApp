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

        public bool CheckSourceFolder(ApplicationConfig appConfig)
        {
            string sourcePath = appConfig.sourceFolder;
            if (!Directory.EnumerateFileSystemEntries(sourcePath).Any())
            {
                MessageBox.Show($"Source Folder is Empty. Operation can't be completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
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

        public string GetBackupName(ApplicationConfig appConfig, DateTime backupStamp,string BackupFolderName)
        {
            string backupFolderPath = appConfig.backupFolder;
            string backupPath = Path.Combine(backupFolderPath, BackupFolderName + backupStamp.ToString("_yyyy-MM-dd_HH-mm-ss"));
            return backupPath;
        }

        //private void CopyOriginToTarget(ApplicationConfig appConfig, dynamic originPath, dynamic targetPath, int CreatedFolderCount, int CreatedFileCount)
        //{
        //    string sourceFolder = FolderPaths.sourceFolder;

        //    foreach (var destination in FolderPaths.destinationFolders)
        //    {
        //        string destinationPath = destination.path;
        //        CopyDirectory(sourceFolder, destinationPath);
        //    }
        //}
    }
}
