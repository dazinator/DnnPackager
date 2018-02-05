using DnnPackager.Core;
using EnvDTE;
using System.Collections.Generic;

namespace DnnPackager
{
    public static class DteSolutionExtensions
    {

        public const string SolutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        public const string MiscFilesKind = "{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}";


        public static IList<Project> Projects(this Solution solution, ILogger logger)
        {
            Projects projects = solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                var kind = project.Kind;
                //  logger.LogInfo(kind);
                if (kind == SolutionFolderKind)
                {
                    //  logger.LogInfo("Solution Folder");
                    list.AddRange(GetSolutionFolderProjects(project, logger));
                }
                else if (kind == MiscFilesKind)
                {
                    // don't include.
                }
                else
                {
                 //   logger.LogInfo("Project: " + project.Name);
                    list.Add(project);
                }
            }

            return list;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder, ILogger logger)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                var kind = subProject.Kind;
                // If this is another solution folder, do a recursive call, otherwise add
                if (kind == SolutionFolderKind)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject, logger));
                }
                else if (kind != MiscFilesKind)
                {
                  //  logger.LogInfo("Sub Project: " + subProject.Name);
                    list.Add(subProject);
                }
            }
            return list;
        }
    }
}
