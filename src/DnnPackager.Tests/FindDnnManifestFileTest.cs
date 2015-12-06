using DnnPackager.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnnPackager.Tests
{
    [TestFixture]
    public class FindDnnManifestFileTest
    {
        [TestCase("", TestName = "Can Locate Manifest File")]
        [TestCase("Debug", TestName = "Can Locate Build Config Specific Manifest File")]
        public void CanLocateManifestFile(string buildConfiguration)
        {

            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            string projectDir = currentDir.Parent.Parent.FullName.ToString();
            
            var task = new FindDnnManifestFile();
            task.Configuration = buildConfiguration;
            task.ProjectDirectory = projectDir;          
            task.ExecuteTask();

            Assert.That(task.ManifestFileItem, Is.Not.Null);
            Assert.That(!string.IsNullOrWhiteSpace(task.ManifestFileNameWithoutExtension));

            string expectedName = "manifest";
            if (!string.IsNullOrWhiteSpace(buildConfiguration))
            {
                expectedName = expectedName + "." + buildConfiguration;
            }
            Assert.That(task.ManifestFileNameWithoutExtension.ToLowerInvariant(), Is.EqualTo(expectedName.ToLowerInvariant()));        

        }       

    }
}
