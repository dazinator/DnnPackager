using DnnPackager.Core;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{
    public class CommandVisitor : ICommandVisitor
    {
        private ILogger _Logger;

        public CommandVisitor(ILogger logger)
        {
            _Logger = logger;
        }

        #region ICommandVisitor

        public bool Success { get; set; }

        public void VisitBuildCommand(BuildProjectOptions options)
        {
            bool attachDebugger = false;
            DotNetNukeWebAppInfo dnnWebsite = null;

            Success = BuildProjectAndGetOutputZips(options, out FileInfo[] installPackages, out DTE dte);
            attachDebugger = options.Attach;

            dnnWebsite = GetDotNetNukeWebsiteInfo(options.WebsiteName);
            if (installPackages != null && installPackages.Any())
            {
                Success = DeployToIISWebsite(installPackages, dnnWebsite);
            }
            else
            {
                // no packages to install.
                // log warning?
                _Logger.LogInfo("No packages to install.");
                Success = true;
            }

            if (!Success)
            {
                return;
            }

            if (dte != null && attachDebugger)
            {

                _Logger.LogInfo("Hooking up your debugger!");
                var dnnWebsiteProcessId = dnnWebsite.GetWorkerProcessId();
                if (!dnnWebsiteProcessId.HasValue)
                {
                    _Logger.LogInfo("Unable to find running worker process. Is your website running!?");
                    return;
                }
                Success = AttachDebugger(dnnWebsiteProcessId.Value, dte);
            }


        }

        public void VisitDeployCommand(DeployOptions options)
        {
            FileInfo[] installPackages = GetInstallZipsFromDirectory(options.DirectoryPath);

            if (installPackages != null && installPackages.Any())
            {
                DotNetNukeWebAppInfo dnnWebsite = GetDotNetNukeWebsiteInfo(options.WebsiteName);
                Success = DeployToIISWebsite(installPackages, dnnWebsite);
            }
            else
            {
                // no packages to install.
                // log warning?
                _Logger.LogInfo("No packages to install.");
                Success = true;
            }
        }

        public void VisitInstallTargetsCommand(InstallTargetsToVSProjectFileOptions options)
        {
            _Logger.LogInfo(string.Format("Installing targets to: {0}", options.ProjectName));

            Microsoft.Build.Evaluation.ProjectCollection collection = new Microsoft.Build.Evaluation.ProjectCollection();
            Microsoft.Build.Evaluation.Project project = new Microsoft.Build.Evaluation.Project(options.ProjectName, null, null, collection, Microsoft.Build.Evaluation.ProjectLoadSettings.IgnoreMissingImports);

            var installHelper = new InstallTargetsHelper(_Logger);
            var toolsDir = options.ToolsPath.TrimEnd(new char[] { '\\', '/' });
            this.Success = installHelper.Install(project, toolsDir);

        }

        public void VisitDebugCommand(DebugSolutionOptions options)
        {
            if (options.ProcessId == 0)
            {
                if (!TryLoadProcessIdFromTempFile(out int processId))
                {
                    _Logger.LogError("Unable to load process id");
                    Success = false;
                    return;
                }
                options.ProcessId = processId;
            }

            if (!TryGetEnvDte(options.ProcessId, out DTE dte))
            {
                Success = false;
                return;
            }

            MessageFilter.Register();
            FileInfo[] packages = GetPackagesForDeployment(options, dte);


            var dnnWebsite = GetDotNetNukeWebsiteInfo(options.WebsiteName);
            if (packages != null && packages.Any())
            {
                _Logger.LogInfo($"Installing packages to {options.WebsiteName}");
                if (!DeployToIISWebsite(packages, dnnWebsite))
                {
                    Success = false;
                    return;
                }
            }
            else
            {
                // no packages to install.
                // log warning?
                _Logger.LogInfo("No packages to install.");
                Success = true;
            }

            _Logger.LogInfo("Hooking up your debugger!");
            var websiteProcessId = dnnWebsite.GetWorkerProcessId();
            if (!websiteProcessId.HasValue)
            {
                _Logger.LogInfo("Site not running. Pinging..");
                dnnWebsite.Ping();
            }

            websiteProcessId = dnnWebsite.GetWorkerProcessId();
            if (!websiteProcessId.HasValue)
            {
                _Logger.LogInfo("Unable to get website process. Is it running?");
                Success = false;
                return;
            }

            if (!AttachDebugger(websiteProcessId.Value, dte))
            {
                Success = false;
                return;
            }

            if (options.LaunchBrowser)
            {
                var url = string.IsNullOrWhiteSpace(options.LaunchUrl) ? dnnWebsite.Url : options.LaunchUrl;
                Success = LaunchBrowser(url);

            }
        }

        private bool LaunchBrowser(string url)
        {
            System.Diagnostics.Process.Start(url);
            return true;
        }

        private bool TryLoadProcessIdFromTempFile(out int processId)
        {

            // look for a file written allong side this exe to supply a processid.
            processId = 0;
            string path = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;

            //once you have the path you get the directory with:
            var directory = System.IO.Path.GetDirectoryName(path);
            var argsFilePath = Path.Combine(directory, "vsprocessid.tmp");
            _Logger.LogInfo($"Checking for: {argsFilePath}");
            if (!File.Exists(argsFilePath))
            {
                throw new Exception("FILE DOES NOT EXIST: " + argsFilePath);
                //System.Diagnostics.Debugger.Break();
                //_Logger.LogInfo("Unable to determine VS process ID. Not supplied as argument and no 'vsprocessid.tmp' file found next to this exe.");
                //return false;
            }
            using (var reader = new StreamReader(File.OpenRead(argsFilePath)))
            {
                var processIdText = reader.ReadLine();
                if (!int.TryParse(processIdText, out processId))
                {
                    _Logger.LogInfo("Not able to read valid process id integrer from 'vsprocessid.tmp'");
                }
                else
                {
                    return true;
                }
            }

            return false;

        }

        #endregion

        #region Helper Methods

        public bool BuildProjectAndGetOutputZips(BuildProjectOptions options, out FileInfo[] installPackages, out DTE dte)
        {

            // Get an instance of the currently running Visual Studio IDE.
            installPackages = null;
            if (!TryGetEnvDte(options.ProcessId, out dte))
            {
                return false;
            }

            // Register the IOleMessageFilter to handle any threading errors as per: https://msdn.microsoft.com/en-us/library/ms228772(v=vs.100).aspx
            MessageFilter.Register();

            string configurationName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            if (string.IsNullOrWhiteSpace(options.Configuration))
            {
                configurationName = options.Configuration;
            }

            //  dte.Solution.SolutionBuild.Build(true);
            var projects = dte.Solution.Projects;
            var project = projects.OfType<EnvDTE.Project>().FirstOrDefault(p => p.Name == options.ProjectName);
            if (project == null)
            {
                _Logger.LogError(string.Format("Unable to find project named: {0}.", options.ProjectName));
                return false;
            }

            var fullName = project.FullName;
            dte.Solution.SolutionBuild.BuildProject(configurationName, fullName, true);

            // now get output zips
            installPackages = GetProjectOutputZips(project, configurationName);

            // now filter based on whether we want to install the sources or normal install package.
            Microsoft.Build.Evaluation.ProjectCollection collection = new Microsoft.Build.Evaluation.ProjectCollection();
            Microsoft.Build.Evaluation.Project msBuildProject = new Microsoft.Build.Evaluation.Project(project.FullName, null, null, collection, Microsoft.Build.Evaluation.ProjectLoadSettings.IgnoreMissingImports);


            if (options.Sources)
            {
                string sourceszipSuffix = msBuildProject.GetPropertyValue("DnnSourcesZipFileSuffix");
                installPackages =
                    installPackages.Where(a => Path.GetFileNameWithoutExtension(a.Name).ToLowerInvariant().EndsWith(sourceszipSuffix.ToLowerInvariant()))
                        .ToArray();
            }
            else
            {
                string installzipSuffix = msBuildProject.GetPropertyValue("DnnInstallZipFileSuffix");
                installPackages =
                 installPackages.Where(a => Path.GetFileNameWithoutExtension(a.Name).ToLowerInvariant().EndsWith(installzipSuffix.ToLowerInvariant()))
                     .ToArray();
            }

            return true;

        }

        private bool TryGetEnvDte(int processId, out DTE dte)
        {
            dte = null;
            var glob = DotNet.Globbing.Glob.Parse($"!VisualStudio.DTE.*:{processId}");

            // string dteObjectString = string.Format("VisualStudio.DTE.{0}", options.EnvDteVersion);
            // string runningObjectName = string.Format("!{0}:{1}", dteObjectString, options.ProcessId);

            var runningObjects = RunningObjectsTable.GetRunningObjects();
            var visualStudioRunningObject = runningObjects.FirstOrDefault(r => glob.IsMatch(r.name));
            if (visualStudioRunningObject.o == null)
            {
                _Logger.LogError(string.Format("Unable to find Visual Studio instance: {0}. Ensure if VS is running as Admin, then DnnPackager.exe should also be executed from Admin elevated process otherwise it won't find VS. It's also possible DnnPackager.exe doesn't support your visual studio version yet.. Please raise an issue on GitHub.", glob.ToString()));
                foreach (var item in runningObjects)
                {
                    _Logger.LogInfo(string.Format("running object name: {0}", item.name));
                }
                return false;
            }

            dte = (EnvDTE.DTE)visualStudioRunningObject.o;
            return true;
        }

        public FileInfo[] GetProjectOutputZips(EnvDTE.Project project, string configuration)
        {
            string fullPath = project.Properties.Item("FullPath").Value.ToString();

            //  string installzipSuffix = project.Properties.Item("DnnInstallZipFileSuffix").Value.ToString();
            // string sourceszipSuffix = project.Properties.Item("DnnSourcesZipFileSuffix").Value.ToString();

            var projectConfig = project.ConfigurationManager.OfType<Configuration>().FirstOrDefault(c => c.ConfigurationName == configuration);
            string outputPath = projectConfig.Properties.Item("OutputPath").Value.ToString();

            // string installzipSuffix = projectConfig.Properties.Item("DnnInstallZipFileSuffix").Value.ToString();
            // string sourceszipSuffix = projectConfig.Properties.Item("DnnSourcesZipFileSuffix").Value.ToString();


            string outputDir = Path.Combine(fullPath, outputPath);

            var outputFiles = GetInstallZipsFromDirectory(outputDir);
            return outputFiles;


            //var outputGroup = new OutputGroup()

            //foreach (var outputGroup in project.ConfigurationManager.ActiveConfiguration.OutputGroups.OfType<EnvDTE.OutputGroup>())
            //{
            //    LogInfo(string.Format("Output Group: {0}, Desc: {1}, DisplayName: {1}", outputGroup.CanonicalName, outputGroup.Description, outputGroup.DisplayName));

            //    foreach (var strUri in ((object[])outputGroup.FileURLs).OfType<string>())
            //    {
            //        var uri = new Uri(strUri, UriKind.Absolute);
            //        var filePath = uri.LocalPath;
            //        var extension = Path.GetExtension(filePath);
            //        LogInfo(string.Format("Built: {0}", filePath));

            //        if (extension.EndsWith("zip"))
            //        {
            //            var fullFileName = Path.GetFullPath(filePath);
            //            var fileInfo = new FileInfo(fullFileName);
            //            outputFiles.Add(fileInfo);
            //        }

            //    }
            //}

            // var builtGroup = project.ConfigurationManager.ActiveConfiguration.OutputGroups.OfType<EnvDTE.OutputGroup>().First(x => x.CanonicalName == "Built");



            //return outputFiles.ToArray();
        }

        public FileInfo[] GetInstallZipsFromDirectory(string directory)
        {
            var sourcePackagesFolder = directory;
            var sourcePath = Path.GetFullPath(sourcePackagesFolder);
            var sourceDirInfo = new DirectoryInfo(sourcePath);
            // Default to a "Content" subfolder if there is one.
            // This might be the case if the insall package has been extraced from a nuget pachage to this location.
            if (sourceDirInfo.GetDirectories("Content").Any())
            {
                sourceDirInfo = sourceDirInfo.GetDirectories("Content").First();
            }

            var packageFiles = DnnInstallHelper.GetInstallPackagesInDirectory(sourceDirInfo);
            return packageFiles;
        }

        public DotNetNukeWebAppInfo GetDotNetNukeWebsiteInfo(string websiteName)
        {
            return DotNetNukeWebAppInfo.Load(websiteName);
        }

        public bool DeployToIISWebsite(FileInfo[] installZips, DotNetNukeWebAppInfo targetDnnWebsite)
        {
            int retries = 10;
            bool success = false;
            FileInfo[] failedPackages = null;

            if (targetDnnWebsite.Version.Major >= 8)
            {
                success = targetDnnWebsite.DeployPackages(installZips, retries, Console.WriteLine, out failedPackages);

            }
            else
            {
                success = targetDnnWebsite.DeployPackagesViaUrl(installZips, retries, Console.WriteLine, out failedPackages);
            }

            if (!success)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(string.Format("After {0} attempts, the following packages have failed to install:", retries));

                // Get failed install packages.
                foreach (var fileInfo in failedPackages)
                {
                    message.AppendLine(fileInfo.Name);
                }
                message.AppendLine("Some packages failed to install.");
                _Logger.LogError(message.ToString());
                return false;
            }
            else
            {
                _Logger.LogSuccess("Dnn package installation successful.");
                return true;
            }
        }

        private FileInfo[] GetPackagesForDeployment(DebugSolutionOptions options, DTE dte)
        {
            // Register the IOleMessageFilter to handle any threading errors as per: https://msdn.microsoft.com/en-us/library/ms228772(v=vs.100).aspx
            // MessageFilter.Register();
            if (dte == null)
            {
                throw new ArgumentNullException(nameof(dte));
            }

            var solution = dte.Solution;

            if (solution == null)
            {
                throw new Exception(nameof(solution));
            }

            var solutionBuild = solution.SolutionBuild;

            if (solutionBuild == null)
            {
                throw new Exception(nameof(solutionBuild));
            }

            var activeConfiguration = solutionBuild.ActiveConfiguration;

            if (activeConfiguration == null)
            {
                throw new Exception(nameof(activeConfiguration));
            }

            string configurationName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;

            var packagesForDeploy = new List<FileInfo>();
            Microsoft.Build.Evaluation.ProjectCollection collection = new Microsoft.Build.Evaluation.ProjectCollection();

            //  dte.Solution.SolutionBuild.Build(true);

            var projects = DteSolutionExtensions.Projects(dte.Solution, _Logger);
            foreach (var project in projects)
            {
                var configManager = project.ConfigurationManager;

                var activeConfig = project.ConfigurationManager.ActiveConfiguration;
              //  activeConfig.
                // bool isDeployable = activeConfig.IsDeployable;
                //  _Logger.LogInfo($"project: {project.Name} IsDeployable: {isDeployable}");

                var zips = GetProjectOutputZips(project, configurationName);
                if (zips.Any())
                {
                    Microsoft.Build.Evaluation.Project msBuildProject = new Microsoft.Build.Evaluation.Project(project.FullName, null, null, collection, Microsoft.Build.Evaluation.ProjectLoadSettings.IgnoreMissingImports);

                    string suffix = null;
                    if (options.Sources)
                    {
                        suffix = msBuildProject.GetPropertyValue("DnnSourcesZipFileSuffix");
                    }
                    else
                    {
                        suffix = msBuildProject.GetPropertyValue("DnnInstallZipFileSuffix");
                    }

                    var forDeploy = zips.Where(a => Path.GetFileNameWithoutExtension(a.Name).ToLowerInvariant().EndsWith(suffix.ToLowerInvariant())).ToArray();
                    if (forDeploy.Any())
                    {
                        foreach (var item in forDeploy)
                        {
                            _Logger.LogInfo($"Detected package for deployment: {item.Name}");
                            packagesForDeploy.AddRange(forDeploy);
                        }


                    }
                }
            }
            //var projectCount = dte.Solution.Projects.Count;
            //for (int i = 0; i < projectCount; i++)
            //{
            //    var project = dte.Solution.Projects.Item(i);


            //}
            //var projects = dte.Solution.Projects.OfType<EnvDTE.Project>();
            //foreach (var project in projects)
            //{

            //}

            return packagesForDeploy.ToArray();
        }

        private bool AttachDebugger(int processId, DTE dte)
        {
            dte.Debugger.DetachAll();

            return ProcessExtensions.Attach(processId, dte, _Logger.LogInfo);
        }

        #endregion  
    }
}