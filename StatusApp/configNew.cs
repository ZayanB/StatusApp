using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusApp
{
    public class configNew
    {
        public string tempFolderPath { get; set; }
        public List<string> unwantedExtensions { get; set; }

        public int deleteOldFoldersByDays { get; set; }

        public int executionTimerInterval { get; set; }

    }
}
