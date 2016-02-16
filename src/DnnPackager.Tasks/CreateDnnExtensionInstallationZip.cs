using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using DnnPackager.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Build.Utilities;

namespace DnnPackager.Tasks
{

    public class CreateDnnExtensionInstallationZip : AbstractTask
    {

        public const string ReleaseNotesFileName = "ReleaseNotes.txt";
        public const string IntermediateOutputFolderName = "DnnPackager";

        private IBuildServer _buildServer;


        public CreateDnnExtensionInstallationZip()
        {
            //todo: if more build servers are supported in future, this can become an array.
            _buildServer = new TeamCityBuildServer((message) => this.LogMessage(message, MessageImportance.High));
        }

        [Required]
        public ITaskItem[] ManifestFileItems { get; set; }

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

        [Required]
        public ITaskItem[] ResourceFiles { get; set; }

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

            // we want to put module content, and resx files, in the resources zip that will get deployed to module install (desktop modules) folder dir.

            ITaskItem[] resourcesZipContentItems = ResourceFiles != null
                ? ResourcesZipContent.Concat(ResourceFiles).ToArray()
                : ResourcesZipContent.ToArray();

            CreateResourcesZip(outputZipFileName, resourcesZipContentItems);

            // copy the manifests to packaging dir root
            foreach (var item in ManifestFileItems)
            {
                var manifestFilePath = item.GetFullPath(this.ProjectDirectory);
                CopyFile(manifestFilePath, packagingDir);
            }

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

            // copy AdditionalFiles directly into packagingdir (keeping same relative path from new parent dir)
            if (AdditionalFiles.Length > 0)
            {
                // This item array is initialised with a dummy item, so that its easy for 
                // for consumers to override and add in their own items.
                // This means we have to take care of removing the dummy entry though.
                var dummyItem = AdditionalFiles.FirstOrDefault(a => a.ItemSpec == "_DummyEntry_.txt");
                if (dummyItem != null)
                {
                    var filesList = AdditionalFiles.ToList();
                    filesList.Remove(dummyItem);
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

            // publish asset to build server.
            PublishToBuildServer(new ITaskItem[] { InstallPackage });
            return true;
        }

        private ITaskItem[] FindContentFiles(Predicate<ITaskItem> filter)
        {
            var items = ResourcesZipContent.Where(t => filter(t)).ToArray();
            return items;
        }

        private void CopyFileTaskItems(string baseDir, ITaskItem[] taskItems, string destinationFolder, bool skipWhenNotExists = false, bool keepRelativePath = false)
        {

            var baseDirectoryInfo = new DirectoryInfo(baseDir);

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
                    var relativePath = MakeRelativePath(baseDir, sourceFilePath);
                    var parts = relativePath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(0);
                        relativePath = string.Join("\\", parts.ToArray());
                    }

                    // var pathRoot = Path.GetPathRoot(relativePath);

                    var targetPath = Path.Combine(destinationFolder, relativePath);
                    targetDir = Path.GetDirectoryName(targetPath); // Path.GetFullPath(relativePath);
                                                                   //Path.GetDirectoryName(Path.GetFullPath(Path.Combine(destinationFolder, item.ItemSpec)));

                }
                CopyFile(sourceFilePath, targetDir, skipWhenNotExists);
            }
        }



        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
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

        public void CreateResourcesZip(string outputZipFileName, ITaskItem[] taskItems)
        {
            //  var outputFileName = Path.Combine(outputPathForZip, OutputZipFileName);
            using (var fsOut = File.Create(outputZipFileName))
            {
                using (var zipStream = new ZipOutputStream(fsOut))
                {
                    zipStream.SetLevel(9); //0-9, 9 being the highest level of compression
                                           //  zipStream.Password = password;  // optional. Null is the same as not setting. Required if using AES.                            
                    CompressFileItems(ProjectDirectory, zipStream, taskItems);
                    zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                    zipStream.Close();
                }
            }
        }

        private void CompressFileItems(string baseDir, ZipOutputStream zipStream, ITaskItem[] items)
        {
            // string[] files = Directory.GetFiles(path);
            int folderOffset = baseDir.Length + (baseDir.EndsWith("\\") ? 0 : 1);
            foreach (var contentItem in items)
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

        public void PublishToBuildServer(ITaskItem[] items)
        {
            // detects if we are running within the context of a recognised build sever such as team city, and if so, 
            // informs that system via console.out of the produced zip file.
            if (_buildServer != null)

                foreach (var item in items)
                {
                    var fileInfo = new FileInfo(item.ItemSpec);
                    _buildServer.NewBuildArtifact(fileInfo);
                }
        }

    }
}
