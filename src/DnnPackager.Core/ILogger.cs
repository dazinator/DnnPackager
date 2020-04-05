namespace DnnPackager.Core
{
    public interface ILogger
    {
        void LogInfo(string message);

        void LogSuccess(string message);

        void LogError(string message);
    }
}
