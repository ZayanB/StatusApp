using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    }
}
