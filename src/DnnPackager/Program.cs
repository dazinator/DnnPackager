﻿using DnnPackager.Command;
using DnnPackager.Core;
using System;

namespace DnnPackager
{

    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            try
            {
                string invokedVerb = null;
                IVisitableOptions invokedVerbInstance = null;

                var options = new Options();

                bool parsed = CommandLine.Parser.Default.ParseArguments(args, options, (verb, subOptions) =>
                {
                    invokedVerb = verb;
                    invokedVerbInstance = (IVisitableOptions)subOptions;
                });

              

                if (!parsed)
                {
                    // write args
                    LogInvalidArgs(args, logger);
                    logger.LogInfo(options.GetUsage());
                    return -1;
                }

                var commandVisitor = new CommandVisitor(logger);
                invokedVerbInstance.Accept(commandVisitor);

                if (!commandVisitor.Success)
                {
                    return -1;
                }
              
                return 0;
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                if(!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Launch();
                }
                else
                {
                    System.Diagnostics.Debugger.Break();
                }
               
                throw;
            }

        
          
        }      

        private static void LogInvalidArgs(string[] args, ILogger logger)
        {

            logger.LogInfo("Could not parse arguments: ");
            int x = 0;
            foreach (var item in args)
            {
                logger.LogInfo(string.Format("arg {0}, value enclosed in double asterix: **{1}**", x, item));
                x = x + 1;
            }
        }
               
    }
}
