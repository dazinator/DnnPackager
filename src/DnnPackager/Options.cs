using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager
{

    public class CommonOptions
    {
        [Option('w', "websitename", Required = true, HelpText = "The name of the dnn website in IIS to deploy to.")]
        public string WebsiteName { get; set; }
    }

    public class DeployOptions : CommonOptions
    {
        [Option('d', "dir", Required = true, HelpText = "The full path of the directory containing the install zip packages to deploy.")]
        public string DirectoryPath { get; set; }

    }

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


    }


    public class Options
    {

        [VerbOption("deploy", HelpText = "Deploy packages to a local DNN website in IIS.")]
        public DeployOptions DeployVerb { get; set; }

        [VerbOption("build", HelpText = "Build a visual studio project and deploy the packages to a local DNN website in IIS.")]
        public BuildOptions BuildVerb { get; set; }

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

    }
}
