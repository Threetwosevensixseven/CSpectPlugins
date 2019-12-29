using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.Classes
{
    public class I2CSlave : II2CDevice
    {
        private I2CBus bus;
        private UpdateLogEventHandler logCallback;
        private bool lastSCL;
        private bool lastSDA;
        private CommandStates state;

        public I2CSlave(I2CBus Bus, UpdateLogEventHandler LogCallback = null)
        {
            bus = Bus;
            logCallback = LogCallback;
            state = CommandStates.Stopped;
            bus.Register(this);
            Log("State: " + state.ToString());
        }

        public string DeviceName { get { return "DS1307 RTC"; } }

        public bool IsMaster { get { return false; } }

        public void Log(string Text)
        {
            if (logCallback != null)
                logCallback(Text);
        }

        private void LogBus(bool SDA, bool SCL)
        {
            Log("SDA=" + (SDA ? "1" : "0") + ", SCL=" + (SCL ? "1" : "0"));
        }

        public void Tick(bool NewSDA, bool NewSCL)
        {
            lastSCL = bus.SCL;
            lastSDA = bus.SDA;
            LogBus(NewSDA, NewSCL);

            // Process CMD_START
            // A change in the state of the data line, from HIGH to LOW, while the clock is HIGH, defines a START condition.
            if ((state == CommandStates.Stopped || state == CommandStates.Started) && NewSCL && lastSDA && !NewSDA)
            {
                Log("RX CMD_START");
                state = CommandStates.Started;
                Log("State: " + state.ToString());
            }

            // Process CMD_STOP
            // A change in the state of the data line, from LOW to HIGH, while the clock line is HIGH, defines a STOP condition.
            else if (state == CommandStates.Started && NewSCL && !lastSDA && NewSDA)
            {
                Log("RX CMD_STOP");
                state = CommandStates.Stopped;
                Log("State: " + state.ToString());
            }
        }
    }
}
