using EnvDTE;
using System;
using System.Runtime.InteropServices;

namespace DnnPackager
{
    public static class ProcessExtensions
    {
        public static bool Attach(int processId, EnvDTE.DTE dte, Action<string> logger)
        {
            // Try loop - Visual Studio may not respond the first time.
            int tryCount = 5;
            while (tryCount-- > 0)
            {
                try
                {
                    Processes processes = dte.Debugger.LocalProcesses;
                    Process targetProcess = null;
                    foreach (Process process in processes)
                    {

                        if (process.ProcessID == processId)
                        {
                            targetProcess = process;
                        }
                    }

                    if (targetProcess != null)
                    {
                        logger(String.Format("Attaching to process {0}.", targetProcess.Name));
                        targetProcess.Attach();
                        logger(String.Format("Attached to process {0} successfully.", targetProcess.Name));
                        return true;
                    }

                    logger(String.Format("Unable to find a process with ID: {0} - is your website still running?", processId));
                    return false;

                }
                catch (COMException)
                {
                    logger(String.Format("Will retry attaching to process in a second.."));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            return false;
        }
    }
}
