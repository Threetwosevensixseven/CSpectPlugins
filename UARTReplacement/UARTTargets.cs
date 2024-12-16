using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTReplacement
{
    /// <summary>
    /// This represents the two different Next UARTs.
    /// The numeric value determines what is returned from a port PORT_UART_CONTROL (0x153b) read.
    /// Bits 17..15 of the prescaler value are ORed into the bottom three bits of this value.
    /// </summary>
    public enum UARTTargets
    {
        ESP = 0b0_0_000_000,
        Pi = 0b0_1_000_000
    }
}
