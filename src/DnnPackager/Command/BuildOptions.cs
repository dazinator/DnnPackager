using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{
    public class BuildOptions : CommonOptions
    {
        [Option('n', "name", Required = true, HelpText = "The project name to build and deploy.")]
        public string ProjectName { get; set; }

        [Option('c', "configuration", Required = true, HelpText = "The build configuration to use.")]
        public string Configuration { get; set; }

        [Option('e', "envdteversion", Required = true, HelpText = "The version of envdte for the running visual studio instance.")]
        public string EnvDteVersion { get; set; }

        [Option('p', "processid", Required = true, HelpText = "The process id for the running visual studio instance.")]
        public int ProcessId { get; set; }

        [Option('a', "attach", Required = false, HelpText = "Whether to attach the debugger after the deployment is complete.")]
        public bool Attach { get; set; }

        [Option('s', "sources", Required = false, DefaultValue = false, HelpText = "If true, will deploy the sources version of the install package, otherwise will install the ordinary install package.")]
        public bool Sources { get; set; }

        public override void Accept(ICommandVisitor visitor)
        {
            visitor.VisitBuildCommand(this);
        }

    }
}
