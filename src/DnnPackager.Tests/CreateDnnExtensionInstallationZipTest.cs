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
             

        [TestCase("manifest.dnn", TestName = "Can Create Install Zip Package")]
        public void CanCreateInstallationZip(string manifestFileName)
        {

            MockRepository mock = new MockRepository();
            IBuildEngine engine = mock.Stub<IBuildEngine>();

            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);

            string manifestFilePath = Path.Combine(currentDir.ToString(), manifestFileName);
            string outputDir = Path.Combine(currentDir.ToString(), "testpackageoutput");
            string intermediatedir = Path.Combine(currentDir.ToString(), "testintermediatedir");
            RecreateDir(outputDir);
            RecreateDir(intermediatedir);

            string outputZipFileName = "unittest.zip";

            // Use current project location as the project dir.

            string projectDir = currentDir.Parent.Parent.FullName.ToString();
            TaskItem manifestFile = new TaskItem(manifestFileName);
            var manifestItems = new List<TaskItem>();
            manifestItems.Add(manifestFile);         

            var task = new CreateDnnExtensionInstallationZip();
            task.BuildEngine = engine;
            task.ManifestFileItems = manifestItems.ToArray();
            task.OutputDirectory = outputDir;
            task.OutputZipFileName = outputZipFileName;
            task.ProjectDirectory = projectDir;
            task.IntermediateOutputPath = intermediatedir;
            task.ResourcesZipContent = TestPackageContentHelper.GetFakeResourcesContentItems();
            task.Symbols = TestPackageContentHelper.GetFakeSymbolFileItems();
            task.Assemblies = TestPackageContentHelper.GetFakeAssemblyFileItems();
            task.AdditionalFiles = TestPackageContentHelper.GetFakeAdditionalFileItems();
            task.DebugSymbols = true;

            try
            {
                // set a team city environment variable so that we get team city integration.
                Environment.SetEnvironmentVariable("TEAMCITY_VERSION", "9.1.0");
                task.ExecuteTask();
                Assert.That(task.InstallPackage, Is.Not.Null);
                var installPackagePath = task.InstallPackage.ItemSpec;
            }
            catch (IOException)
            {
                string path = Path.Combine(projectDir, intermediatedir, @"\DnnPackager\resources.zip");
                CheckForLock(path);
                throw;
            }


        }

        [TestCase("manifest.dnn", TestName = "Can Create Sources Zip Package")]
        public void CanCreateSourcesZip(string manifestFileName)
        {

            MockRepository mock = new MockRepository();
            IBuildEngine engine = mock.Stub<IBuildEngine>();

            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);

            string manifestFilePath = Path.Combine(currentDir.ToString(), manifestFileName);
            string outputDir = Path.Combine(currentDir.ToString(), "testpackageoutput");
            string intermediatedir = Path.Combine(currentDir.ToString(), "testintermediatedir");
            RecreateDir(outputDir);
            RecreateDir(intermediatedir);

            string outputZipFileName = "unittest.zip";
            string outputSourcesZipFileName = "unittestsources.zip";

            // Use current project location as the project dir.

            string projectDir = currentDir.Parent.Parent.FullName.ToString();
            TaskItem manifestFile = new TaskItem(manifestFileName);
            var manifestItems = new List<TaskItem>();
            manifestItems.Add(manifestFile);

            var task = new CreateDnnExtensionInstallationZip();
            task.BuildEngine = engine;
            task.ManifestFileItems = manifestItems.ToArray();
            task.OutputDirectory = outputDir;
            task.OutputZipFileName = outputZipFileName;
            task.ProjectDirectory = projectDir;
            task.IntermediateOutputPath = intermediatedir;
            task.ResourcesZipContent = TestPackageContentHelper.GetFakeResourcesContentItems();
            task.Symbols = TestPackageContentHelper.GetFakeSymbolFileItems();
            task.Assemblies = TestPackageContentHelper.GetFakeAssemblyFileItems();
            task.AdditionalFiles = TestPackageContentHelper.GetFakeAdditionalFileItems();
            task.DebugSymbols = true;
            task.OutputSourcesZipFileName = outputSourcesZipFileName;
            task.SourceFiles = TestPackageContentHelper.GetFakeCompiletems();

            try
            {
                // set a team city environment variable so that we get team city integration.
                Environment.SetEnvironmentVariable("TEAMCITY_VERSION", "9.1.0");
                task.ExecuteTask();
                Assert.That(task.InstallPackage, Is.Not.Null);
                var installPackagePath = task.InstallPackage.ItemSpec;

                Assert.That(task.SourcesPackage, Is.Not.Null);
            }
            catch (IOException)
            {
                string path = Path.Combine(projectDir, intermediatedir, @"\DnnPackager\resources.zip");
                CheckForLock(path);
                throw;
            }


        }

        private void CheckForLock(string path)
        {
            string fileName = path;//Path to locked file

            Process tool = new Process();

            Console.WriteLine("Current Dir: " + System.Environment.CurrentDirectory);
            var handleExePath = Path.Combine(new DirectoryInfo(System.Environment.CurrentDirectory)
                                .Parent.Parent.Parent.Parent.FullName, @"tools\handle.exe");

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

    public static class TestPackageContentHelper
    {
        public const string TestPackageContentFolderName = "TestPackageContent";
        public const string SqlFilesFolderName = "SqlFiles";

        public static ITaskItem[] GetFakeAdditionalFileItems()
        {
            List<ITaskItem> items = new List<ITaskItem>();
            var path = TestPackageContentFolderName + "\\" + "TestTextFile1.txt";
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();
        }

        public static ITaskItem[] GetFakeAssemblyFileItems()
        {
            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var targetPath = Path.Combine(currentDir.ToString(), "DnnPackager.Tests.dll");

            List<ITaskItem> items = new List<ITaskItem>();
            var path = targetPath;
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();
        }

        public static  ITaskItem[] GetFakeSymbolFileItems()
        {
            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var targetPath = Path.Combine(currentDir.ToString(), "DnnPackager.Tests.pdb");
            List<ITaskItem> items = new List<ITaskItem>();
            var path = targetPath;
            var newItem = new TaskItem(path);
            items.Add(newItem);
            return items.ToArray();

        }

        public static ITaskItem[] GetFakeCompiletems()
        {
           
            List<ITaskItem> items = new List<ITaskItem>();
            var path = TestPackageContentFolderName + "\\" + "Default.ascx.cs";
            var newItem = new TaskItem(path);
            items.Add(newItem);

            var path2 = TestPackageContentFolderName + "\\" + "Default.ascx.designer.cs";
            var newItem2 = new TaskItem(path2);
            items.Add(newItem2);

            return items.ToArray();

        }

        public static ITaskItem[] GetFakeResourcesContentItems()
        {
            List<ITaskItem> items = new List<ITaskItem>();
            var path = TestPackageContentFolderName + "\\" + "StyleSheet1.css";
            var newItem = new TaskItem(path);
            items.Add(newItem);

            path = SqlFilesFolderName + "\\" + "InstallScript.sqldataprovider";
            items.Add(new TaskItem(path));
            return items.ToArray();
        }

    }

}
