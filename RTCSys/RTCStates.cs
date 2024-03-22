using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTCSys
{
    public enum RTCStates
    {
        Uninitialised = 0,
        Initialised = 1,
        ReadingDateLSB = 2,
        ReadingDateMSB = 3,
        ReadingTimeLSB = 4,
        ReadingTimeMSB = 5,
        ReadingSeconds = 6
    }
}
