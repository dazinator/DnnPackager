using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnnPackager.Tasks
{
    public abstract class AbstractTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            try
            {
                LogProperties();
                return ExecuteTask();
            }
            catch (Exception ex)
            {
                LogError("DNN" + ex.GetType().Name.GetHashCode(), ex.Message);
                LogError("DNN" + ex.GetType().Name.GetHashCode(), ex.ToString());
                return false;              
            }

        }

        public abstract bool ExecuteTask();

        protected void LogMessage(string message, MessageImportance importance = MessageImportance.High)
        {
            if (BuildEngine != null)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs("DnnPackager: " + message, "DnnPackager", "DnnPackager", importance));
            }
        }

        protected void LogWarning(string code, string message)
        {
            if (BuildEngine != null)
            {
                BuildEngine.LogWarningEvent(new BuildWarningEventArgs("DnnPackager", code, null, 0, 0, 0, 0, message, "DnnPackager", "DnnPackager"));
            }
        }

        protected void LogError(string code, string message)
        {
            if (BuildEngine != null)
            {
                BuildEngine.LogErrorEvent(new BuildErrorEventArgs("DnnPackager", code, null, 0, 0, 0, 0, message, "DnnPackager", "DnnPackager"));
            }
        }

        protected void LogProperties()
        {
            LogMessage("---Properties---", MessageImportance.Low);
            foreach (var prop in this.GetType().GetProperties())
            {
                var propValue = prop.GetValue(this, null);
                string propValueToLog = "--EMPTY--";
                if (propValue != null)
                {
                    propValueToLog = propValue.ToString();
                }
                LogMessage(string.Format("{0} = {1}", prop.Name, propValueToLog), MessageImportance.Low);
            }
        }
    }
}
