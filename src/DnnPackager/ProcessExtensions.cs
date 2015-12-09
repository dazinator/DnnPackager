using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
                    var targetProcess = processes.Cast<Process>().FirstOrDefault(proc => proc.ProcessID == processId);
                    if (targetProcess == null)
                    {
                        return false;
                    }

                    targetProcess.Attach();
                    logger(String.Format("Attached to process {0} successfully.", targetProcess.Name));
                    return true;

                }
                catch (COMException)
                {
                    logger(String.Format("Trying to attach to processs.."));
                    System.Threading.Thread.Sleep(1000);
                }
            }

            return false;
        }
    }
}
