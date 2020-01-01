using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Master
{
    public enum I2CLines : ushort
    {
        SCL = 0x103b,
        DATA = 0x113b
    }
}
