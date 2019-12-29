using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    public class DS1307 : I2CSlave
    {
        public DS1307(I2CBus Bus, UpdateLogEventHandler LogCallback = null)
            : base(Bus, LogCallback)
        {
        }

        public override byte SlaveAddress { get { return 0x68; } }

        public override string DeviceName { get { return "DS1307 RTC"; } }
    }
}
