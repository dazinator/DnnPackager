using Microsoft.Build.Framework;
using System.IO;

namespace DnnPackager.Tasks
{
    public static class TaskItemExtensions
    {
        public static string GetFileNameWithoutExtension(this ITaskItem taskItem, string projectDirectory)
        {
            var path = GetFullPath(taskItem, projectDirectory);
            var fileName = Path.GetFileNameWithoutExtension(path);
            return fileName;
        }

        public static string GetFileExtension(this ITaskItem taskItem)
        {
           // var path = GetFullPath(taskItem, projectDirectory);
            var fileName = Path.GetExtension(taskItem.ItemSpec);
            return fileName;
        }

        public static string GetFullPath(this ITaskItem taskItem, string projectDirectory)
        {
            var path = Path.Combine(projectDirectory, taskItem.ItemSpec);
            return path;
        }



    }
}
