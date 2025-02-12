using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.Json;



namespace StatusApp
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "config2.json");

        private static bool IsAppLoaded = false;
        private static bool SkipInitialChange = true;

        private static string ApplicationChoice;

        private DeploymentMethods deploymentMethods = new DeploymentMethods();
        private dynamic FolderPaths;

        public Config2 ConfigData { get; set; }
        public Window2()
        {
            InitializeComponent();

            if (File.Exists(ConfigFilePath))
            {
                ConfigManager.LoadConfig(ConfigFilePath);
                LoadApplicationOptions();
            }
            else
            {
                Console.WriteLine($"Configuration File not found at {ConfigFilePath}");
                Application.Current.Shutdown();
            }
        }

        private void LoadApplicationOptions()
        {
            var applicationOptions = ConfigManager.Config.Applications.Keys.ToList();
            applicationDropdown.ItemsSource = applicationOptions;
            applicationDropdown.SelectedIndex = 0;

        }
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (deploymentMethods.CheckSourceFolder(FolderPaths))
                {
                    DateTime timestamp = deploymentMethods.CreateBackupInstance(FolderPaths);

                    BackupDestination(timestamp);

                    //CopySourceToDestinations();

                    //CreateBackupSource(timestamp);

                    //BackupFolderCount = 0;
                    //BackupFileCount = 0;
                    //ReplacedFolderCount = 0;
                    //ReplacedFileCount = 0;
                    //CreatedFolderCount = 0;
                    //CreatedFileCount = 0;
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
            //IT IS SAME AS COPY SOURCE TO DESTINATION BUT DIFFERENT
        }

        private void showRollbackBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void performRollbackBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void applicationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationChoice = applicationDropdown.SelectedItem.ToString();
            SkipInitialChange = false;

            FolderPaths = ConfigManager.Config.Applications[ApplicationChoice];

            deploymentMethods.ClearLabels(txtCopyCount, txtBackupCount, txtReplacedCount);
            SourceFolderLabel.Content = FolderPaths.sourceFolder;
            BackupFolderLabel.Content = FolderPaths.backupFolder;
            deploymentMethods.AddDestinationLabels(FolderPaths, DestinationLabelsPanel);


            bool checkFolders = deploymentMethods.CheckFolders(FolderPaths);
            if (!checkFolders) { Application.Current.Shutdown(); }

            if (IsAppLoaded) { deploymentMethods.CleanupBackups(FolderPaths, ApplicationChoice); }

        }
    }
}
