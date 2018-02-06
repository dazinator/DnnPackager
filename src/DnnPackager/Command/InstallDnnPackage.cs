using Dazinate.Dnn.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Configuration;

namespace DnnPackager.Command
{
    public class DnnInstallPackageCommand : DnnCommand
    {

        public DnnInstallPackageCommand()
        {


        }

        public string PackageFilePath { get; set; }

        public List<KeyValuePair<string, string>> LogOutput { get; set; }


        public override bool Execute(ICommandHost host)
        {



            var stream = File.OpenRead(this.PackageFilePath);
            var typeName = "DotNetNuke.Services.Installer.Installer";

            var dnnAssemblyPath = Path.Combine(host.DnnWebsitePath, "bin\\dotnetnuke.dll");
            var dnnAssy = System.Reflection.Assembly.LoadFile(dnnAssemblyPath);
            var installerType = dnnAssy.GetType(typeName);

            var installMethod = installerType.GetMethod("Install", BindingFlags.Instance | BindingFlags.Public);
            this.LogOutput = new List<KeyValuePair<string, string>>();

            try
            {

                var args = new object[] { stream, host.DnnWebsitePath, true, true }; // (Stream inputStream, string physicalSitePath, bool loadManifest, bool deleteTemp)
                dynamic instance = Activator.CreateInstance(installerType, args);

                bool valid = instance.IsValid;
                var installerInfo = instance.InstallerInfo;
                var logger = installerInfo.Log;
                var logs = logger.Logs;
                bool failure = false;
                string tempInstallFolder = instance.TempInstallFolder;
                //if(!string.IsNullOrWhiteSpace(tempInstallFolder))
                //{
                //    this.LogOutput.Add(new KeyValuePair<string, string>("Info", $"Deleting temp install dir: {tempInstallFolder}"));
                //    Directory.Delete(tempInstallFolder, true);
                //}

                //var section = System.Configuration.ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                //var mode = section?.FcnMode;
                //var fcnMode = ((ValueType)mode ?? FcnMode.NotSet).ToString();
                //this.LogOutput.Add(new KeyValuePair<string, string>("Info", $"FCN Mode: {fcnMode}"));

                installerInfo.RepairInstall = true;
                foreach (var item in logs)
                {
                    if (item.Type.ToString() == "Failure")
                    {
                        failure = true;
                    }
                    this.LogOutput.Add(new KeyValuePair<string, string>(item.Type.ToString(), item.Description));
                }

                if (valid)
                {
                    dynamic result = installMethod.Invoke(instance, null);
                }

                return valid && !failure;

            }
            catch (Exception e)
            {
                this.LogOutput.Add(new KeyValuePair<string, string>("Exception", e.ToString()));
                throw;
            }

        }


    }
}
