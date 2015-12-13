using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace DnnPackager
{
    public struct RunningObject
    {
        public string name;
        public object o;
    }

    public static class RunningObjectsTable
    {

        // Returns the contents of the Running Object Table (ROT), where
        // open Microsoft applications and their documents are registered.
        public static List<RunningObject> GetRunningObjects()
        {
            // Get the table.
            List<RunningObject> res = new List<RunningObject>();
            IBindCtx bc;
            CreateBindCtx(0, out bc);
            IRunningObjectTable runningObjectTable;
            bc.GetRunningObjectTable(out runningObjectTable);
            IEnumMoniker monikerEnumerator;
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();

            // Enumerate and fill our nice dictionary.
            IMoniker[] monikers = new IMoniker[1];
            IntPtr numFetched = IntPtr.Zero;
          
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                RunningObject running;
                monikers[0].GetDisplayName(bc, null, out running.name);
                runningObjectTable.GetObject(monikers[0], out running.o);
                res.Add(running);
            }
            return res;

        }

        /// <summary>
        /// Returns a pointer to an implementation of IBindCtx (a bind context object). 
        /// This object stores information about a particular moniker-binding operation.
        /// </summary>
        /// <param name="reserved">This parameter is reserved and must be 0.</param>
        /// <param name="ppbc">Address of an IBindCtx* pointer variable that receives 
        /// the interface pointer to the new bind context object. When the function is 
        /// successful, the caller is responsible for calling Release on the bind context. 
        /// A NULL value for the bind context indicates that an error occurred.</param>
        /// <returns>This function can return the standard return values E_OUTOFMEMORY and S_OK.</returns>
        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    }
}
