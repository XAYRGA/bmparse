using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bmparse.debug
{

    public enum MessageLevel
    {
        ERROR,
        WARNING,
        INFO,
    }

    public static class DebugSystem
    {
        public static void message(string message, MessageLevel level = MessageLevel.INFO)
        {
#if DEBUG
            var cc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("bmparse - ");
            if (level == MessageLevel.ERROR)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (level == MessageLevel.WARNING)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else
                Console.ForegroundColor = cc;
            Console.WriteLine(message);
            Console.ForegroundColor = cc;
#endif

        }
    }
}
