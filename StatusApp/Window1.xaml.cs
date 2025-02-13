using System.Windows;
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
            if (File.Exists(ConfigFilePath))
            {
                string jsonString = File.ReadAllText(ConfigFilePath);

                ConfigData = JsonSerializer.Deserialize<configNew>(jsonString);

                int executionTimerInterval = ConfigData.ExecutionTimerIntervalInDays;

                int executionTimerInMilliseconds = (int)TimeSpan.FromDays(executionTimerInterval).TotalMilliseconds;

                repeatTask = new Timer(new TimerCallback(PerformTask), null, 0, executionTimerInMilliseconds);
            }
            else
            {
                Console.WriteLine($"Configuration File not found at {ConfigFilePath}");
                Application.Current.Shutdown();
            }
        }

        private void PerformTask(object state)
        {
            try
            {
                InitializeComponent();

                List<string> foldersToDelete = GetFoldersToDelete();

                DeleteFolders(foldersToDelete);

                foreach (string unwantedFolder in foldersToDelete)
                {
                    Console.WriteLine(unwantedFolder);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Application.Current.Shutdown();
            }
        }

        private List<string> GetFoldersToDelete()
        {
            List<string> folders = GetAllFolders();

            folders = FilterOldFolders(folders);

            folders = FilterByName(folders);

            folders = FilterByExtension(folders);

            return folders;

        }

        //method to get all folders
        private List<string> GetAllFolders()
        {
            string tempFolderPath = ConfigData.tempFolderPath;

            List<string> allFolders = Directory.GetDirectories(tempFolderPath).ToList();

            return allFolders;
        }

        //method to get old folders only
        private List<string> FilterOldFolders(List<string> allfolders)
        {
            int dateFilterValue = ConfigData.DeleteOldFoldersByDays;

            DateTime deleteDate = DateTime.Now.AddDays(-dateFilterValue);

            var oldFolders = allfolders.Where(dir => Directory.GetCreationTime(dir) < deleteDate).ToList();

            return oldFolders;
        }

        //method to fiter guid folders
        private List<string> FilterByName(List<string> folders)
        {
            var namedFolders = folders.Where(path =>
            {
                string folderName = Path.GetFileName(path);
                return folderName.StartsWith("{") && folderName.EndsWith("}") && Guid.TryParse(folderName.Trim('{', '}'), out _);
            }).ToList();

            return namedFolders;
        }

        //methods to filter guid folder that contain specific extensions or empty
        private List<string> FilterByExtension(List<string> folders)
        {
            var unwantedExtensions = ConfigData.unwantedExtensions;

            var foldersToDelete = folders.Where(dir => IsDirectoryUnwanted(dir, unwantedExtensions)).ToList();

            return foldersToDelete;
        }

        private bool IsDirectoryUnwanted(string directoryPath, List<string> unwantedExtensions)
        {
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any()) { return true; }

            bool isUnwanted = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly).Any(file => unwantedExtensions.Contains(Path.GetExtension(file).TrimStart('.'), StringComparer.OrdinalIgnoreCase));

            return isUnwanted;
        }

        //method to delete unwanted folders 
        private void DeleteFolders(List<string> folders)
        {
            foreach (string dir in folders)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting {dir}: {ex.Message}");
                }
            }
        }
    }
}
