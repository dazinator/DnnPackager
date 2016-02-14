using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        [Required]
        public ITaskItem[] ManifestFileProjectItems { get; set; }

        [Output]
        public ITaskItem[] ManifestFileItemsForPackage { get; set; }

        [Output]
        public ITaskItem DefaultManifestFileItemForPackage { get; set; }


        public override bool ExecuteTask()
        {
            if (!Directory.Exists(ProjectDirectory))
            {
                throw new ArgumentException("The project directory was not found. There is no such directory: " + ProjectDirectory);
            }

            // find dnn manifest file in project items.
            if (ManifestFileProjectItems != null && ManifestFileProjectItems.Length > 0)
            {
                ManifestFileItemsForPackage = ChooseManifestFiles(ManifestFileProjectItems, Configuration);
                var orderedByShortestFileExtension = ManifestFileItemsForPackage.OrderBy(a => (a.GetFileExtension().Length));
                DefaultManifestFileItemForPackage = orderedByShortestFileExtension.FirstOrDefault();

                if (DefaultManifestFileItemForPackage != null)
                {
                    return true;
                }
            }

            LogMessage("Unable to find a dnn manifest file in project items.");
            return false;
        }

        private ITaskItem[] ChooseManifestFiles(ITaskItem[] manifestFiles, string Configuration)
        {
            if (manifestFiles.Length == 1)
            {
                return manifestFiles;
            }

            ITaskItem[] overrides = new ITaskItem[] { };

            // Split manifest files into 2 groups. Those that contain the build configuration in the dot segmented file name and those that don't.           
            if (!string.IsNullOrWhiteSpace(Configuration))
            {
                var buildConfigSpecificManifests = manifestFiles.Where(a => DotSeperateName(a.GetFileNameWithoutExtension(ProjectDirectory)).Any(n => n.ToLowerInvariant() == Configuration.ToLowerInvariant())).ToArray();
                if (buildConfigSpecificManifests.Any())
                {
                    overrides = buildConfigSpecificManifests;
                }
            }

            // get non build specific dnn files
            var defaults = manifestFiles.Except(overrides);

            // get distinct list of file extensions. i.e dnn, dnn6, dnn7
            var distinctFileExtensions = manifestFiles.Select(a => a.GetFileExtension()).Distinct().ToArray();

            ITaskItem[] chosenManifests = new ITaskItem[distinctFileExtensions.Length];

            // for each file extension, select the manifest to use, build specific ones should take precedence over non build specific ones.
            int index = 0;
            foreach (var manifestFileExtension in distinctFileExtensions)
            {
                var manifest = ChooseManifestWithExtension(manifestFileExtension, overrides, defaults);
                chosenManifests[index] = manifest;
                index = index + 1;

                if (manifest == null)
                {
                    // should never happen.
                    Debugger.Break();
                }

            }

            return chosenManifests;

        }

        private ITaskItem ChooseManifestWithExtension(string extension, ITaskItem[] overrides, IEnumerable<ITaskItem> defaults)
        {
            var invariantExtension = extension.ToLowerInvariant();
            ITaskItem result = ChooseShortestDotSegmentedItem(overrides.Where(a => a.GetFileExtension().ToLowerInvariant() == invariantExtension));
            if (result == null)
            {
                result = ChooseShortestDotSegmentedItem(defaults.Where(a => a.GetFileExtension().ToLowerInvariant() == invariantExtension));
            }
            return result;
        }

        private ITaskItem ChooseShortestDotSegmentedItem(IEnumerable<ITaskItem> items)
        {

            // Select the item with the shortest number of dot seperated segments in the filename.
            ITaskItem chosenManifestFile = null;
            int shortestNameLength = int.MaxValue;
            foreach (var item in items)
            {
                var nameLength = DotSeperateName(item.ItemSpec).Length;
                if (nameLength < shortestNameLength)
                {
                    chosenManifestFile = item;
                    shortestNameLength = nameLength;
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
