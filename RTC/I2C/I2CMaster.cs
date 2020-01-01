using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Debug;

namespace RTC.I2C
{
    /// <summary>
    /// The I2CMaster class contains functionality to interface with the I2CBus class, and indirectly through the bus,
    /// communicate with one or more I2CSlave concrete subclass implementations.
    /// It has some primitive public command methods prefixed with CMD_*, which can be hooked up to a test harness
    /// or the I/O ports of an emulator.
    /// </summary>
    public class I2CMaster : II2CDevice
    {
        private I2CBus bus;
        private ILogger log;
        private bool lastSCL;
        private bool lastSDA;

        public I2CMaster(I2CBus Bus, ILogger Logger = null)
        {
            bus = Bus;
            log = Logger;
            bus.Register(this);
        }
        public byte SlaveAddress { get { return 0x00; } }
        public byte WriteAddress { get { return 0x00; } }
        public byte ReadAddress { get { return 0x00; } }

        public string DeviceName { get { return "I2C MASTER"; } }

        public bool IsMaster { get { return true; } }

        public void Log(string Text)
        {
            if (log != null)
                log.AppendLine(Text);
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
            bus.SetSCL(this, true); // This should trigger the slave to put the ACK/NACK on the data line
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

        public Byte CMD_RX(bool LastByte = false)
        {
            Log("Tx CMD_RX");
            byte val = 0;
            for (int i = 0; i < 8; i++)
            {
                bool bit = ReceiveBit(i, "data");
                int newVal = bit ? 1 : 0;
                val |= Convert.ToByte(newVal << (7 - i));
                //Log("Val=0x" + val.ToString("X2"));
            }
            if (LastByte) // For last byte, send NACK
                Log("Tx NACK bit 8=1");
            else
                Log("Tx ACK  bit 8=0");
            bus.SetSDA(this, LastByte); // Master should put ACK/NACK on the data line here
            bus.SetSCL(this, true);     // This should trigger the slave to sample an ACK/NACK on the data line
            bus.SetSCL(this, false);
            var chr = '?';
            if (val >= 32 || val < 255)
                chr = Convert.ToChar(val);
            Log("Rx byte=0x" + val.ToString("X2") + " ('" + chr + "')");
            return val;
        }

        private void SendBit(bool Bit, int Count, string BitType)
        {
            Log("Tx " + BitType.PadRight(4) + " bit " + Count + "=" + (Bit ? "1" : "0"));
            bus.SetSDA(this, Bit);   // Master should present data for slave here
            bus.SetSCL(this, true);
            bus.SetSCL(this, false); // Falling clock edge should trigger slave to sample here
        }

        private bool ReceiveBit(int Count, string BitType)
        {
            bus.SetSCL(this, true);  // Falling clock edge should trigger slaves to present data here (subsequent bits)
            bus.SetSCL(this, false); // or here (first bit)
            bool bit = bus.SDA;      // Master should sample data directly after falling clock edge 
            Log("Rx " + BitType.PadRight(4) + " bit " + Count + "=" + (bit ? "1" : "0"));
            return bit;
        }
    }
}
