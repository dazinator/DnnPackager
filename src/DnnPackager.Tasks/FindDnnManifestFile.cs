using DnnPackager.Tasks;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace DnnPackager
{

    public class FindDnnManifestFile : AbstractTask, ITask
    {

        public const string DnnManifestExtension = ".dnn";

        public FindDnnManifestFile()
        {

        }

        /// <summary>
        /// The projects root directory; set to <code>$(MSBuildProjectDirectory)</code> by default.
        /// </summary>
        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string Configuration { get; set; }

        /// <summary>
        /// The full path to the dotnetnuke manifest file to read.
        /// </summary>
        [Output]
        public string ManifestFilePath { get; set; }

        [Output]
        public string ManifestFileNameWithoutExtension { get; set; }


        public override bool ExecuteTask()
        {

            if (!Directory.Exists(ProjectDirectory))
            {
                throw new ArgumentException("The project directory was not found. There is no such directory: " + ProjectDirectory);
            }

            // TODO: Locate the dnn manifest file.
            var dirInfo = new DirectoryInfo(ProjectDirectory);
            var files = dirInfo.EnumerateFiles();
            //  dirInfo.EnumerateFiles("",SearchOption.TopDirectoryOnly)
            var manifestFiles = files.Where(f => f.Extension.ToLowerInvariant() == DnnManifestExtension).ToList();

            if (!manifestFiles.Any())
            {
                throw new FileNotFoundException("Could not locate a dnn manifest file within the project's directory. Have you added a .dnn manifest file to the project? " + ProjectDirectory);
            }
            
            FileInfo manifestFileInfo = SelectDefaultManifestFile(manifestFiles, Configuration);
            ManifestFilePath = manifestFileInfo.FullName;
            ManifestFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(manifestFileInfo.Name);
            return true;

        }

        private FileInfo SelectDefaultManifestFile(List<FileInfo> manifestFiles, string Configuration)
        {
            if (manifestFiles.Count == 1)
            {
                return manifestFiles.Single();
            }

            List<FileInfo> candidates = null;

            // If a build configuration is specified, 
            // select only the subset of the manifest files that contain the build config name in it's dot segmented file name.
            if (!string.IsNullOrWhiteSpace(Configuration))
            {
                var buildConfigSpecificManifests = manifestFiles.Where(a => DotSeperateName(a).Any(n => n.ToLowerInvariant() == Configuration.ToLowerInvariant())).ToList();
                if (buildConfigSpecificManifests.Any())
                {
                    candidates = buildConfigSpecificManifests;
                }
            }

            // If a build config was not provided, or if it was but no build specific manifests where found, then we will select from all of the manifests found not a subset.
            if (candidates == null)
            {
                candidates = manifestFiles;
            }

            // If more than 1, select the one with the shortest number of dot seperated segments in the filename.
            FileInfo chosenManifestFile = null;
            if (candidates.Count == 1)
            {
                chosenManifestFile = candidates.Single();
            }
            else
            {
                int shortestNameLength = int.MaxValue;
                foreach (var item in candidates)
                {
                    var nameLength = DotSeperateName(item).Length;
                    if (nameLength < shortestNameLength)
                    {
                        chosenManifestFile = item;
                        shortestNameLength = nameLength;
                    }
                }
            }

            return chosenManifestFile;

        }

        private string[] DotSeperateName(FileInfo a)
        {
            var nameSplit = a.Name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return nameSplit;
        }

    }

 
}
