using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Configuration;
using System.IO;

namespace StatusApp
{
    public class Destinations
    {
        public string name { get; set; }
        public string path { get; set; }
    }

    public class AppConfigRoot
    {
        public string DefaultApp { get; set; }
        public Dictionary<string, ApplicationConfig> Applications { get; set; }

    }

    public class ApplicationConfig
    {
        public string sourceFolder { get; set; }

        public List<Destinations> destinationFolders { get; set; }

        public string backupFolder { get; set; }

        public int keepBackupsCount { get; set; }

    }

    public class ConfigManager
    {
        public static AppConfigRoot Config { get; set; }

        public static void  LoadConfig(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                Config = JsonSerializer.Deserialize<AppConfigRoot>(jsonString, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load operation", ex);
            }
        }
    }
}