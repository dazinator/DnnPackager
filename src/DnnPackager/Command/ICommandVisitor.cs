using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{
    public interface ICommandVisitor
    {
        void VisitDeployCommand(DeployOptions options);
        void VisitBuildCommand(BuildOptions options);
        void VisitInstallTargetsCommand(InstallTargetsToVSProjectFileOptions options);

    }
}
