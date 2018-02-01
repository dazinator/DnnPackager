using System.Management;

namespace System.Diagnostics
{

    public static class ProcessExtensions
    {

        public static bool TryGetParentDevenvProcessId(this Process proc, out int parentProcessId)
        {
            parentProcessId = 0;
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ProcessId = " + proc.Id))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        int parentId = Convert.ToInt32((UInt32)obj["ParentProcessId"]);
                        if (Process.GetProcessById(parentId).ProcessName.ToLower().Contains("devenv"))
                        {
                            parentProcessId = parentId;
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
    }

}
