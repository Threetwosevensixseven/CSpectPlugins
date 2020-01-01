using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Debug;

namespace RTC.I2C
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
        private bool enableClock;
        private bool mode24Hour;
        private bool clockUpdated;
        private long offset;

            
        public DS1307(I2CBus Bus, ILogger Logger = null)
            : base(Bus, Logger)
        {
            enableClock = true;
            mode24Hour = false;
            clockUpdated = false;
            offset = 0;
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
                CopyTimeToReg();
            }
            else if (transactionState == CommandStates.Started && NewState == CommandStates.Started)
            {
                Log("** RTC transaction restarted");
                transactionState = NewState;
                bytesSinceStart = 0;
                CopyTimeToReg();
            }
            else if (transactionState == CommandStates.Started && NewState == CommandStates.Stopped)
            {
                Log("** RTC transaction stopped");
                transactionState = NewState;
                CopyRegToTime();
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
                if (regPointer >= 0 && regPointer <= 6)
                    clockUpdated = true; // Set buffer dirty flag
                registers[regPointer] = Byte;
                IncreaseRegPointer();
            }
            bytesSinceStart++;
            return true;
        }

        private void CopyTimeToReg()
        {
            Log("** Copying current date/time to clock buffers...");
            var now = new DateTime(DateTime.Now.Ticks - offset);
            Log("** Freezing date/time at " + now.ToString("s"));
            Log("** Current date/time offset is " + offset);
            string d;
            if (mode24Hour)
                d = now.ToString("ddMMyyHHmmss");
            else
                d = now.ToString("ddMMyyhhmmsst");
            // Reg 0: Seconds (0..59)
            registers[0] = Convert.ToByte(((((byte)d[10] - (byte)'0') * 16) + ((byte)d[11] - (byte)'0')) | (enableClock ? 0 : 128));
            // Reg 1: Minutes (0..59)
            registers[1] = Convert.ToByte((((byte)d[8] - (byte)'0') * 16) + ((byte)d[9] - (byte)'0'));
            // Reg 2: Hours (0..24 or 0..12)
            if (mode24Hour)
                registers[2] = Convert.ToByte((((byte)d[6] - (byte)'0') * 16) + ((byte)d[7] - (byte)'0'));
            else
                registers[2] = Convert.ToByte(((((byte)d[6] - (byte)'0') * 16) + ((byte)d[7] - (byte)'0')) | 64 | (d[12] == 'P' ? 32 : 0));
            // Reg 3: Day of week (Sun=1, Sat=7)
            registers[3] = Convert.ToByte(now.DayOfWeek + 1);
            // Reg 4: Day of month aka Date (1..31)
            registers[4] = Convert.ToByte((((byte)d[0] - (byte)'0') * 16) + ((byte)d[1] - (byte)'0'));
            // Reg 5: Month (1..12)
            registers[5] = Convert.ToByte((((byte)d[2] - (byte)'0') * 16) + ((byte)d[3] - (byte)'0'));
            // Reg 6: Year (0.99)
            registers[6] = Convert.ToByte((((byte)d[4] - (byte)'0') * 16) + ((byte)d[5] - (byte)'0'));
            // Reset buffer dirty flag
            clockUpdated = false;
        }

        private void CopyRegToTime()
        {
            if (!clockUpdated)
            {
                Log("** Clock buffers unchanged, discarding");
                return;
            }
            Log("** Clock buffers have changed, updating clock...");
            // Reg 6: Year (0.99)
            string d = "20"
                + (char)(((registers[6] >> 4) & 15) + '0')
                + (char)((registers[6] & 15) + '0') + '-'
            // Reg 5: Month (1..12)
                + (char)(((registers[5] >> 4) & 15) + '0')
                + (char)((registers[5] & 15) + '0') + '-'
            // Reg 4: Day of month aka Date (1..31)
                + (char)(((registers[4] >> 4) & 15) + '0')
                + (char)((registers[4] & 15) + '0') + "T";
            // Reg 3: Day of week (Sun=1, Sat=7) - copy set this, as it's not really a property of the date
            // Reg 2: Hours (0..24 or 0..12)
            bool newMode24Hour = (registers[2] & 64) == 0;
            string h;
            if (newMode24Hour)
            {
                h = ((char)(((registers[2] >> 4) & 15) + '0')).ToString()
                + (char)((registers[2] & 15) + '0');
            }
            else
            {
                bool pm = (registers[2] & 32) != 0;
                h = ((char)(((registers[2] >> 4) & 1) + '0')).ToString()
                + (char)((registers[2] & 15) + '0');
                if ((registers[2] & 32) != 0) // pm
                {
                    int hh;
                    int.TryParse(h, out hh);
                    h = (hh + 12).ToString("D2");
                }
            }
            d += h + ':'
            // Reg 1: Minutes (0..59)
                + (char)(((registers[1] >> 4) & 15) + '0')
                + (char)((registers[1] & 15) + '0') + ":"
            // Reg 0: Seconds (0..59)
                + (char)(((registers[0] >> 4) & 7) + '0')
                + (char)((registers[0] & 15) + '0');
            bool newEnableClock = (registers[2] & 128) == 0;

            DateTime newDate;
            if (DateTime.TryParseExact(d, "s", CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate))
            {
                Log("** Updating date/time to " + newDate.ToString("s"));
                var now = new DateTime(DateTime.Now.Ticks);
                offset = (now - newDate).Ticks;
                Log("** New date/time offset is " + offset);
                if (newEnableClock != enableClock)
                {
                    if (newEnableClock)
                        Log("** Enabling clock");
                    else
                        Log("** Disabling clock");
                    enableClock = newEnableClock;
                }
            }
            else
            {
                Log("** Ignoring invalid date/time " + d);
            }
            // Reset buffer dirty flag
            clockUpdated = false;
        }

        public static DateTime ConvertDateTime(byte[] Bytes, int Start = 0)
        {
            var dt = DateTime.MinValue;
            // Reg 6: Year (0.99)
            string d = "20"
                + (char)(((Bytes[6 + Start] >> 4) & 15) + '0')
                + (char)((Bytes[6 + Start] & 15) + '0') + '-'
                // Reg 5: Month (1..12)
                + (char)(((Bytes[5 + Start] >> 4) & 15) + '0')
                + (char)((Bytes[5 + Start] & 15) + '0') + '-'
                // Reg 4: Day of month aka Date (1..31)
                + (char)(((Bytes[4 + Start] >> 4) & 15) + '0')
                + (char)((Bytes[4 + Start] & 15) + '0') + "T";
            // Reg 3: Day of week (Sun=1, Sat=7) - copy set this, as it's not really a property of the date
            // Reg 2: Hours (0..24 or 0..12)
            bool newMode24Hour = (Bytes[2 + Start] & 64) == 0;
            string h;
            if (newMode24Hour)
            {
                h = ((char)(((Bytes[2 + Start] >> 4) & 15) + '0')).ToString()
                + (char)((Bytes[2 + Start] & 15) + '0');
            }
            else
            {
                bool pm = (Bytes[2 + Start] & 32) != 0;
                h = ((char)(((Bytes[2 + Start] >> 4) & 1) + '0')).ToString()
                + (char)((Bytes[2 + Start] & 15) + '0');
                if ((Bytes[2 + Start] & 32) != 0) // pm
                {
                    int hh;
                    int.TryParse(h, out hh);
                    h = (hh + 12).ToString("D2");
                }
            }
            d += h + ':'
                // Reg 1: Minutes (0..59)
                + (char)(((Bytes[1 + Start] >> 4) & 15) + '0')
                + (char)((Bytes[1 + Start] & 15) + '0') + ":"
                // Reg 0: Seconds (0..59)
                + (char)(((Bytes[0 + Start] >> 4) & 7) + '0')
                + (char)((Bytes[0 + Start] & 15) + '0');

            DateTime newDate;
            if (!DateTime.TryParseExact(d, "s", CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate))
                newDate = DateTime.MinValue;

            return newDate;
        }

    }
}
