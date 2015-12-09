using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DnnPackager
{
    public class DotNetNukeWebAppInfo
    {

        private int? _WorkerProcessId = null;

        public string Name { get; private set; }
        public string AppPoolName { get; private set; }
        public string Protocol { get; private set; }
        public int Port { get; private set; }

        public string Host { get; private set; }

        public string Url
        {
            get
            {
                return string.Format("{0}://{1}:{2}", Protocol, Host, Port);
            }
        }

        public string PhysicalPath { get; private set; }

        public Uri InstallUrl
        {
            get
            {
                var uriBuilder = new UriBuilder(Url);
                var portalUrl = uriBuilder.Uri;
                var installUri = new Uri(portalUrl, @"Install/Install.aspx?mode=installresources");
                return installUri;
            }
        }

        /// <summary>
        /// Returns the worker process id for the website. May be null if the website is not currently running.
        /// </summary>
        public int? WorkerProcessId
        {
            get
            {
                if (_WorkerProcessId == null)
                {
                    _WorkerProcessId = GetWorkerProcessId();
                }
                return _WorkerProcessId;
            }
        }

        public static DotNetNukeWebAppInfo Load(string iisWebSiteName)
        {
            using (var serverManager = new Microsoft.Web.Administration.ServerManager())
            {
                var site = serverManager.Sites.FirstOrDefault(w => w.Name.ToLower() == iisWebSiteName.ToLower());
                if (site == null)
                {
                    throw new ArgumentOutOfRangeException("Could not find IIS website named: " + iisWebSiteName);
                }

                var defaultBinding = site.Bindings.FirstOrDefault();
                if (defaultBinding == null)
                {
                    throw new ArgumentOutOfRangeException("The IIS website named: " + iisWebSiteName + " does not appear to have any Bindings. Please set up a binding for it.");
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

                DotNetNukeWebAppInfo info = new DotNetNukeWebAppInfo();
                info.Name = site.Name;
                info.Port = port;
                info.Protocol = protocol;
                info.Host = host;

                if (site.Applications == null || site.Applications.Count() == 0)
                {
                    throw new ArgumentOutOfRangeException("The IIS website named: " + iisWebSiteName + " does not appear to be set up as a web application.");
                }

                var siteApp = site.Applications["/"];
                if (siteApp == null)
                {
                    throw new ArgumentOutOfRangeException("The IIS website named: " + iisWebSiteName + " does not appear to be set up as a web application.");
                }

                info.AppPoolName = siteApp.ApplicationPoolName;

                if (siteApp.VirtualDirectories == null || siteApp.VirtualDirectories.Count() == 0)
                {
                    throw new ArgumentOutOfRangeException("The IIS website named: " + iisWebSiteName + " does not appear to have a virtual directory configured.");
                }

                var siteVirtualDir = siteApp.VirtualDirectories["/"];
                string websitePhysicalPath = siteVirtualDir.PhysicalPath;

                info.PhysicalPath = websitePhysicalPath;
                return info;

            }

        }

        public bool DeployPackages(FileInfo[] installZips, int retryAttempts, Action<string> logger, out FileInfo[] failedPackages)
        {
            var targetPath = Path.GetFullPath(PhysicalPath);
            var targetInstallModulePath = Path.Combine(targetPath, "Install", "Module");
            var targetInstallModuleDirInfo = new DirectoryInfo(targetInstallModulePath);

            logger("Clearing Install/Module directory " + targetInstallModulePath);
            DnnInstallHelper.DeleteInstallPackagesInDirectory(targetInstallModuleDirInfo);

            logger("Deploying install packages..");
            var deployedPackages = DnnInstallHelper.DeployInstallPackages(installZips, targetInstallModuleDirInfo, (i, t) => logger(string.Format("Deploying package {0} of {1}", i, t)));
            foreach (var deployedPackage in deployedPackages)
            {
                logger("Dnn Extension Package: " + deployedPackage.Name + " will be installed.");
            }

            logger("Installing packages..");
            bool success = DnnInstallHelper.PerformBulkInstall(InstallUrl, targetInstallModuleDirInfo, logger, retryAttempts);
            if (!success)
            {
                failedPackages = DnnInstallHelper.GetInstallPackagesInDirectory(targetInstallModuleDirInfo);
            }
            else
            {
                failedPackages = null;
            }

            return success;
        }

        public int? GetWorkerProcessId()
        {
            using (var serverManager = new Microsoft.Web.Administration.ServerManager())
            {
                // find the worker process.             
                foreach (WorkerProcess proc in serverManager.WorkerProcesses)
                {
                    if (proc.AppPoolName == AppPoolName)
                    {

                        return proc.ProcessId;
                    }
                }

                return null;
            }
        }



    }
}
