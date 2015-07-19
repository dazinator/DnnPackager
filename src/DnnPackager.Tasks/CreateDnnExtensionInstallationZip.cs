using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DnnPackager.Util;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

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
            string outputZipFileName = Path.Combine(packagingDir, "resources.zip");
            CreateResourcesZip(outputZipFileName);         

            //todo: finish below
            // copy the manifest to packaging dir

            // copy assemblies to packagingdir\bin

            // copy symbols to packagingdir\bin

            // copy AdditionalFiles to packagingdir (keeping same relative path from new parent dir)

            // find any .sqldataprovider files in project and copy them to packagingdir (keeping same relative path from new parent dir)

            // find any .lic files in project and copy them to packagingdir (keeping same relative path from new parent dir)

            // find any ReleaseNotes.txt file in project and copy it to packagingdir (keeping same relative path from parent dir)
                  

            // otpional: check that if a lic file is referenced in manifest that it exists in packagingdir
            // otpional: check that if a releasenotes file is referenced in manifest that it exists in packagingdir
            // otpional: run variable substitution against manifest?
            // otpional: ensure manifest has a ResourceFile component that references Resources.zip?

            // zip up packagingdir to  OutputDirectory\OutputZipFileName          
            throw new NotImplementedException();

        }

        public void CreateResourcesZip(string outputZipFileName)
        {
          //  var outputFileName = Path.Combine(outputPathForZip, OutputZipFileName);
            using (var fsOut = File.Create(outputZipFileName))
            {
                using (var zipStream = new ZipOutputStream(fsOut))
                {
                    zipStream.SetLevel(9); //0-9, 9 being the highest level of compression
                    //  zipStream.Password = password;  // optional. Null is the same as not setting. Required if using AES.                    
                    CompressFileItems(ProjectDirectory, zipStream, ResourcesZipContent);
                    zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                    zipStream.Close();
                }
            }      
        }

        private void CompressFileItems(string baseDir, ZipOutputStream zipStream, ITaskItem[] items)
        {
            // string[] files = Directory.GetFiles(path);
            int folderOffset = baseDir.Length + (baseDir.EndsWith("\\") ? 0 : 1);
            foreach (var contentItem in ResourcesZipContent)
            {
                var sourceFilePath = Path.Combine(baseDir, contentItem.ItemSpec);
                sourceFilePath = Path.GetFullPath(sourceFilePath);

                var fi = new FileInfo(sourceFilePath);
                if (!fi.Exists)
                {
                    LogMessage("The source file '" + sourceFilePath + "' does not exist, so it will not be included in the package", MessageImportance.High);
                    continue;
                }              

                string entryName = sourceFilePath.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

                // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
                // A password on the ZipOutputStream is required if using AES.
                //   newEntry.AESKeySize = 256;

                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                // but the zip will be in Zip64 format which not all utilities can understand.
                //   zipStream.UseZip64 = UseZip64.Off;
                newEntry.Size = fi.Length;

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(fi.FullName))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
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


    }
}
