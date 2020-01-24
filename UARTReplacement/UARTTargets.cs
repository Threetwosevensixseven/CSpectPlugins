using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTReplacement
{
    /// <summary>
    /// This represents the two different Next UARTs. Currently only the ESP UART is replaced by this plugin.
    /// </summary>
    public enum UARTTargets
    {
        ESP,
        Pi
    }
}
