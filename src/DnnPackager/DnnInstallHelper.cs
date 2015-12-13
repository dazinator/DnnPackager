using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace DnnPackager
{
    class DnnInstallHelper
    {

        /// <summary>
        /// Deletes all install packages in the specified directory.
        /// </summary>
        /// <param name="dir"></param>
        public static FileInfo[] GetInstallPackagesInDirectory(DirectoryInfo dir)
        {
            var extensions = new[] { ".resources", ".zip" };

            FileInfo[] files =
                dir.EnumerateFiles()
                     .Where(f => extensions.Contains(f.Extension.ToLower()))
                     .ToArray();
            return files;

        }

        /// <summary>
        /// Deletes all install packages in the specified directory.
        /// </summary>
        /// <param name="dir"></param>
        public static void DeleteInstallPackagesInDirectory(DirectoryInfo dir)
        {

            var files = GetInstallPackagesInDirectory(dir);
            foreach (var fileInfo in files)
            {
                File.SetAttributes(fileInfo.FullName, FileAttributes.Normal);
                fileInfo.Delete();
            }

        }

        /// <summary>
        /// Deploys all Dnn packages in the source directory to the target directory.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="progressCallback"></param>
        public static FileInfo[] DeployInstallPackages(FileInfo[] packageFiles, DirectoryInfo target, Action<int, int> progressCallback = null)
        {          
            int total = packageFiles.Count();
            int i = 0;
            var deployedFiles = new List<FileInfo>();
            foreach (var file in packageFiles)
            {
                i++;
                deployedFiles.Add(file.CopyTo(Path.Combine(target.FullName, file.Name)));
                if (progressCallback != null)
                {
                    progressCallback(i, total);
                }
            }
            return deployedFiles.ToArray();
        }

        /// <summary>
        /// Performs a DNN bulk install of the deployed packages.
        /// <remarks>This must be running within the DNN bin directory in order to work.</remarks>
        /// </summary>
        public static bool PerformBulkInstall(Uri portalUrl, DirectoryInfo targetDir, Action<string> logMessage, int maxRetries = 5)
        {
            int retryCount = 0;
            bool success = false;
            string message = "Performing bulk module install @ " + portalUrl;
            logMessage(message);

            FileInfo[] deployPackages = GetInstallPackagesInDirectory(targetDir);
            //var remainingPackageCount = 
            //var lastPackageCount = remainingPackageCount;
            // deployPackages.AsEnumerable().Where(e => e.Exists).Count();
            // message = string.Format("{0} packages to insall.", deployPackages.Count());
            // logMessage(message);

            while (!success && retryCount < maxRetries)
            {
                logMessage("Attempt " + (retryCount + 1));
                logMessage(string.Format("{0} packages to install.", deployPackages.Count()));

                try
                {
                    using (var client = new ExtendedWebClient())
                    {
                        //Prvent proxy use..
                        client.Proxy = new WebProxy();
                        client.Timeout = 600000;

                        using (var stream = client.OpenRead(portalUrl))
                        {
                            if (stream != null)
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    var response = reader.ReadToEnd();
                                    // analyse to see if it failed.
                                    if (response.Contains("Error"))
                                    {
                                        //logMessage("Response contains an error. Response is: " + response);
                                        continue;
                                    }
                                    success = true;
                                }
                            }

                        }
                    }

                }
                catch (WebException ex)
                {
                    logMessage("Exception Message: " + ex.Message);
                    if (ex.Response is HttpWebResponse)
                    {
                        var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                        // logMessage("Request errored with status code is: " + statusCode);
                    }
                }
                catch (Exception e)
                {
                    logMessage("Error. " + e.Message);
                }
                finally
                {
                    var remainingPackages = GetInstallPackagesInDirectory(targetDir);
                    if (remainingPackages.Count() < deployPackages.Count())
                    {
                        var progress = deployPackages.Count() - remainingPackages.Count();
                        // we made some progress so this is just a network jitter.
                        logMessage(string.Format("{0} packages were just installed.", progress));
                        deployPackages = remainingPackages;
                        // reset retry counter.
                        retryCount = 0;
                    }
                    else
                    {
                        // we made no progress so could be legitimate error preventing install.
                        retryCount++;
                        logMessage(string.Format("No packages were installed, will retry.."));
                    }
                }
            }

            return success;
        }

    }
}
