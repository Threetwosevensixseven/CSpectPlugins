using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;

namespace Plugins.RTC
{
    public class RTC_Device : iPlugin
    {
        private bool Read_Internal;
        private iCSpect CSpect;
        private I2CMaster i2c;

        public List<sIO> Init(iCSpect _CSpect)
        {
            CSpect = _CSpect;
            i2c = new I2CMaster();
            var ports = new List<sIO>();
            ports.Add(new sIO((int)I2CLines.SCL, eAccess.Port_Read));
            ports.Add(new sIO((int)I2CLines.SCL, eAccess.Port_Write));
            ports.Add(new sIO((int)I2CLines.DATA, eAccess.Port_Read));
            ports.Add(new sIO((int)I2CLines.DATA, eAccess.Port_Write));
            return ports;
        }

        public void Quit()
        {
        }

        public byte Read(eAccess _type, int _address, out bool _isvalid)
        {
            // Only handle the two Next I/O ports corresponding to the I2C SCL and Data lines
            if (_type == eAccess.Port_Read && (_address == (int)I2CLines.SCL || _address == (int)I2CLines.DATA))
            {
                if (Read_Internal)
                {
                    // If this callback was triggered by the CSpect.InPort read from our own plugin,
                    // then return without handling the callback.
                    _isvalid = false;
                    return 0;
                }
                // Otherwise, the callback was triggered by another plugin or Z80 code running inside
                // the emulator, so tell our plugin not to response to the next read callback,
                // then read the actual value, then handle the callback with the read value.
                Read_Internal = true;
                I2CLines line = (I2CLines)_address;
                byte val = CSpect.InPort((ushort)line);
                i2c.Process(I2CActions.Read, line, val);
                Read_Internal = false;
            }

            _isvalid = false;
            return 0;
        }

        public bool Write(eAccess _type, int _port, byte _value)
        {
            if (_type == eAccess.Port_Write && (_port == (int)I2CLines.SCL || _port == (int)I2CLines.DATA))
            {
                I2CLines line = (I2CLines)_port;
                i2c.Process(I2CActions.Write, line, _value);
            }
            return false;
        }
    }
}
