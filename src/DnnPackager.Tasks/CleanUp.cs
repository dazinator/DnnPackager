using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Build.Utilities;

namespace DnnPackager.Tasks
{

    public class CleanUp : AbstractTask
    {
        public const string IntermediateOutputFolderName = "DnnPackager";

        public CleanUp()
        {

        }

        [Required]
        public string OutputDirectory { get; set; }      

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }


        public override bool ExecuteTask()
        {
            var packagingDir = CleanIntermediateOutputDirectory(IntermediateOutputFolderName);
            return true;
        }


        private string CleanIntermediateOutputDirectory(string name)
        {
            var temp = Path.Combine(IntermediateOutputPath, name);
            LogMessage("Cleaning directory: " + temp, MessageImportance.Low);
            EnsureEmptyDirectory(temp);
            //_fileSystem.EnsureDiskHasEnoughFreeSpace(temp);
            return temp;
        }

        private void EnsureEmptyDirectory(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                System.IO.DirectoryInfo dir = new DirectoryInfo(dirPath);
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    d.Delete(true);
                }
            }

            Directory.CreateDirectory(dirPath);
            LogMessage("Created directory: " + dirPath, MessageImportance.Low);
        }

    }
}
