using DnnPackager.Core;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace DnnPackager.CpsProjectSupport
{

    public class TraceLogger : ILogger
    {
        public void LogError(string message)
        {
            Trace.TraceError(message);
        }

        public void LogInfo(string message)
        {
            Trace.TraceInformation(message);
        }

        public void LogSuccess(string message)
        {
            Trace.WriteLine(message);
        }
    }

    public static class CpsHelper
    {
        public static async Task InstallTargets(EnvDTE.Project envDteProject, IProjectLockService projectLockService, UnconfiguredProject unconfiguredProject, string toolsPath)
        {

            var logger = new TraceLogger();

            try
            {

                if (projectLockService == null)
                {
                    throw new ArgumentNullException("projectLockService");
                }

                if (unconfiguredProject == null)
                {
                    throw new ArgumentNullException("unconfiguredProject");
                }

                using (var access = await projectLockService.WriteLockAsync())
                {


                    var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
                    Project project = await access.GetProjectAsync(configuredProject);

                    // If you're going to change the project in any way, 
                    // check it out from SCC first:
                    await access.CheckoutAsync(configuredProject.UnconfiguredProject.FullPath);

                    // install targets

                    var installer = new InstallTargetsHelper(logger);
                    installer.Install(project, toolsPath);
                    // configureCallback(project);

                    // envDteProject.Save(envDteProject.FullName);

                    // save changes.
                    //project.Save(project.FullPath);

                }

            }
            catch (AggregateException ex)
            {
                logger.LogError(ex.Message);
                foreach (var e in ex.InnerExceptions)
                {
                    logger.LogError(e.Message);
                }

                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }
    }
}

