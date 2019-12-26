using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Plugin
{
    public class Logger
    {
        public const int INDENT = 2;
        public LogLevels LogThisLevelOrHigher { get; private set; }

        public Logger(LogLevels LogThisLevelOrHigher)
        {
            this.LogThisLevelOrHigher = LogThisLevelOrHigher;
        }
    }

    public static class LoggerExtensions
    {
        // Extenson method allows us to call Log() on a null object without worrying
        public static void Log(this Logger This, LogLevels LogLevel, string Text)
        {
            if (This == null)
                return;
            if (LogLevel >= This.LogThisLevelOrHigher)
                Debug.WriteLine(new string(' ', (int)LogLevel * Logger.INDENT) + (Text ?? ""));
        }
    }
}
