using System.IO;

namespace DnnPackager.Core
{
    public interface IBuildServer
    {

        void NewBuildArtifact(FileInfo file);


    }
}
