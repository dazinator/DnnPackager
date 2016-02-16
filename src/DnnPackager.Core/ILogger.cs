using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Core
{
    public interface ILogger
    {
        void LogInfo(string message);

        void LogSuccess(string message);

        void LogError(string message);


    }
}
