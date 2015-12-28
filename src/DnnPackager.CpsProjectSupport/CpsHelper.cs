using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace DnnPackager.CpsProjectSupport
{
    public static class CpsHelper
    {
        public static async Task GetMsBuildProject(IProjectLockService projectLockService, UnconfiguredProject unconfiguredProject, System.Action<Project> configureCallback)
        {
            Debugger.Break();
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

                // party on it, respecting the type of lock you've acquired. 

                // If you're going to change the project in any way, 
                // check it out from SCC first:
                await access.CheckoutAsync(configuredProject.UnconfiguredProject.FullPath);

                configureCallback(project);

                // save changes.
                project.Save(project.FullPath);

            }
        }
    }
}

