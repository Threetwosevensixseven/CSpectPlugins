using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
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
        public byte SlaveAddress { get { return 0x00; } }
        public byte WriteAddress { get { return 0x00; } }
        public byte ReadAddress { get { return 0x00; } }

        public string DeviceName { get { return "I2C MASTER"; } }

        public bool IsMaster { get { return true; } }

        public void Log(string Text)
        {
            #if DEBUG
            if (logCallback != null)
                logCallback(Text);
            #endif
        }

        public void Tick(bool NewSDA, bool NewSCL, bool OldSDA, bool OldSCL)
        {
            lastSDA = OldSDA;
            lastSCL = OldSCL;
            LogBus(NewSDA, NewSCL);
        }

        private void LogBus(bool SDA, bool SCL)
        {
            Log("    SDA=" + (SDA ? "1" : "0") + ", SCL=" + (SCL ? "1" : "0"));
        }

        public bool CMD_START()
        {
            // A change in the state of the data line, from HIGH to LOW, while the clock is HIGH, defines a START condition.
            Log("Tx CMD_START");
            bus.SetSDA(this, true);
            bus.SetSCL(this, true);
            bus.SetSDA(this, false); // Falling data edge should trigger slaves
            bus.SetSCL(this, false);
            return true; // Allows chaining commands together with lazy &&
        }

        public void CMD_STOP()
        {
            // STOP condition is defined by a change in the state of the data line, from LOW to HIGH, while the clock line is HIGH
            Log("Tx CMD_STOP");
            bus.SetSDA(this, false);
            bus.SetSCL(this, true);
            bus.SetSDA(this, true); // Rising data edge should trigger slaves
        }

        public bool CMD_TX(byte Byte)
        {
            Log("Tx CMD_TX");
            Log("Tx byte=0x" + Byte.ToString("X2"));
            for (int i = 0; i < 8; i++)
            {
                bool val = ((Byte >> (7 - i)) & 1) == 1;
                SendBit(val, i, "data");
            }
            Log("Waiting for ACK/NACK...");
            bus.SetSDA(this, true);
            bus.SetSCL(this, true);
            bool ack = !bus.SDA; // Read ACK/NACK
            if (ack)
                Log("Rx ACK  bit 8=0");
            else
            {
                Log("Rx NACK bit 8=1");
                Log("Aborting transaction...");
            }
            bus.SetSCL(this, false);
            return ack;
        }

        private void SendBit(bool Bit, int Count, string BitType)
        {
            Log("Tx " + BitType.PadRight(4) + " bit " + Count + "=" + (Bit ? "1" : "0"));
            bus.SetSDA(this, Bit);
            bus.SetSCL(this, true);
            bus.SetSCL(this, false); // Falling clock edge should trigger slaves
        }
    }
}
