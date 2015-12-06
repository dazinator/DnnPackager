using DnnPackager.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace DnnPackager.Tasks
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

        [Required]
        public string IntermediateOutputPath { get; set; }
        

        public ITaskItem[] ManifestFileProjectItems { get; set; }

        [Output]
        public ITaskItem ManifestFileItem { get; set; }    

        [Output]
        public string ManifestFileNameWithoutExtension { get; set; }


        public override bool ExecuteTask()
        {

            if (!Directory.Exists(ProjectDirectory))
            {
                throw new ArgumentException("The project directory was not found. There is no such directory: " + ProjectDirectory);
            }

            // find dnn manifest file in project items.
            if (ManifestFileProjectItems != null && ManifestFileProjectItems.Length > 0)
            {
                ManifestFileItem = SelectDefaultManifestFile(ManifestFileProjectItems, Configuration);
                if (ManifestFileItem != null)
                {
                    // ManifestFilePath = ManifestFileItem.GetFullPath(ProjectDirectory);
                    ManifestFileNameWithoutExtension = ManifestFileItem.GetFileNameWithoutExtension(ProjectDirectory);
                    return true;
                }
            }

            LogMessage("Unable to find a dnn manifest file in project items. Will check project directory.");

            // find manifest file on disk in project directory (it may not be added to the project)         
            var dirInfo = new DirectoryInfo(ProjectDirectory);
            var files = dirInfo.EnumerateFiles();
            //  dirInfo.EnumerateFiles("",SearchOption.TopDirectoryOnly)
            var manifestFiles = files.Where(f => f.Extension.ToLowerInvariant() == DnnManifestExtension).ToList();

            if (!manifestFiles.Any())
            {
                throw new FileNotFoundException("Could not locate a dnn manifest file within the project's directory. Have you added a .dnn manifest file to the project? " + ProjectDirectory);
            }

            FileInfo manifestFileInfo = SelectDefaultManifestFile(manifestFiles, Configuration);
            ManifestFileItem = new TaskItem(manifestFileInfo.Name);
            //ManifestFilePath = manifestFileInfo.FullName;
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

        private ITaskItem SelectDefaultManifestFile(ITaskItem[] manifestFiles, string Configuration)
        {
            if (manifestFiles.Length == 1)
            {
                return manifestFiles.Single();
            }

            ITaskItem[] candidates = null;

            // If a build configuration is specified, 
            // select only the subset of the manifest files that contain the build config name in it's dot segmented file name.
            if (!string.IsNullOrWhiteSpace(Configuration))
            {
                var buildConfigSpecificManifests = manifestFiles.Where(a => DotSeperateName(a.GetFileNameWithoutExtension(ProjectDirectory)).Any(n => n.ToLowerInvariant() == Configuration.ToLowerInvariant())).ToArray();
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
            ITaskItem chosenManifestFile = null;
            if (candidates.Length == 1)
            {
                chosenManifestFile = candidates.Single();
            }
            else
            {
                int shortestNameLength = int.MaxValue;
                foreach (var item in candidates)
                {
                    var nameLength = DotSeperateName(item.ItemSpec).Length;
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
            var nameSplit = DotSeperateName(a.Name);
            return nameSplit;
        }

        private string[] DotSeperateName(string name)
        {
            var nameSplit = name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return nameSplit;
        }

        private string[] SeperateItemSpecPath(string name)
        {

            var nameSplit = name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return nameSplit;
        }





    }


}
