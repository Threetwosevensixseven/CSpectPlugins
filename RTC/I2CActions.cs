using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC
{
    public enum I2CActions
    {
        Read = Plugin.eAccess.Port_Read,
        Write = Plugin.eAccess.Port_Write
    }
}
