﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{

    public abstract class CommonOptions : VisitableCommandOptions
    {
        [Option('w', "websitename", Required = true, HelpText = "The name of the dnn website in IIS to deploy to.")]
        public string WebsiteName { get; set; }

    }
}
