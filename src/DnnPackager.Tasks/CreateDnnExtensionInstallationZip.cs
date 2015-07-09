using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DnnPackager.Util;

namespace DnnPackager.Tasks
{


    public class CreateDnnExtensionInstallationZip : AbstractTask
    {
        private IFileSystem _fileSystem;

        public CreateDnnExtensionInstallationZip()
            : this(new PhysicalFileSystem())
        {

        }

        public CreateDnnExtensionInstallationZip(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            _fileSystem = fileSystem;
        }

        [Required]
        public string ManifestFilePath { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string OutputZipFileName { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        /// <summary>
        /// The list of content files in the project that will be packed up into a ResourcesZip file and included in the
        /// Dnn installation zip package.
        /// </summary>
        [Required]
        public ITaskItem[] ResourcesZipContent { get; set; }

        [Required]
        public ITaskItem[] AdditionalFiles { get; set; }

        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem[] Symbols { get; set; }

        /// <summary>
        /// Used to output the built zip install package.
        /// </summary>
        [Output]
        public ITaskItem InstallPackage { get; set; }

        public override bool ExecuteTask()
        {
            var packagingDir = CreateEmptyOutputDirectory("dnnpackager");
            //todo: finish
            throw new NotImplementedException();
        }

        private string CreateEmptyOutputDirectory(string name)
        {
            var temp = Path.Combine(ProjectDirectory, "obj", name);
            LogMessage("Create directory: " + temp, MessageImportance.Low);

            _fileSystem.PurgeDirectory(temp, DeletionOptions.TryThreeTimes);
            _fileSystem.EnsureDirectoryExists(temp);
            _fileSystem.EnsureDiskHasEnoughFreeSpace(temp);
            return temp;
        }                  

        private void Copy(IEnumerable<string> sourceFiles, string baseDirectory, string destinationDirectory)
        {
            foreach (var source in sourceFiles)
            {
                var relativePath = _fileSystem.GetPathRelativeTo(source, baseDirectory);
                var destination = Path.Combine(destinationDirectory, relativePath);

                LogMessage("Copy file: " + source, importance: MessageImportance.Normal);

                var relativeDirectory = Path.GetDirectoryName(destination);
                _fileSystem.EnsureDirectoryExists(relativeDirectory);
                _fileSystem.CopyFile(source, destination);
            }
        }


    }
}
