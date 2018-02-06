using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;

namespace DnnPackager.Tests
{
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

        public static ITaskItem[] GetFakeSymbolFileItems()
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
