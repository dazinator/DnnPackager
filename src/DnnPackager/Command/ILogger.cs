using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{
    public interface ILogger
    {
        void LogInfo(string message);

        void LogSuccess(string message);

        void LogError(string message);


    }

    public class ConsoleLogger : ILogger
    {              

        public void LogInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

    }
}
