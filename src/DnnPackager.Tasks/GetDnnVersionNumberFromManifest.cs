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

    public class FindDnnManifestFile : AbstractTask
    {

        public const string DnnManifestExtension = "dnn";

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


            FileInfo manifestFileInfo = null;
            if (manifestFiles.Count == 1)
            {
                manifestFileInfo = manifestFiles.Single();
            }
            else
            {
                // find the appropriate one based on current build config.
                // bool foundBuildConfigSpecificManifest = false;

                foreach (var item in manifestFiles)
                {
                    var nameSplit = item.Name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameSplit != null)
                    {
                        foreach (var nameSegment in nameSplit)
                        {
                            if (nameSegment.ToLowerInvariant() == Configuration.ToLowerInvariant())
                            {
                                // found a dnn manifest file that is named containing the current build configuration so use that.
                                // foundBuildConfigSpecificManifest = true;
                                manifestFileInfo = item;
                                break;

                            }
                        }
                    }

                    if (manifestFileInfo != null)
                    {
                        break;
                    }

                }

                if (manifestFileInfo != null)
                {
                    LogMessage("Found build config specific dnn manifest file: " + manifestFileInfo.FullName);
                }
                else
                {
                    LogWarning("1", "Multiple dnn manifest files were found in the project directory " + ProjectDirectory + ". Not sure which one should be used, so defaulting to the first one - this could be wrong. ");
                    manifestFileInfo = manifestFiles.First();
                }


            }

            ManifestFilePath = manifestFileInfo.FullName;           
            ManifestFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(manifestFileInfo.Name);
            return true;

        }

    }

    public class ReadDnnManifestInfo : AbstractTask
    {
        public const string DefaultVersionNumber = "1.0.0";
        public const string DefaultPackageName = "MyExtension";
        public const string DefaultPackageFriendlyName = "My Extension";
        public const string DefaultPackageDescription = "My Extension";

        public ReadDnnManifestInfo()
        {

        }

        /// <summary>
        /// The full path to the dotnetnuke manifest file to read.
        /// </summary>
        [Required]
        public string ManifestFilePath { get; set; }

        /// <summary>
        /// The version major number from the manifest file.
        /// </summary>
        [Output]
        public string ManifestMajor { get; set; }

        /// <summary>
        /// The version minor number from the manifest file.
        /// </summary>
        [Output]
        public string ManifestMinor { get; set; }

        /// <summary>
        /// The version patch number from the manifest file.
        /// </summary>
        [Output]
        public string ManifestBuild { get; set; }

        /// <summary>
        /// The version minor number from the manifest file.
        /// </summary>
        [Output]
        public string ManifestVersionNumber { get; set; }

        [Output]
        public string ManifestPackageName { get; set; }

        [Output]
        public string ManifestPackageDescription { get; set; }

        [Output]
        public string ManifestPackageFriendlyName { get; set; }

        public override bool ExecuteTask()
        {

            if (!File.Exists(ManifestFilePath))
            {
                throw new ArgumentException("The manifest file specified does not exist. There is no such file: " + ManifestFilePath);
            }

            using (var reader = XmlReader.Create(ManifestFilePath))
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(reader);

                ParseVersion(GetXpathNodeValueOrDefault(xdoc,
                                                        "/dotnetnuke/packages/package[1]/@version",
                                                        DefaultVersionNumber,
                                                        "A version attribute was not found in the manifest file, so will default to " + DefaultVersionNumber));


                this.ManifestPackageName = GetXpathNodeValueOrDefault(xdoc,
                                                       "/dotnetnuke/packages/package[1]/@name",
                                                       DefaultPackageName,
                                                       "A package name attribute was not found in the manifest file, so will default to " + DefaultPackageName);

                this.ManifestPackageFriendlyName = GetXpathNodeValueOrDefault(xdoc,
                                                      "/dotnetnuke/packages/package[1]/friendlyName/text()",
                                                      DefaultPackageFriendlyName,
                                                      "A package friendly name attribute was not found in the manifest file, so will default to " + DefaultPackageFriendlyName);


                this.ManifestPackageFriendlyName = GetXpathNodeValueOrDefault(xdoc,
                                                     "/dotnetnuke/packages/package[1]/description/text()",
                                                     DefaultPackageDescription,
                                                     "A package description attribute was not found in the manifest file, so will default to " + DefaultPackageDescription);


            }

            return true;

        }

        private void ParseVersion(string versionString)
        {
            var versionInfo = new Version(versionString);
            ManifestMajor = versionInfo.Major.ToString();
            ManifestMinor = versionInfo.Minor.ToString();
            ManifestBuild = versionInfo.Build.ToString();
            ManifestVersionNumber = versionInfo.ToString();
        }

        private string GetXpathNodeValueOrDefault(XmlDocument xdoc, string xpath, string defaultValue, string logMessageIfNotFound)
        {
            var node = xdoc.SelectSingleNode(xpath);
            if (node != null)
            {
                return node.Value;
                // return true;
            }
            else
            {
                LogWarning("1", logMessageIfNotFound);
                return defaultValue;
            }
        }



    }
}
