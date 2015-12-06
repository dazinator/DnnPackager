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

    public class CreateDnnExtensionInstallationZip : AbstractTask
    {

        public const string ReleaseNotesFileName = "ReleaseNotes.txt";
        public const string IntermediateOutputFolderName = "DnnPackager";

        public CreateDnnExtensionInstallationZip()
        {

        }

        [Required]
        public ITaskItem ManifestFileItem { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string OutputZipFileName { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

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

        public bool DebugSymbols { get; set; }

        /// <summary>
        /// Used to output the built zip install package.
        /// </summary>
        [Output]
        public ITaskItem InstallPackage { get; set; }

        public override bool ExecuteTask()
        {

            var packagingDir = CreateEmptyOutputDirectory(IntermediateOutputFolderName);
            string outputZipFileName = Path.Combine(packagingDir, "resources.zip");
            CreateResourcesZip(outputZipFileName);


            // copy the manifest to packaging dir
            var manifestFilePath = ManifestFileItem.GetFullPath(this.ProjectDirectory);
            CopyFile(manifestFilePath, packagingDir);

            // Ensure packagingdir\bin dir
            string binFolder = Path.Combine(packagingDir, "bin");
            EnsureEmptyDirectory(binFolder);

            // copy assemblies to packagingdir\bin              
            CopyFileTaskItems(ProjectDirectory, Assemblies, binFolder);

            // copy symbols to packagingdir\bin
            if (DebugSymbols)
            {
                CopyFileTaskItems(ProjectDirectory, Symbols, binFolder, true);
            }

            // copy AdditionalFiles to packagingdir (keeping same relative path from new parent dir)
            if(AdditionalFiles.Length > 0)
            {
                // This item array is initialised with a dummy item, so that its easy for 
                // for consumers to override and add in their own items.
                // This means we have to take care of removing the dummy entry though.
                if (AdditionalFiles[0].ItemSpec == "_DummyEntry_.txt")
                {
                    var filesList = AdditionalFiles.ToList();
                    filesList.RemoveAt(0);
                    AdditionalFiles = filesList.ToArray();
                }
            }

            CopyFileTaskItems(ProjectDirectory, AdditionalFiles, packagingDir, false, true);

            // find any
            // .sqldataprovider files 
            // .lic files
            // "ReleaseNotes.txt" file
            // and copy them to the same relative directory in the packaging dir.
            ITaskItem[] specialPackageContentFiles =
                FindContentFiles(t =>
                    Path.GetExtension(t.ItemSpec).ToLowerInvariant() == ".sqldataprovider" ||
                    Path.GetExtension(t.ItemSpec).ToLowerInvariant() == ".lic" ||
                    Path.GetFileName(t.ItemSpec).ToLowerInvariant() == ReleaseNotesFileName.ToLowerInvariant()
                    );
            CopyFileTaskItems(ProjectDirectory, specialPackageContentFiles, packagingDir, false, true);


            // otpional: check that if a lic file is referenced in manifest that it exists in packagingdir
            // otpional: check that if a releasenotes file is referenced in manifest that it exists in packagingdir
            // otpional: run variable substitution against manifest?
            // otpional: ensure manifest has a ResourceFile component that references Resources.zip?

            // zip up packagingdir to  OutputDirectory\OutputZipFileName     
            string installZipFileName = Path.Combine(OutputDirectory, OutputZipFileName);
            CompressFolder(packagingDir, installZipFileName);

            InstallPackage = new TaskItem(installZipFileName);
            return true;
        }

        private ITaskItem[] FindContentFiles(Predicate<ITaskItem> filter)
        {
            var items = ResourcesZipContent.Where(t => filter(t)).ToArray();
            return items;
        }

        private void CopyFileTaskItems(string baseDir, ITaskItem[] taskItems, string destinationFolder, bool skipWhenNotExists = false, bool keepRelativePath = false)
        {
            foreach (var item in taskItems)
            {
                var sourceFilePath = Path.Combine(baseDir, item.ItemSpec);
                sourceFilePath = Path.GetFullPath(sourceFilePath);
                string targetDir = destinationFolder;

                if (keepRelativePath)
                {
                    // rather than copy the source files directly into the destination folder,
                    // if the source file is in: baseDir/somefolder/someotherFolder
                    // then it should end up in destinationFolder/somefolder/someotherFolder    
                    targetDir = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(destinationFolder, item.ItemSpec)));

                }
                CopyFile(sourceFilePath, targetDir, skipWhenNotExists);
            }
        }

        private void CopyFile(string sourceFile, string targetDir, bool skipIfNotExists = false)
        {
            var fileInfo = new FileInfo(sourceFile);
            if (!fileInfo.Exists)
            {
                if (skipIfNotExists)
                {
                    return;
                }
                throw new FileNotFoundException("Unable to find file.", sourceFile);
            }

            var sourceFileName = Path.GetFileName(sourceFile);

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var targetFileName = Path.Combine(targetDir, sourceFileName);
            File.Copy(sourceFile, targetFileName);
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

        private void CompressFolder(string sourceDir, string outputFileName)
        {
            FastZip fastZip = new FastZip();
            bool recurse = true;  // Include all files by recursing through the directory structure
            string filter = null; // Dont filter any files at all
            fastZip.CreateZip(outputFileName, sourceDir, recurse, filter);
        }

        private string CreateEmptyOutputDirectory(string name)
        {
            var temp = Path.Combine(ProjectDirectory, IntermediateOutputPath, name);
            LogMessage("Create directory: " + temp, MessageImportance.Low);
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
