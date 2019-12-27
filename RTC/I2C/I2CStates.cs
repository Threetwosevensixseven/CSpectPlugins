using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.I2C
{
    public enum I2CStates
    {
        Stopped,
        Stopping,
        Started,
        Starting,
        ReceivingByte,
    }
}
