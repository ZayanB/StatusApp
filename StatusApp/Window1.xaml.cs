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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public configNew ConfigData { get; set; }

        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigFilePath = Path.Combine(AppDirectory, "tempConfig.json");

        private static Timer repeatTask;

        public Window1()
        {
            //if (File.Exists(ConfigFilePath))
            //{
            //    string jsonString = File.ReadAllText(ConfigFilePath);

            //    ConfigData = JsonSerializer.Deserialize<configNew>(jsonString);
            //    int executionTimerInterval = ConfigData.executionTimerInterval;

            //    int executionTimerInMilliseconds = (int)TimeSpan.FromDays(executionTimerInterval).TotalMilliseconds;

            //    repeatTask = new Timer(new TimerCallback(PerformTask), null, 0, executionTimerInMilliseconds);
            //}
            //else
            //{
            //    Console.WriteLine($"Configuration File not found at {ConfigFilePath}");
            //    Application.Current.Shutdown();
            //}

            PerformTask();
        }

        //private void PerformTask(object state)
        private void PerformTask()
        {
            try
            {
                InitializeComponent();

                List<string> allTempFolders = GetAllItems();

                List<string> oldFolders = FilterOldFolders(allTempFolders);

                List<string> nameGuidFolders = FilterByName(oldFolders);

                List<string> unwantedFolders = FilterByExtension(nameGuidFolders);

                //DeleteFolders(unwantedFolders);

                foreach (string folder in unwantedFolders)
                {
                    Console.WriteLine(folder);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Application.Current.Shutdown();
            }
        }
        //method to get all folders
        private List<string> GetAllItems()
        {
            string tempFolderPath = ConfigData.tempFolderPath;

            List<string> tempFolders = Directory.GetDirectories(tempFolderPath).ToList();

            return tempFolders;
        }

        //method to get old folders only
        private List<string> FilterOldFolders(List<string> folders)
        {
            int filterValue = ConfigData.deleteOldFoldersByDays;

            DateTime deleteDate = DateTime.Now.AddDays(-filterValue);

            var oldFolders = folders.Where(dir => Directory.GetCreationTime(dir) < deleteDate).ToList();

            return oldFolders;
        }

        //method to fiter guid folders
        private List<string> FilterByName(List<string> folders)
        {
            var guidFolders = folders.Where(path =>
            {
                string folderName = Path.GetFileName(path);
                return folderName.StartsWith("{") && folderName.EndsWith("}") && Guid.TryParse(folderName.Trim('{', '}'), out _);
            }).ToList();

            return guidFolders;

        }

        //methods to filter guid folder that contain specific extensions or empty
        private List<string> FilterByExtension(List<string> folders)
        {
            var unwantedExtensions = ConfigData.unwantedExtensions;

            var unwantedFolders = folders.Where(dir => IsDirectoryUnwanted(dir, unwantedExtensions)).ToList();

            return unwantedFolders;
        }

        private bool IsDirectoryUnwanted(string directoryPath, List<string> unwantedExtensions)
        {
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any()) { return true; }

            bool isUnwanted = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).Any(file => unwantedExtensions.Contains(Path.GetExtension(file).TrimStart('.'), StringComparer.OrdinalIgnoreCase));

            return isUnwanted;

        }

        //method to delete unwanted folders 
        private void DeleteFolders(List<string> folders)
        {
            foreach (string dir in folders)
            {
                Directory.Delete(dir, true);
            }

        }
    }

}
