using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace StatusApp
{
    public class Destination
    {
        public string name { get; set; }
        public string path { get; set; }
    }
    public class Config2
    {
        public string sourceFolder { get; set; }
        public string backupFolder { get; set; }
        public List<Destination> destinations { get; set; }
        public int keepBackupsCount { get; set; }
        public List<string> filesToKeep { get; set; }
        public List<string> foldersToKeep { get; set; }
  
    }



}

