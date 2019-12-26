using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC
{
    public class I2CMaster
    {
        private I2CStates state;

        public I2CMaster()
        {
            state = I2CStates.Unknown;
        }

        public void Process(I2CActions Action, I2CLines Line, byte Value)
        {
            Debug.WriteLine(Action.ToString().PadRight(6) + Line.ToString().PadRight(4) + " = 0x" + Value.ToString("x2"));
        }
    }
}
