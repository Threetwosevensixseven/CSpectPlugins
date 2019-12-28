using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Plugin;

namespace Plugins.RTC.Slave
{
    public class DS1307Device
    {
        public const byte DEVICE_ADDRESS = 0x68;

        private Logger Logger;
        private byte[] Addresses;

        public DS1307Device(Logger Logger)
        {
            this.Logger = Logger;
            Addresses = new byte[64];
        }

        public bool HasAddress(byte Address)
        {
            // The Address passed in is shifted one bit to the left, 
            // with the new bit 0 set for reads, and unset for writes.
            // Shift it one bit to the right to match this device's address.
            byte match = Convert.ToByte(Address >> 1);
            return match == DEVICE_ADDRESS;
        }

        public bool HasAddress(IEnumerable<byte> Bytes)
        {
            if (Bytes == null || Bytes.Count() == 0)
                return false;
            return HasAddress(Bytes.FirstOrDefault());
        }

        private RTCActions GetAction(IEnumerable<byte> Bytes)
        {
            // Assume Read if no device address (should never happen)
            if (Bytes == null || Bytes.Count() == 0)
                return RTCActions.Read;
            // Bit 0 of device address: set = Write, unset = Read
            var action = (RTCActions)(Bytes.FirstOrDefault() & 1);
            return action;
        }

        public void Process(IEnumerable<byte> Bytes)
        {
            // Only respond to addresses matching our device address
            if (!HasAddress(Bytes))
                return;

            var action = GetAction(Bytes);
            Logger.Log(LogLevels.RTCAccess, "DS1307 " + action.ToString() + " bytes: " + Bytes.ToLogString());

            // Check allowed commands
            var bytes = Bytes.ToArray();
            var address = bytes.Length > 1 ? bytes[1] : 0xff;
            
            // Write Date
            if (action == RTCActions.Write && address == 4)
            {
                bool valid = false;
                DateTime date = DateTime.MinValue;
                int day = 0;
                int month = 0;
                int year = 0;
                if (bytes.Length == 5)
                {
                    day = bytes[2].FromBCD();
                    month = bytes[3].FromBCD();
                    year = 2000 + bytes[4].FromBCD();
                    string formatted = day.ToString("D2") + "/" + month.ToString("D2") + "/" + year.ToString("D4");
                    valid = DateTime.TryParseExact(formatted, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                    // We may have a valid date in .NET Framework, but DS1307 only accepts years between 00..99
                    valid = valid && date.Year >= 2000 && date.Year <= 2099;
                }
                if (valid)
                {
                    Logger.Log(LogLevels.RTCCommand, "DS1307 Setting date to: " + date.ToString("dd/MM/yyyy"));
                }
                else
                {
                    Logger.Log(LogLevels.RTCCommand, "DS1307 Malformed command to reg 0x" + address.ToString("x2"));
                }
            }

            // Write RAM
            else if (action == RTCActions.Write && address >= 0x08 && address <= 0x3f)
            {
                bool valid = true;
                var payload = bytes.Skip(2).ToArray();
                int endAddress = address + payload.Length - 1;
                if (endAddress < address || endAddress > 0x3f)
                    valid = false;
                if (valid)
                {
                    string count = payload.Length + payload.Length == 1 ? "byte" : "bytes";
                    Logger.Log(LogLevels.RTCCommand, "DS1307 Write " + count + " to RAM 0x" + address.ToString("x2") 
                        + ": " + payload.ToLogString() + payload.ToASCII());
                    for (byte i = 0; i < payload.Length; i++)
                        Addresses[address + i] = payload[i];
                }
                else
                {
                    Logger.Log(LogLevels.RTCCommand, "DS1307 Malformed write to RAM 0x" + address.ToString("x2"));
                }
            }

            // Unknown commands
            else
            {
                Logger.Log(LogLevels.RTCCommand, "DS1307 Unknown command");
            }

            //if (Address >= 0x00 && Address <= 0x07)
            //{
            //    Logger.Log(LogLevels.RTCAccess, "Set DS1307 Reg 0x" + Address.ToString("x2") 
            //        + " = 0x" + Value.ToString("x2"));
            //    Addresses[Address] = Value;
            //}
            //else if (Address >= 0x08 && Address <= 0x3F)
            //{
            //    Logger.Log(LogLevels.RTCAccess, "Set DS1307 RAM 0x" + Address.ToString("x2") 
            //        + " = 0x" + Value.ToString("x2"));
            //    Addresses[Address] = Value;
            //}
            //else
            //{
            //    Logger.Log(LogLevels.RTCAccess, "Set DS1307 invalid address " + Address.ToString("x2") 
            //        + " = 0x" + Value.ToString("x2"));
            //}
        }

        public byte Read(byte Address)
        {
            if (Address >= 0x00 && Address <= 0x07)
            {
                Logger.Log(LogLevels.RTCAccess, "Get DS1307 Reg 0x" + Address.ToString("x2") 
                    + " = 0x" + Addresses[Address].ToString("x2"));
                return Addresses[Address];
            }
            else if (Address >= 0x08 && Address <= 0x3F)
            {
                Logger.Log(LogLevels.RTCAccess, "Get DS1307 RAM 0x" + Address.ToString("x2") 
                    + " = 0x" + Addresses[Address].ToString("x2"));
                return Addresses[Address];
            }
            else
            {
                Logger.Log(LogLevels.RTCAccess, "Get DS1307 invalid address " + Address.ToString("x2"));
                return 0;
            }
        }
    }
}
