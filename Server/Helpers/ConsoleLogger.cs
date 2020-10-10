using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Helpers
{
    public static class ConsoleLogger
    {
        public static void Write(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " " + message);
        }
    }
}
