using DnnPackager.Core;
using Microsoft.Build.Construction;
using System;
using System.IO;
using System.Linq;

namespace DnnPackager
{
    public class InstallTargetsHelper
    {
        private ILogger _Logger;

        public InstallTargetsHelper(ILogger logger)
        {
            _Logger = logger;
        }

        public bool Install(Microsoft.Build.Evaluation.Project project, string toolsPath)
        {
            var projectDir = Path.GetDirectoryName(project.FullPath);
            _Logger.LogInfo(string.Format("Project Dir is: {0}", projectDir));

            var globalPropsFileName = "DnnPackager.props";
            var toolsDir = toolsPath.TrimEnd(new char[] { '\\', '/' });

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
            project.Save(project.FullPath);

            return true;
        }

        private void ReImportTargetIfExists(Microsoft.Build.Evaluation.Project project, string importProjectPath)
        {
            var fileName = Path.GetFileName(importProjectPath);
            var existingImport = project.Xml.Imports.Cast<ProjectImportElement>().FirstOrDefault(i => i.Project.EndsWith(fileName));

            if (existingImport != null)
            {
                project.Xml.RemoveChild(existingImport);
                project.Xml.AddImport(existingImport.Project);
                _Logger.LogInfo(string.Format("Re-imported: {0}", fileName));
            }

        }

        private void RemoveImport(Microsoft.Build.Evaluation.Project project, string importProjectPath)
        {
            var fileName = Path.GetFileName(importProjectPath);
            var existingImport = project.Xml.Imports.Cast<ProjectImportElement>().FirstOrDefault(i => i.Project.EndsWith(fileName));
            if (existingImport != null)
            {
                project.Xml.RemoveChild(existingImport);
                _Logger.LogInfo(string.Format("Removed import of: {0}", fileName));
            }
        }

        private void EnsureImport(Microsoft.Build.Evaluation.Project project, string importProjectPath)
        {
            // Ensure import is present, replace existing if found.
            var fileName = Path.GetFileName(importProjectPath);
            var existingImport = project.Xml.Imports.Cast<ProjectImportElement>().FirstOrDefault(i => i.Project.EndsWith(fileName));
            if (existingImport != null)
            {
                _Logger.LogInfo(string.Format("The existing import will be upgraded for: {0}", fileName));
                project.Xml.RemoveChild(existingImport);

            }

            var projectUri = new Uri(string.Format("file://{0}", project.FullPath));
            var importFileUri = new Uri(string.Format("file://{0}", importProjectPath));

            var relativeImportFile = projectUri.MakeRelativeUri(importFileUri).ToString().Replace('/', '\\');
            project.Xml.AddImport(relativeImportFile);

            _Logger.LogInfo(string.Format("Successfully imported: {0}", relativeImportFile));
        }

    }


}
