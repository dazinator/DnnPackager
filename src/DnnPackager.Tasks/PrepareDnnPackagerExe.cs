using Microsoft.Build.Framework;
using System.IO;
using System.Diagnostics;


namespace DnnPackager.Tasks
{
    public class PrepareDnnPackagerExe : AbstractTask
    {
        public PrepareDnnPackagerExe()
        {
            //todo: if more build servers are supported in future, this can become an array.
        }

        //[Required]
        //public string BuildingInsideVisualStudio { get; set; }         

        public string DnnPackagerExeInstallLocation { get; set; }

        public string MSBuildThisFileFullPath { get; set; }

        public string ProjectDirectory { get; set; }

        public string IntermediateOutputPath { get; set; }

        // Configuration
        public string Configuration { get; set; }

        // MSBuildProjectName
       // public string ProjectName { get; set; }

        public override bool ExecuteTask()
        {
            // Create the args for dnnpackager exe in the intermediate output directory
            LogMessage("Executing OutputDnnPackagerExeArgs", MessageImportance.Normal);
           // LogMessage($"Project Name: {ProjectName} and Configuration: {Configuration}", MessageImportance.Normal);

            if (Process.GetCurrentProcess().TryGetParentDevenvProcessId(out int processId))
            {
                LogMessage($"Parent Process: {processId}", MessageImportance.Normal);
                var outputFolder = Path.Combine(ProjectDirectory, IntermediateOutputPath, CreateDnnExtensionInstallationZip.IntermediateOutputFolderName);
                EnsureArgsFile(processId, Configuration, outputFolder);
                CopyDnnPackagerExeToObj(outputFolder);
            }

            return true;
        }

        public void CopyDnnPackagerExeToObj(string outputFolder)
        {
          //  var dnnPackagerExe = Path.Combine(MSBuildThisFileFullPath, @"..\tools\DnnPackager.exe");
            var destExe = Path.Combine(outputFolder, "DnnPackager.exe");
            LogMessage($"Copying DnnPackager Exe from {DnnPackagerExeInstallLocation} to: {destExe}", MessageImportance.Normal);

            if (File.Exists(destExe))
            {
                File.Delete(destExe);
            }
            File.Copy(DnnPackagerExeInstallLocation, destExe);
        }

        private void EnsureArgsFile(int devenvPid, string configuration, string outputDir)
        {
            LogMessage($"Creating DnnPackager Args file: {devenvPid}, {configuration}, {outputDir}", MessageImportance.Normal);
            var argsFilePath = Path.Combine(outputDir, "vsprocessid.tmp");
            if (File.Exists(argsFilePath))
            {
                File.Delete(argsFilePath);
            }

            using (var writer = new StreamWriter(argsFilePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(devenvPid);
               // writer.WriteLine(configuration);
               // writer.WriteLine(intermediateOutputDirectory);
            }

        }

    }
}
