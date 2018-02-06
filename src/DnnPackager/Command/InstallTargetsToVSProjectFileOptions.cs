using CommandLine;

namespace DnnPackager.Command
{
    public class InstallTargetsToVSProjectFileOptions : VisitableCommandOptions
    {

        [Option('p', "projectfilepath", Required = true, HelpText = "The full path of the project file to add the dnnpackager targets to.")]
        public string ProjectName { get; set; }

        [Option('t', "toolspath", Required = true, HelpText = "The full path to the dnnpackager tools directory, which contains the props and targets files to be installed.")]
        public string ToolsPath { get; set; }

        public override void Accept(ICommandVisitor visitor)
        {
            visitor.VisitInstallTargetsCommand(this);
        }
    }
}
