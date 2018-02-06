using CommandLine;
using CommandLine.Text;
using System.Text;

namespace DnnPackager.Command
{

    public class Options : VisitableCommandOptions
    {

        [VerbOption("deploy", HelpText = "Deploy packages to a local DNN website in IIS.")]
        public DeployOptions DeployVerb { get; set; }

        [VerbOption("build", HelpText = "Build a visual studio project and deploy the packages to a local DNN website in IIS.")]
        public BuildProjectOptions BuildVerb { get; set; }

        [VerbOption("debug", HelpText = "Deploy all packages within a visual studio solution to the local Dnn websites and then attatch the visual studio debugger.")]
        public DebugSolutionOptions DebugSolutionVerb { get; set; }

        [VerbOption("install-targets", HelpText = "Install the DnnPackager targets to a visual studio project file.")]
        public InstallTargetsToVSProjectFileOptions InstallTargetsVerb { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine("Dnn Packager");
            usage.AppendLine(HelpText.AutoBuild(this).ToString());
            return usage.ToString();
        }

        public override void Accept(ICommandVisitor visitor)
        {
            if (this.DeployVerb != null)
            {
                this.DeployVerb.Accept(visitor);
            }

            if (this.BuildVerb != null)
            {
                this.BuildVerb.Accept(visitor);
            }

            if (this.InstallTargetsVerb != null)
            {
                this.InstallTargetsVerb.Accept(visitor);
            }

            if (this.DebugSolutionVerb != null)
            {
                this.InstallTargetsVerb.Accept(visitor);
            }
        }

    }
}
