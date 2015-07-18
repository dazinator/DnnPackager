using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnnPackager.Tasks
{
    public class ReadPackageInfoFromManifest : AbstractTask
    {
        public const string DefaultVersionNumber = "1.0.0";
        public const string DefaultPackageName = "MyExtension";
        public const string DefaultPackageFriendlyName = "My Extension";
        public const string DefaultPackageDescription = "My Extension";

        public ReadPackageInfoFromManifest()
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


                this.ManifestPackageDescription = GetXpathNodeValueOrDefault(xdoc,
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
