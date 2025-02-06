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
    public class Destination
    {
        public string name { get; set; }
        public string path { get; set; }
    }
    public class Config
    {
        public string sourceFolder { get; set; }
        public List<Destination> destinationFolders { get; set; }
        public string backupFolder { get; set; }
   
        public int keepBackupsCount { get; set; }

    }
}