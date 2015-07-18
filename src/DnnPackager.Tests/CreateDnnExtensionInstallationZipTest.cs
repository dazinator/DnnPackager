using DnnPackager.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnnPackager.Tests
{
    [TestFixture]
    public class CreateDnnExtensionInstallationZipTest
    {

        public const string TestPackageContentFolderName = "TestPackageContent";


        [TestCase("manifest.dnn", TestName = "Can Create Install Zip Package")]
        public void CanLocateManifestFile(string manifestFileName)
        {

            MockRepository mock = new MockRepository();
            IBuildEngine engine = mock.Stub<IBuildEngine>();

            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);

            string manifestFilePath = Path.Combine(currentDir.ToString(), manifestFileName);
            string outputDir = Path.Combine(currentDir.ToString(), "testpackageoutput");
            RecreateDir(outputDir);

            string outputZipFileName = "unittest.zip";

            // Use current project location as the project dir.

            string projectDir = currentDir.Parent.Parent.FullName.ToString();



            //ManifestFilePath="$(DnnManifestFilePath)"
            //OutputDirectory="$(OutDir)"
            //OutputZipFileName="$(DnnInstallationZipFileName)"

            //ResourcesZipContent="@(ResourcesZipContentFiles)"
            //AdditionalFiles="@(PackageFiles)"
            //Assemblies="@(PackageAssemblies)"
            //Symbols="@(PackageSymbols)"
            //ProjectDirectory="$(MSBuildProjectDirectory)"
            //>

            var task = new CreateDnnExtensionInstallationZip();
            task.BuildEngine = engine;
            task.ManifestFilePath = manifestFilePath;
            task.OutputDirectory = outputDir;
            task.OutputZipFileName = outputZipFileName;
            task.ProjectDirectory = projectDir;

            task.ResourcesZipContent = GetFakeResourcesContentItems();
            task.Symbols = GetFakeSymbolFileItems();
            task.Assemblies = GetFakeAssemblyFileItems();
            task.AdditionalFiles = GetFakeAdditionalFileItems();

            task.ExecuteTask();

            Assert.That(task.InstallPackage, Is.Not.Null);
            var installPackagePath = task.InstallPackage.ItemSpec;


        }

        private ITaskItem[] GetFakeAdditionalFileItems()
        {
            List<ITaskItem> items = new List<ITaskItem>();
            var path = TestPackageContentFolderName + "\\" + "TestTextFile1.txt";
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();
        }

        private ITaskItem[] GetFakeAssemblyFileItems()
        {
            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var targetPath = Path.Combine(currentDir.ToString(), "DnnPackager.Tests.dll");

            List<ITaskItem> items = new List<ITaskItem>();
            var path = targetPath;
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();
        }

        private ITaskItem[] GetFakeSymbolFileItems()
        {
            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var targetPath = Path.Combine(currentDir.ToString(), "DnnPackager.Tests.pdb");
            List<ITaskItem> items = new List<ITaskItem>();
            var path = targetPath;
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();

        }

        private ITaskItem[] GetFakeResourcesContentItems()
        {         
            List<ITaskItem> items = new List<ITaskItem>();
            var path = TestPackageContentFolderName + "\\" + "StyleSheet1.css";
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();
        }

        private void RecreateDir(string outputDir)
        {
            if (Directory.Exists(outputDir))
            {
                System.IO.DirectoryInfo dir = new DirectoryInfo(outputDir);
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    d.Delete(true);
                }
            }

            Directory.CreateDirectory(outputDir);
        }

    }
}
