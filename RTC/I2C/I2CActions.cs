using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.I2C
{
    public enum I2CActions
    {
        Read = global::Plugin.eAccess.Port_Read,
        Write = global::Plugin.eAccess.Port_Write
    }
}
