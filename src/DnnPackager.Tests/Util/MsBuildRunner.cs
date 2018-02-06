using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnnPackager.Tests.Util
{
   
    public static class MsBuildRunner
    {

        public static void MsBuild(string commandLineArguments)
        {
            MsBuild(commandLineArguments, null);
        }

        public static void MsBuild(string commandLineArguments, Action<string> outputValidator)
        {
            var netFx = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var msBuild = Path.Combine(netFx, "msbuild.exe");
            if (!File.Exists(msBuild))
            {
                Assert.Fail("Could not find MSBuild at: " + msBuild);
            }

            var allOutput = new StringBuilder();

            Action<string> writer = (output) =>
            {
                allOutput.AppendLine(output);
                Console.WriteLine(output);
            };

            var result = SilentProcessRunner.ExecuteCommand(msBuild, commandLineArguments, Environment.CurrentDirectory, writer, e => writer("ERROR: " + e));

            if (result != 0)
            {
                Assert.Fail("MSBuild returned a non-zero exit code: " + result);
            }

            if (outputValidator != null)
            {
                outputValidator(allOutput.ToString());
            }
        }

    }
}
