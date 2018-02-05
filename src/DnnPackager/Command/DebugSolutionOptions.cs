using CommandLine;

namespace DnnPackager.Command
{
    public class DebugSolutionOptions : CommonOptions
    {
        //[Option('n', "name", Required = true, HelpText = "The project name to build and deploy.")]
        //public string ProjectName { get; set; }

        //[Option('c', "configuration", Required = true, HelpText = "The build configuration to use.")]
        //public string Configuration { get; set; }
        
        [Option('p', "processid", Required = false, HelpText = "The process id for the running visual studio instance.")]
        public int ProcessId { get; set; }      

        [Option('s', "sources", Required = false, DefaultValue = false, HelpText = "If true, will deploy the sources version of the install package, otherwise will install the ordinary install package.")]
        public bool Sources { get; set; }

        [Option('b', "launchbrowser", Required = false, DefaultValue = false, HelpText = "If true, will launch the browser using the url of the denn website instance, or the url provided by --launchurl argument.")]
        public bool LaunchBrowser { get; set; }

        [Option('u', "launchurl", Required = false, DefaultValue = null, HelpText = "Specify the URL to open the browser at. Used in conjunction with --launchbrowser")]
        public string LaunchUrl { get; set; }


        public override void Accept(ICommandVisitor visitor)
        {
            visitor.VisitDebugCommand(this);
        }

    }
}
