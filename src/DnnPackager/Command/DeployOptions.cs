using CommandLine;

namespace DnnPackager.Command
{
    public class DeployOptions : CommonOptions
    {
        [Option('d', "dir", Required = true, HelpText = "The full path of the directory containing the install zip packages to deploy.")]
        public string DirectoryPath { get; set; }

        public override void Accept(ICommandVisitor visitor)
        {
            visitor.VisitDeployCommand(this);
        }
    }
}
