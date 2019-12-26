using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Plugin
{
    public enum LogLevels
    {
        I2CRaw = 0,
        I2CSequence = 1,
        I2CState = 2,
        RTCAction = 3,
        All = int.MinValue
    }
}
