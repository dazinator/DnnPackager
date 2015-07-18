using NUnit.Framework;
using System;
using System.Collections.Generic;
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

            var task = new FindDnnManifestFile();
            task.Configuration = buildConfiguration;
            task.ProjectDirectory = Environment.CurrentDirectory;
            task.ExecuteTask();

            Assert.That(!string.IsNullOrWhiteSpace(task.ManifestFilePath));
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
