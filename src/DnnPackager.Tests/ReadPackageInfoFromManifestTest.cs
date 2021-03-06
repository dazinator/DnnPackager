﻿using DnnPackager.Tasks;
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
    public class ReadPackageInfoFromManifestTest
    {
        [TestCase("manifest.dnn", TestName = "Can Read Manifest File")]   
        public void CanReadManifestFile(string manifestFileName)
        {
            string manifestFilePath = Path.Combine(System.Environment.CurrentDirectory, manifestFileName);
            var currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            string projectDir = currentDir.Parent.Parent.FullName.ToString();

            var task = new ReadPackageInfoFromManifest();
            task.ProjectDirectory = projectDir;
            task.ManifestFileItem = new TaskItem(manifestFileName);          
            task.ExecuteTask();

            Assert.That(task.ManifestPackageName, Is.EqualTo("TestPackage"));
            Assert.That(task.ManifestVersionNumber, Is.EqualTo("1.0.0"));
            Assert.That(task.ManifestPackageFriendlyName, Is.EqualTo("Unit Test Package"));
            Assert.That(task.ManifestPackageDescription, Is.EqualTo("a test module"));
            Assert.That(task.ManifestMajor, Is.EqualTo("1"));
            Assert.That(task.ManifestMinor, Is.EqualTo("0"));
            Assert.That(task.ManifestBuild, Is.EqualTo("0"));          

        }       

    }
}
