using EnvDTE;
using Microsoft.Build.BuildEngine;
using System;
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

        public bool Success { get; set; }


        public void VisitBuildCommand(BuildOptions options)
        {
            FileInfo[] installPackages = null;
            DTE dte = null;
            bool attachDebugger = false;
            DotNetNukeWebAppInfo dnnWebsite = null;

            Success = BuildProjectAndGetOutputZips(options, out installPackages, out dte);
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
                var processId = dnnWebsite.GetWorkerProcessId();
                if (!processId.HasValue)
                {
                    _Logger.LogInfo("Unable to find running worker process. Is your website running!?");
                }
                ProcessExtensions.Attach(processId.Value, dte, _Logger.LogInfo);
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

            Microsoft.Build.BuildEngine.Project project = new Microsoft.Build.BuildEngine.Project();
            project.Load(options.ProjectName, ProjectLoadSettings.IgnoreMissingImports);
           
            var projectDir = Path.GetDirectoryName(options.ProjectName);
            _Logger.LogInfo(string.Format("Project Dir is: {0}", projectDir));

            var globalPropsFileName = "DnnPackager.props";
            var toolsDir = options.ToolsPath.TrimEnd(new char[] { '\\', '/' });

            var globalPropsFilePath = Path.Combine(toolsDir, globalPropsFileName);

            EnsureImport(project, globalPropsFilePath);

            var projectPropsFileName = "DnnPackageBuilderOverrides.props";
            var projectPropsFilePath = Path.Combine(projectDir, projectPropsFileName);

            EnsureImport(project, projectPropsFilePath);

            var targetsFileName = "dnnpackager.targets";
            var targetsFilePath = Path.Combine(toolsDir, targetsFileName);

            EnsureImport(project, targetsFilePath);

            // remove legacy imports.
            RemoveImport(project, "DnnPackager.Build.targets");

            // if octopack targets are there ensure they are added after other targets.
            ReImportTargetIfExists(project, "OctoPack.targets");    

            project.Save(options.ProjectName);
            Success = true;
        }

        private void ReImportTargetIfExists(Microsoft.Build.BuildEngine.Project project, string importProjectPath)
        {
            var fileName = Path.GetFileName(importProjectPath);
            var existingImport = project.Imports.Cast<Import>().FirstOrDefault(i => i.ProjectPath.EndsWith(fileName));            
            if (existingImport != null)
            {
                project.Imports.RemoveImport(existingImport);
                project.AddNewImport(existingImport.ProjectPath, existingImport.Condition);
                _Logger.LogInfo(string.Format("Re-imported: {0}", fileName));
            }

        }

        private void RemoveImport(Microsoft.Build.BuildEngine.Project project, string importProjectPath)
        {
            var fileName = Path.GetFileName(importProjectPath);
            var existingImport = project.Imports.Cast<Import>().FirstOrDefault(i => i.ProjectPath.EndsWith(fileName));
            if (existingImport != null)
            {               
                project.Imports.RemoveImport(existingImport);
                _Logger.LogInfo(string.Format("Removed import of: {0}", fileName));
            }
        }

        private void EnsureImport(Microsoft.Build.BuildEngine.Project project, string importProjectPath)
        {
            // Ensure import is present, replace existing if found.
            var fileName = Path.GetFileName(importProjectPath);

            var existingImport = project.Imports.Cast<Import>().FirstOrDefault(i => i.ProjectPath.EndsWith(fileName));
            if (existingImport != null)
            {
                _Logger.LogInfo(string.Format("The existing import will be upgraded for: {0}", fileName));
                project.Imports.RemoveImport(existingImport);
            }


            var projectUri = new Uri(string.Format("file://{0}", project.FullFileName));
            var importFileUri = new Uri(string.Format("file://{0}", importProjectPath));

            var relativeImportFile = projectUri.MakeRelativeUri(importFileUri).ToString().Replace('/', '\\');

            project.AddNewImport(relativeImportFile, null);
            _Logger.LogInfo(string.Format("Successfully imported: {0}", relativeImportFile));
        }

        public bool BuildProjectAndGetOutputZips(BuildOptions options, out FileInfo[] installPackages, out DTE dte)
        {

            // Get an instance of the currently running Visual Studio IDE.
            installPackages = null;
            dte = null;
            string dteObjectString = string.Format("VisualStudio.DTE.{0}", options.EnvDteVersion);
            string runningObjectName = string.Format("!{0}:{1}", dteObjectString, options.ProcessId);

            var runningObjects = RunningObjectsTable.GetRunningObjects();
            var visualStudioRunningObject = runningObjects.FirstOrDefault(r => r.name == runningObjectName);
            if (visualStudioRunningObject.o == null)
            {
                _Logger.LogError(string.Format("Unable to find Visual Studio instance: {0}. Ensure if VS is running as Admin, then DnnPackager.exe should also be executed from Admin elevated process otherwise it won't find VS. It's also possible DnnPackager.exe doesn't support your visual studio version yet.. Please raise an issue on GitHub.", runningObjectName));
                foreach (var item in runningObjects)
                {
                    _Logger.LogInfo(string.Format("running object name: {0}", item.name));
                }
                return false;
            }

            dte = (EnvDTE.DTE)visualStudioRunningObject.o;

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
            //  dte.Solution.SolutionBuild.BuildProject(configurationName, fullName, true);

            // now get output zips
            installPackages = GetProjectOutputZips(project, configurationName);
            return true;

        }

        public FileInfo[] GetProjectOutputZips(EnvDTE.Project project, string configuration)
        {
            string fullPath = project.Properties.Item("FullPath").Value.ToString();

            var projectConfig = project.ConfigurationManager.OfType<Configuration>().FirstOrDefault(c => c.ConfigurationName == configuration);
            string outputPath = projectConfig.Properties.Item("OutputPath").Value.ToString();
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
            FileInfo[] failedPackages;
            int retries = 10;
            bool success = targetDnnWebsite.DeployPackages(installZips, retries, Console.WriteLine, out failedPackages);

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
    }
}
