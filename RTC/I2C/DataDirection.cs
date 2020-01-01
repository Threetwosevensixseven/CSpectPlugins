using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTC.I2C
{
    /// <summary>
    /// Command states are used by the abstract I2CSlave class to represent the directionality of received bytes, 
    /// depending on LSB of the eight-bit command address received from the I2CBus.
    /// </summary>
    public enum DataDirection
    {
        Read,
        Write
    }
}
