using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTReplacement
{
    public class Buffer : Queue<Byte>
    {
        public readonly string PortName;
        public readonly UARTTargets Target;
        public readonly string LogPrefix;


        public Buffer(string portName, UARTTargets target)
        {
            PortName = portName;
            Target = target;
            LogPrefix = target.ToString().Substring(0, 1) + "." + (portName ?? "").Trim() + ".";
        }
    }
}
