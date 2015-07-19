using DnnPackager.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DnnPackager.Tests
{
    [TestFixture]
    public class CreateDnnExtensionInstallationZipTest
    {

        public const string TestPackageContentFolderName = "TestPackageContent";
        public const string SqlFilesFolderName = "SqlFiles";

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
            task.DebugSymbols = true;

            try
            {
                task.ExecuteTask();
                Assert.That(task.InstallPackage, Is.Not.Null);
                var installPackagePath = task.InstallPackage.ItemSpec;
            }
            catch (IOException ex)
            {
                string path = Path.Combine(projectDir, @"obj\DnnPackager\resources.zip");
                CheckForLock(path);
                throw;
            }        

           


        }

        private void CheckForLock(string path)
        {
            string fileName = path;//Path to locked file

            Process tool = new Process();

            var handleExePath = Path.Combine(new DirectoryInfo(System.Environment.CurrentDirectory)
                                .Parent.Parent.Parent.FullName, @"tools\handle.exe");

            tool.StartInfo.FileName = handleExePath;
            tool.StartInfo.Arguments = fileName + " /accepteula";
            tool.StartInfo.UseShellExecute = false;
            tool.StartInfo.RedirectStandardOutput = true;
            tool.Start();
            tool.WaitForExit();
            string outputTool = tool.StandardOutput.ReadToEnd();

            string matchPattern = @"(?<=\s+pid:\s+)\b(\d+)\b(?=\s+)";
            foreach (Match match in Regex.Matches(outputTool, matchPattern))
            {
               var process = Process.GetProcessById(int.Parse(match.Value));

                   // .Kill();
            }
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

            path = SqlFilesFolderName + "\\" + "InstallScript.sqldataprovider";          
            items.Add(new TaskItem(path));
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
