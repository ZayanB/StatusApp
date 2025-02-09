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

        public int DeleteOldFoldersByDays { get; set; }

        public int ExecutionTimerIntervalInDays { get; set; }

    }
}
