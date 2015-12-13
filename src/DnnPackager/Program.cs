using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DnnPackager
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {

            string invokedVerb = null;
            CommonOptions invokedVerbInstance = null;

            var options = new Options();
            bool success = false;

            bool parsed = CommandLine.Parser.Default.ParseArguments(args, options, (verb, subOptions) =>
            {
                invokedVerb = verb;
                invokedVerbInstance = (CommonOptions)subOptions;
            });

            if (!parsed)
            {               
                // write args
                LogInvalidArgs(args);
                LogInfo(options.GetUsage());
                return -1;
            }

            FileInfo[] installPackages = null;
            DTE dte = null;
            bool attachDebugger = false;
            DotNetNukeWebAppInfo dnnWebsite = null;

            switch (invokedVerb)
            {
                case "build":
                    var buildArgs = (BuildOptions)invokedVerbInstance;
                    success = BuildProjectAndGetOutputZips(buildArgs, out installPackages, out dte);
                    attachDebugger = buildArgs.Attach;
                    break;
                case "deploy":
                    installPackages = GetInstallZipsFromDirectory(((DeployOptions)invokedVerbInstance).DirectoryPath);
                    break;
            }

            dnnWebsite = GetDotNetNukeWebsiteInfo(invokedVerbInstance.WebsiteName);

            if (installPackages != null && installPackages.Any())
            {
                success = DeployToIISWebsite(installPackages, dnnWebsite);
            }
            else
            {
                // no packages to install.
                // log warning?
                LogInfo("No packages to install.");
                success = true;
            }

            if (!success)
            {
                return -1;
            }

            if (dte != null && attachDebugger)
            {

                LogInfo("Hooking up your debugger!");
                var processId = dnnWebsite.GetWorkerProcessId();
                if (!processId.HasValue)
                {
                    LogInfo("Unable to find running worker process. Is your website running!?");
                }
                ProcessExtensions.Attach(processId.Value, dte, LogInfo);
            }

            return 0;
        }

        private static void LogInvalidArgs(string[] args)
        {
            LogInfo("Could not parse arguments: ");
            int x = 0;
            foreach (var item in args)
            {
                LogInfo(string.Format("arg {0}, value enclosed in double asterix: **{1}**", x, item));
                x = x + 1;
            }
        }

        private static bool BuildProjectAndGetOutputZips(BuildOptions options, out FileInfo[] installPackages, out DTE dte)
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
                LogError(string.Format("Unable to find Visual Studio instance: {0}. Ensure if VS is running as Admin, then DnnPackager.exe should also be executed from Admin elevated process otherwise it won't find VS. It's also possible DnnPackager.exe doesn't support your visual studio version yet.. Please raise an issue on GitHub.", runningObjectName));
                foreach (var item in runningObjects)
                {
                    LogInfo(string.Format("running object name: {0}", item.name));
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
                LogError(string.Format("Unable to find project named: {0}.", options.ProjectName));
                return false;
            }

            var fullName = project.FullName;
            //  dte.Solution.SolutionBuild.BuildProject(configurationName, fullName, true);

            // now get output zips
            installPackages = GetProjectOutputZips(project, configurationName);
            return true;

        }

        private static void LogInfo(string message)
        {
            Console.WriteLine(message);
        }

        private static FileInfo[] GetProjectOutputZips(EnvDTE.Project project, string configuration)
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

        private static FileInfo[] GetInstallZipsFromDirectory(string directory)
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

        private static DotNetNukeWebAppInfo GetDotNetNukeWebsiteInfo(string websiteName)
        {
            return DotNetNukeWebAppInfo.Load(websiteName);
        }

        private static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static bool DeployToIISWebsite(FileInfo[] installZips, DotNetNukeWebAppInfo targetDnnWebsite)
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
                LogError(message.ToString());
                return false;
            }
            else
            {
                LogSuccess("Dnn package installation successful.");
                return true;
            }
        }
    }
}
