using NUnit.Framework;
using System;
using System.IO;

namespace DnnPackager.Tests.Util
{
    public static class EnvironmentSetup
    {
        //private static bool IsRunningOnCIServer()
        //{
        //    return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APPVEYOR"));
        //}

        public readonly static Lazy<string> EnsureEnvironmentCurrentDirectory = new Lazy<string>(() =>
        {
            //if (IsRunningOnCIServer())
            //{
            //    Environment.CurrentDirectory = Path.Combine(System.Environment.CurrentDirectory, @"src\DnnPackager.Tests\bin\Release\net451\");
            //}

            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            Console.WriteLine("Current Dir: " + System.Environment.CurrentDirectory);
            return Environment.CurrentDirectory;
        });


        public readonly static Lazy<string> TestsProjectDirectory = new Lazy<string>(() =>
        {
            var currentDir = new DirectoryInfo(EnsureEnvironmentCurrentDirectory.Value);
            return currentDir.Parent.Parent.Parent.FullName.ToString();
        });
    }
}
