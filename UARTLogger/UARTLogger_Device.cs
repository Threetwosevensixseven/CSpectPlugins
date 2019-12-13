using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;

namespace UARTLogger
{
    public class UARTLogger_Device : iPlugin
    {
        public const ushort PORT_UART_TX = 0x133b;
        public const ushort PORT_UART_RX = 0x143b;
        public const ushort PORT_UART_CONTROL = 0x153b;

        public iCSpect CSpect;
        public Settings Settings;
        public UARTBuffer Buffer;
        public bool UART_RX_Internal;

        public List<sIO> Init(iCSpect _CSpect)
        {
            // Initialise and load plugin settings
            CSpect = _CSpect;
            Settings = Settings.Load();
            Buffer = new UARTBuffer(Settings);

            // Read current setting of UART control
            byte b = CSpect.InPort(PORT_UART_CONTROL);
            // Returns 255 on first read, so assume we start off as ESP
            //CurrentUARTType = (_value & 64) == 0 ? UARTTypes.ESP : UARTTypes.Pi;

            // create a list of the ports we're interested in, but only if we're logging
            List<sIO> ports = new List<sIO>();
            if (Settings.EnableESPLogging || Settings.EnableESPLogging)
            {
                ports.Add(new sIO(PORT_UART_TX, eAccess.Port_Write));
                ports.Add(new sIO(PORT_UART_RX, eAccess.Port_Read));
                ports.Add(new sIO(PORT_UART_CONTROL, eAccess.Port_Write));
            }
            return ports;
        }

        public void Quit()
        {
            Buffer.Dispose();
        }

        public bool Write(eAccess _type, int _port, byte _value)
        {
            switch (_port)
            {
                case PORT_UART_CONTROL:
                    var target = (_value & 64) == 0 ? UARTTargets.ESP : UARTTargets.Pi;
                    Buffer.ChangeUARTType(target);
                    //Debug.WriteLine("Switched UART to " + target.ToString());
                    // We are transparently logging without handling the write, so return false
                    return false;
                case PORT_UART_TX:
                    // We are transparently logging without handling the write, so return false
                    Buffer.Log(_value, UARTStates.Writing);
                    //Debug.WriteLine("TX: " + _value.ToString("X2"));
                    return false;
            }
            // Don't handle any writes we didn't register for
            return false;
        }

        public byte Read(eAccess _type, int _port, out bool _isvalid)
        {
            switch (_port)
            {
                case PORT_UART_RX:
                    if (UART_RX_Internal)
                    {
                        // If this callback was triggered by the CSpect.InPort read from our own plugin,
                        // then return without handling the callback.
                        _isvalid = false;
                        return 0;
                    }
                    // Otherwise, the callback was triggered by another plugin or Z80 code running inside
                    // the emulator, so tell our plugin not to response to the next read callback,
                    // then read the actual value, then handle the callback with the read value.
                    UART_RX_Internal = true;
                    byte val = CSpect.InPort(PORT_UART_RX);
                    Buffer.Log(val, UARTStates.Reading);
                    UART_RX_Internal = false;
                    //Debug.WriteLine("RX: " + val.ToString("X2"));
                    _isvalid = true;
                    return val;
            }
            // Don't handle any reads we didn't register for
            _isvalid = false;
            return 0xff;
        }
    }
}
