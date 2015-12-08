using DnnPackager.Tasks;
using Microsoft.Build.Utilities;
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
        [TestCase("", "manifest.dnn", TestName = "Can find manifest.dnn when no active build configuration specified.")]
        [TestCase("Debug", "manifest.dnn", TestName = "Can find manifest.dnn if no build configuration specific manifest present.")]
        [TestCase("Debug", "manifest.dnn", "manifest.debug.dnn", TestName = "Can find build specific manifest when one is present rather than manifest.dnn.")]
       
        public void CanLocateManifestFile(string buildConfiguration, params string[] manifestFilesInProject)
        {

            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            string projectDir = currentDir.Parent.Parent.FullName.ToString();

            //string manifestFilePath = Path.Combine(currentDir.ToString(), manifestFileName);

            var manifestItems = new List<TaskItem>();

            foreach (var item in manifestFilesInProject)
            {
                manifestItems.Add(new TaskItem(item));
            }

            var task = new FindDnnManifestFile();
            task.Configuration = buildConfiguration;
            task.ProjectDirectory = projectDir;
            task.ManifestFileProjectItems = manifestItems.ToArray();
            task.ExecuteTask();

            Assert.That(task.ManifestFileItemsForPackage, Is.Not.Null);
            Assert.That(task.DefaultManifestFileItemForPackage, Is.Not.Null);

            Assert.That(!string.IsNullOrWhiteSpace(task.DefaultManifestFileItemForPackage.ItemSpec));

            string expectedName = "manifest";
            if (!string.IsNullOrWhiteSpace(buildConfiguration) && manifestItems.Any(a=>a.GetFileNameWithoutExtension(projectDir).ToLowerInvariant().Contains(buildConfiguration.ToLowerInvariant())))
            {
                expectedName = expectedName + "." + buildConfiguration;
            }
            Assert.That(task.DefaultManifestFileItemForPackage.GetFileNameWithoutExtension(projectDir).ToLowerInvariant(), Is.EqualTo(expectedName.ToLowerInvariant()));

        }

    }
}
