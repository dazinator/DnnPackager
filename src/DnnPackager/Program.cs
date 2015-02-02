using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace DnnPackager
{
    class Program
    {
        static int Main(string[] args)
        {
            // Load paramaeters from settings / config file, but lalow override via command line arguments.
            if (args == null)
            {
                throw new ArgumentNullException("invalid arguments.");
            }

            if (args.Length < 1)
            {
                throw new ArgumentNullException("invalid arguments.");
            }

            var argType = args[0];

            bool success = false;

            switch (argType.ToLowerInvariant())
            {
                case "iiswebsite":

                    if (args.Length < 3)
                    {
                        throw new ArgumentNullException("invalid arguments.");
                    }

                    string sourceZipPackagesFolder = args[1];
                    string websiteName = args[2];
                    success = DeployToIISWebsite(sourceZipPackagesFolder, websiteName);
                    break;

                default:
                    throw new ArgumentException("invalid arguments.");
            }


            if (success)
            {
                return 0;
            }
            else
            {
                return -1;
            }

        }

        private static bool DeployToIISWebsite(string sourcePackagesFolder, string websiteName)
        {


            // Get the physical website install dir and url.

            // string siteName = "Default Web Site";

            var serverManager = new Microsoft.Web.Administration.ServerManager();
            var site = serverManager.Sites.FirstOrDefault(w => w.Name.ToLower() == websiteName.ToLower());
            if (site == null)
            {
                throw new ArgumentOutOfRangeException("Could not find IIS website named: " + websiteName);
            }

            var defaultBinding = site.Bindings.FirstOrDefault();
            if (defaultBinding == null)
            {
                throw new ArgumentOutOfRangeException("The IIS website named: " + websiteName + " does not appear to have any Bindings. Please set up a binding for it.");
            }

            int port = 80;
            string protocol = "http";
            string host = "localhost";
            if (defaultBinding.EndPoint != null)
            {
                port = defaultBinding.EndPoint.Port;
            }
            if (!string.IsNullOrEmpty(defaultBinding.Protocol))
            {
                protocol = defaultBinding.Protocol;
            }
            if (!string.IsNullOrEmpty(defaultBinding.Host))
            {
                host = defaultBinding.Host;
            }

            string websiteUrl = string.Format("{0}://{1}:{2}", protocol, host, port);


            var uriBuilder = new UriBuilder(websiteUrl);
            var portalUrl = uriBuilder.Uri;
            var installUri = new Uri(portalUrl, @"Install/Install.aspx?mode=installresources");

            // Clean target install directory.
            if (site.Applications == null || site.Applications.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("The IIS website named: " + websiteName + " does not appear to be set up as a web application.");
            }

            var siteApp = site.Applications["/"];
            if (siteApp == null)
            {
                throw new ArgumentOutOfRangeException("The IIS website named: " + websiteName + " does not appear to be set up as a web application.");
            }

            if (siteApp.VirtualDirectories == null || siteApp.VirtualDirectories.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("The IIS website named: " + websiteName + " does not appear to have a virtual directory configured.");
            }

            var siteVirtualDir = siteApp.VirtualDirectories["/"];
            string websitePhysicalPath = siteVirtualDir.PhysicalPath;

            var targetPath = Path.GetFullPath(websitePhysicalPath);
            var targetInstallModulePath = Path.Combine(targetPath, "Install", "Module");
            var targetInstallModuleDirInfo = new DirectoryInfo(targetInstallModulePath);

            Console.WriteLine("Clearing Install/Module directory " + targetInstallModulePath);
            DnnInstallHelper.DeleteInstallPackagesInDirectory(targetInstallModuleDirInfo);

            var sourcePath = Path.GetFullPath(sourcePackagesFolder);
            var sourceDirInfo = new DirectoryInfo(sourcePath);
            // Default to a "Content" subfolder if there is one.
            if (sourceDirInfo.GetDirectories("Content").Any())
            {
                sourceDirInfo = sourceDirInfo.GetDirectories("Content").First();
            }
            Console.WriteLine("Deploying install packages..");
            var deployedPackages = DnnInstallHelper.DeployInstallPackages(sourceDirInfo, targetInstallModuleDirInfo, (i, t) => Console.WriteLine(string.Format("Deploying package {0} of {1}", i, t)));
            foreach (var deployedPackage in deployedPackages)
            {
                Console.WriteLine("Dnn Extension Package: " + deployedPackage.Name + " will be installed.");
            }

            Console.WriteLine("Installing packages..");
            int maxAttempt = 10;
            bool success = DnnInstallHelper.PerformBulkInstall(installUri, targetInstallModuleDirInfo, Console.WriteLine, 10);
            if (!success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var message = string.Format("After {0} attempts, the following packages have failed to install:", maxAttempt);
                Console.WriteLine(message);

                // Get failed install packages.
                var failures = DnnInstallHelper.GetInstallPackagesInDirectory(targetInstallModuleDirInfo);
                foreach (var fileInfo in failures)
                {
                    Console.WriteLine(fileInfo.Name);
                }
                Console.WriteLine("Some packages failed to install.");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Dnn package installation successful.");
            return true;
        }
    }
}
