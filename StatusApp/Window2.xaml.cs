using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace StatusApp
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    /// 

    public class FileSystemItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }

        public FileSystemItem Parent { get; set; }

        public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();
        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));

                    foreach (var child in Children)
                    {
                        child.IsSelected = value;
                    }

                    if (!_isSelected)
                    {
                        Parent?.DeselectIfAnyChildIsDeselected();
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DeselectIfAnyChildIsDeselected()
        {
            if (Children.Any(child => !child.IsSelected))
            {
                _isSelected = false;
                OnPropertyChanged(nameof(IsSelected));
                Parent?.DeselectIfAnyChildIsDeselected();
            }
        }

        public ObservableCollection<FileSystemItem> LoadDirectory(string path, FileSystemItem parent = null)
        {
            var items = new ObservableCollection<FileSystemItem>();
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirItem = new FileSystemItem
                    {

                        Name = System.IO.Path.GetFileName(dir),
                        Path = dir,
                        IsDirectory = true,
                        IsSelected = false,
                        Parent = parent
                    };

                    dirItem.Children = LoadDirectory(dir, dirItem);
                    items.Add(dirItem);
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    items.Add(new FileSystemItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Path = file,
                        IsDirectory = false,
                        IsSelected = false,
                        Parent = parent

                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return items;
        }

        public List<FileSystemItem> GetSelectedItems(ObservableCollection<FileSystemItem> items)
        {
            var selectedItems = new List<FileSystemItem>();

            foreach (var item in items)
            {
                if (item.IsSelected)
                {
                    selectedItems.Add(item);
                }

                // Recursively check child items
                if (item.IsDirectory && item.Children.Any())
                {
                    selectedItems.AddRange(GetSelectedItems(item.Children));
                }
            }

            return selectedItems;
        }

        public List<string> GetUnwantedItems(List<FileSystemItem> selectedItems, List<string> destinationPaths, string basePath)
        {
            var unwantedItems = new List<string>();

            foreach (var item in selectedItems)
            {
                foreach (var destinationPath in destinationPaths)
                {
                    string fullPath = item.Path.Replace(basePath, destinationPath);
                    if (File.Exists(fullPath)) { unwantedItems.Add(fullPath); }
                    else if (Directory.Exists(fullPath)) { unwantedItems.Add(fullPath); }
                }
            }
            return unwantedItems;
        }
    }
    public partial class Window2 : Window
    {
        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config2.json");

        private static readonly string BackupFolderName = "Backup";
        private static readonly string DestinationFolderName = "Destination";
        private static readonly string RollbackFile = "Rollback Log.txt";

        private int BackupFolderCount = 0;
        private int BackupFileCount = 0;
        private int DeletedFolderCount = 0;
        private int DeletedFileCount = 0;
        private int CreatedFileCount = 0;
        private int CreatedFolderCount = 0;

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;

        private static string ApplicationChoice;

        private DeploymentMethods deploymentMethods = new DeploymentMethods();
        private FileSystemItem fileSystemItem = new FileSystemItem();
        private ConfigManager configManager = new ConfigManager();
        private dynamic ConfigData;
        List<string> UnwantedItems;

        public Window2()
        {
            try
            {
                InitializeComponent();

                if (File.Exists(ConfigFilePath))
                {

                    configManager.LoadConfig(ConfigFilePath);
                    LoadApplicationOptions();
                    IsAppLoaded = true;
                    this.Loaded += Window2_Loaded;

                    LoadDirectoryForUI();

                }
                else
                {
                    MessageBox.Show($"Configuration File not found at {ConfigFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadApplicationOptions()
        {
            var applicationOptions = configManager.Config.Applications.Keys.ToList();
            applicationDropdown.ItemsSource = applicationOptions;
            applicationDropdown.SelectedIndex = 0;
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!deploymentMethods.IsDirectoryEmpty(ConfigData.sourceFolder))
                {

                    DateTime timestamp = deploymentMethods.CreateBackupInstance(ConfigData);

                    BackupDestination(timestamp);

                    DeleteItems(UnwantedItems);

                    deploymentMethods.CopySourceToDestination(ConfigData, txtCopyCount, ref CreatedFolderCount, ref CreatedFileCount);

                    deploymentMethods.CreateBackupSource(ConfigData, timestamp); //Comment to not empty source

                    BackupFolderCount = 0;
                    BackupFileCount = 0;
                    CreatedFolderCount = 0;
                    CreatedFileCount = 0;
                    DeletedFileCount = 0;
                    DeletedFolderCount = 0;

                    LoadDirectoryForUI();

                    if (UnwantedItems != null && UnwantedItems.Count != 0)
                    {
                        UnwantedItems.Clear();
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed. {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        private void BackupDestination(DateTime timestamp)
        {
            string backupPath = deploymentMethods.GetBackupPath(ConfigData, timestamp);

            string destinationBackupFolder = Path.Combine(backupPath, DestinationFolderName);

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                if (!Directory.Exists(destinationBackupFolder))
                {
                    Directory.CreateDirectory(destinationBackupFolder);
                }
                string specificBackupFolder = Path.Combine(destinationBackupFolder, destination.name);
                if (!Directory.Exists(specificBackupFolder))
                {
                    Directory.CreateDirectory(specificBackupFolder);
                }
                BackupAllItems(destinationPath, specificBackupFolder, timestamp);
            }
        }

        private void LoadDirectoryForUI()
        {
            string directoryPath = ConfigData.destinationFolders[0].path;
            if (Directory.Exists(directoryPath))
            {
                var directoryItems = fileSystemItem.LoadDirectory(directoryPath, null);
                DirectoryTreeView.ItemsSource = directoryItems;
            }
        }

        private void DeleteItems(List<string> itemsToDelete)
        {
            if (UnwantedItems == null || UnwantedItems.Count == 0)
            {
                txtDeleteCount.Content = $" No Items Deleted ";
            }
            else
            {
                foreach (var item in itemsToDelete)
                {
                    try
                    {
                        if (File.Exists(item))
                        {
                            File.Delete(item);
                            DeletedFileCount++;
                        }
                        else if (Directory.Exists(item))
                        {
                            Directory.Delete(item, true);
                            DeletedFolderCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting {item}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    txtDeleteCount.Content = $" Deleted {DeletedFolderCount} Folders & {DeletedFileCount} Files ";
                }
            }
        }

        private void BackupAllItems(string sourceDir, string destDir, DateTime backupStamp)
        {
            string backupPath = deploymentMethods.GetBackupPath(ConfigData, backupStamp);

            string backupDateTime = "Backup " + backupStamp.ToString("yyyy-MM-dd_HH:mm:ss");

            string backupLogFile = Path.Combine(backupPath, "Backup Log.txt");

            if (!File.Exists(backupLogFile))
            {
                File.WriteAllText(backupLogFile, $"{backupDateTime} Log: \n------------------------------------------------------------------------------------------------------------------\n\n");
            }

            foreach (var destination in ConfigData.destinationFolders)
            {
                string destinationPath = destination.path;

                List<string> allDestinationItems = Directory.EnumerateFileSystemEntries(destinationPath, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(destinationPath, path)).ToList();

                foreach (var item in allDestinationItems)
                {
                    deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, ref BackupFolderCount, ref BackupFileCount);
                }
            }
            txtBackupCount.Content = $" Backed Up {BackupFolderCount} Folders & {BackupFileCount} Files ";

        }

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            rollbackPopup.IsOpen = true;

            deploymentMethods.LoadBackupOptions(ConfigData, BackupDropdown, performRollbackBtn);
        }

        private void performRollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            string rollbackPath = BackupDropdown.SelectedValue.ToString();

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to rollback backup {Path.GetFileName(rollbackPath)} back to {DestinationFolderName}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                deploymentMethods.Rollback(ConfigData, rollbackPath);
            }
        }

        private void applicationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationChoice = applicationDropdown.SelectedItem.ToString();
            SkipInitialChange = false;

            ConfigData = configManager.Config.Applications[ApplicationChoice];

            deploymentMethods.ClearLabels(txtCopyCount, txtBackupCount, txtDeleteCount);
            SourceFolderLabel.Content = ConfigData.sourceFolder;
            BackupFolderLabel.Content = ConfigData.backupFolder;
            deploymentMethods.AddDestinationLabels(ConfigData, DestinationLabelsPanel);

            LoadDirectoryForUI();

            bool checkFolders = deploymentMethods.CheckFolders(ConfigData);
            if (!checkFolders) { Application.Current.Shutdown(); }

            if (IsAppLoaded) { deploymentMethods.CleanupBackups(ConfigData, ApplicationChoice); }
        }

        private void Window2_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SkipInitialChange)
            {
                deploymentMethods.CleanupBackups(ConfigData, ApplicationChoice);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            string basePath = ConfigData.destinationFolders[0].path;

            List<string> destinationPaths = new List<string>();

            foreach (var dest in ConfigData.destinationFolders)
            {
                string destinationPath = dest.path;
                destinationPaths.Add(destinationPath);
            }

            var selectedItems = fileSystemItem.GetSelectedItems(DirectoryTreeView.ItemsSource as ObservableCollection<FileSystemItem>);

            UnwantedItems = fileSystemItem.GetUnwantedItems(selectedItems, destinationPaths, basePath);

        }

    }
}
