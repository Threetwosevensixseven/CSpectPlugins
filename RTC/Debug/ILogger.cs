using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Debug
{
    public interface ILogger
    {
        void Append(string Text);
        void AppendLine(string Text);
    }

    /// <summary>
    /// ILogger extension methods allow members to be called on a null object.
    /// </summary>
    public static class LoggerExtensions
    {
        public static void Append(this ILogger This, string Text)
        {
            if (This != null)
                This.Append(Text);
        }

        public static void AppendLine(this ILogger This, string Text)
        {
            if (This != null)
                This.AppendLine(Text);
        }

    }

    public enum LogTargets
    {
        Master,
        Slave,
        Bus
    }
}
