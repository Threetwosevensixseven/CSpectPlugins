using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.Classes
{
    public class I2CMaster : II2CDevice
    {
        private I2CBus bus;
        private UpdateLogEventHandler logCallback;
        private bool lastSCL;
        private bool lastSDA;

        public I2CMaster(I2CBus Bus, UpdateLogEventHandler LogCallback = null)
        {
            bus = Bus;
            logCallback = LogCallback;
            bus.Register(this);
        }
        public string DeviceName { get { return "I2C MASTER"; } }

        public bool IsMaster { get { return true; } }

        public void Log(string Text)
        {
            if (logCallback != null)
                logCallback(Text);
        }

        public void Tick(bool NewSDA, bool NewSCL)
        {
            lastSCL = bus.SCL;
            lastSDA = bus.SDA;
            LogBus(NewSDA, NewSCL);
        }

        private void LogBus(bool SDA, bool SCL)
        {
            Log("SDA=" + (SDA ? "1" : "0") + ", SCL=" + (SCL ? "1" : "0"));
        }

        public void CMD_START()
        {
            // A change in the state of the data line, from HIGH to LOW, while the clock is HIGH, defines a START condition.
            Log("TX CMD_START");
            // 1) If clock is high then take it low first to avoid triggering a STOP
            if (bus.SCL)
                bus.SetSCL(this, false);
            // 2) Now we can safely take data high if it isn't already
            if (!bus.SDA)
                bus.SetSDA(this, true);
            // 3) Finally take clock high and data low
            bus.SetSCL(this, true);
            bus.SetSDA(this, false);
        }

        public void CMD_STOP()
        {
            // A change in the state of the data line, from LOW to HIGH, while the clock line is HIGH, defines a STOP condition.
            Log("TX CMD_STOP");
            // 1) If clock is high then take it low first to avoid triggering a START
            if (bus.SCL)
                bus.SetSCL(this, false);
            // 2) Now we can safely take data low if it isn't already
            if (bus.SDA)
                bus.SetSDA(this, false);
            // 3) Finally take clock high and data high
            bus.SetSCL(this, true);
            bus.SetSDA(this, true);
        }
    }
}
