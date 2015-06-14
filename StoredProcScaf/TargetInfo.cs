using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoredProcScaf
{
    public class TargetInfo
    {
        public TargetInfo(string fileName, string path, string fullPath)
        {
            FileName = fileName;
            Path = path;
            FullPath = fullPath;
        }

        public string FileName { get; set; }
        public string Path { get; set; }
        public string FullPath { get; set; }

    }
}
