namespace DnnPackager.Command
{
    public interface ICommandVisitor
    {
        void VisitDeployCommand(DeployOptions options);
        void VisitBuildCommand(BuildProjectOptions options);
        void VisitDebugCommand(DebugSolutionOptions options);
        void VisitInstallTargetsCommand(InstallTargetsToVSProjectFileOptions options);

    }
}
