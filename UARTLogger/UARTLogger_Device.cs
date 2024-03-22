using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Plugin;

namespace Plugins.UARTLogger
{
    public class UARTLogger_Device : iPlugin
    {
        private const ushort PORT_UART_TX = 0x133b;
        private const ushort PORT_UART_RX = 0x143b;
        private const ushort PORT_UART_CONTROL = 0x153b;

        private iCSpect CSpect;
        private Settings Settings;
        private UARTBuffer Buffer;
        private bool UART_RX_Internal;

        public static string PluginName = "";

        public List<sIO> Init(iCSpect _CSpect)
        {
            List<sIO> ports = new List<sIO>();
            try
            {
                var assy = Assembly.GetExecutingAssembly();
                PluginName = assy.GetName().Name + " Plugin: ";
                Console.Write(PluginName + "v");
                Console.Write(assy.GetName().Version.ToString() + ".");
                Console.Write(assy.GetAssemblyConfiguration().ToLower());
                Console.WriteLine(" started.");

                // Initialise and load plugin settings
                CSpect = _CSpect;
                Settings = Settings.Load();
                Buffer = new UARTBuffer(Settings);

                // create a list of the ports we're interested in, but only if we're logging
                if (Settings.EnableESPLogging || Settings.EnableESPLogging)
                {
                    ports.Add(new sIO(PORT_UART_TX, eAccess.Port_Write));
                    ports.Add(new sIO(PORT_UART_RX, eAccess.Port_Read));
                    ports.Add(new sIO(PORT_UART_CONTROL, eAccess.Port_Write));
                }
            }
            catch(Exception ex)
            {
                Console.Error.Write(PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
            return ports;
        }

        public void Quit()
        {
            try
            {
                if (Buffer != null)
                {
                    Buffer.Dispose();
                    Buffer = null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                Console.WriteLine(PluginName + "Terminated.");
            }
        }

        public bool Write(eAccess _type, int _port, int _id, byte _value)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.Error.Write(PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
            // Don't handle any writes we didn't register for
            return false;
        }

        public byte Read(eAccess _type, int _port, int _id, out bool _isvalid)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.Error.Write(PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
            // Don't handle any reads we didn't register for
            _isvalid = false;
            return 0xff;
        }

        public void Tick()
        {
        }

        public bool KeyPressed(int _id)
        {
            return false;
        }

        public void Reset()
        {
        }

        public void OSTick()
        {
        }
    }
}
