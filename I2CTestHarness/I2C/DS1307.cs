using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    /// <summary>
    /// This concrete implementation of a DS1307 RTC I2C slave device abstracts away much of the plumbing in the I2CSlave
    /// base class, and does away with most of the need to interact with the I2CBus or I2CMaster classes.
    /// Members marked override provide an implementation for specific concrete details the base class or bus needs to know about.
    /// Protected and public members allow the concrete slave device to interface with the bus.
    /// Methods whose name begins with On*, such as OnByteRead or OnByteWritten, are notification methods from the base class, 
    /// originating from the bus but presented here at a higher level of abstraction.
    /// </summary>
    public class DS1307 : I2CSlave
    {
        private CommandStates transactionState;
        private int regPointer;
        private int bytesSinceStart;
        private byte[] registers;

        public DS1307(I2CBus Bus, UpdateLogEventHandler LogCallback = null)
            : base(Bus, LogCallback)
        {
            transactionState = CommandStates.Stopped;
            registers = new byte[64];
            regPointer = -1;
            IncreaseRegPointer();
            registers[62] = (byte)'Z';
            registers[63] = (byte)'X';
        }

        private int IncreaseRegPointer()
        {
            regPointer = (regPointer + 1) % 64;
            Log("** RTC reg=0x" + regPointer.ToString("X2"));
            return regPointer;
        }

        private int SetRegPointer(int Address)
        {
            regPointer = Address % 64;
            Log("** RTC reg=0x" + regPointer.ToString("X2"));
            return regPointer;
        }

        public override byte SlaveAddress { get { return 0x68; } }

        public override string DeviceName { get { return "DS1307 RTC"; } }

        protected override void OnTransactionChanged(CommandStates NewState)
        {
            if (transactionState == CommandStates.Stopped && NewState == CommandStates.Started)
            {
                Log("** RTC transaction started");
                transactionState = NewState;
                bytesSinceStart = 0;
            }
            else if (transactionState == CommandStates.Started && NewState == CommandStates.Started)
            {
                Log("** RTC transaction restarted");
                transactionState = NewState;
                bytesSinceStart = 0;
            }
            else if (transactionState == CommandStates.Started && NewState == CommandStates.Stopped)
            {
                Log("** RTC transaction stopped");
                transactionState = NewState;
            }
        }

        protected override byte OnByteRead()
        {
            byte val = registers[regPointer];
            var chr = '?';
            if (val >= 32 || val < 255)
                chr = Convert.ToChar(val);
            Log("** RTC read reg 0x" + regPointer.ToString("X2") + "=0x" + val.ToString("X2") + " ('" + chr + "')");
            IncreaseRegPointer();
            bytesSinceStart++;
            return val;
        }

        protected override bool OnByteWritten(byte Byte)
        {
            if (bytesSinceStart == 0)
            {
                SetRegPointer(Byte);
            }
            else
            {
                Log("** RTC write reg 0x" + regPointer.ToString("X2") + "=0x" + Byte.ToString("X2"));
                registers[regPointer] = Byte;
                IncreaseRegPointer();
            }
            bytesSinceStart++;
            return true;
        }
    }
}
