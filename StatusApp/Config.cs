using System.Text.Json;
using System.IO;

namespace StatusApp
{
    public class AppConfigRoot
    {
        public Dictionary<string, ApplicationConfig> Applications { get; set; }

    }

    public class ApplicationConfig
    {
        public string sourceFolder { get; set; }

        public List<Destinations> destinationFolders { get; set; }

        public string backupFolder { get; set; }

        public int keepBackupsCount { get; set; }
        public List<string> filesToKeep { get; set; }
        public List<string> foldersToKeep { get; set; }


    }

    public class ConfigManager
    {
        public AppConfigRoot Config { get; private set; }

        public void LoadConfig(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            Config = JsonSerializer.Deserialize<AppConfigRoot>(jsonString, options);
        }
    }

    public class Destinations
    {
        public string name { get; set; }
        public string path { get; set; }
    }
}