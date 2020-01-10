using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Plugin;

namespace Plugins.UARTForwarder
{
    public class UARTLogger_Device : iPlugin
    {
        private const ushort PORT_UART_TX = 0x133b;
        private const ushort PORT_UART_RX = 0x143b;
        private const ushort PORT_UART_CONTROL = 0x153b;

        private iCSpect CSpect;
        private UARTTargets Target;
        private SerialPort espPort;
        private bool UART_RX_Internal;
        private bool UART_TX_Internal;
        private Thread espThread;
        private bool threadCancel;
        private Queue<byte> readBuffer;
        private object sync;

        public List<sIO> Init(iCSpect _CSpect)
        {
            // Initialise and load plugin settings
            readBuffer = new Queue<byte>();
            sync = new object();
            CSpect = _CSpect;
            Target = UARTTargets.ESP;
            espPort = new SerialPort("COM5", 115200);
            UART_RX_Internal = false;
            UART_TX_Internal = false;
            threadCancel = false;
            espThread = new Thread(ESPWork);
            espThread.IsBackground = true;
            espThread.Start();

            // create a list of the ports we're interested in, but only if we're logging
            List<sIO> ports = new List<sIO>();
                ports.Add(new sIO(PORT_UART_TX, eAccess.Port_Write));
                ports.Add(new sIO(PORT_UART_TX, eAccess.Port_Read));
                ports.Add(new sIO(PORT_UART_RX, eAccess.Port_Read));
                ports.Add(new sIO(PORT_UART_CONTROL, eAccess.Port_Write));
            return ports;
        }

        public void Quit()
        {
            threadCancel = true;
            if (espPort != null)
            {
                espPort.Dispose();
                espPort = null;
            }
        }

        public bool Write(eAccess _type, int _port, byte _value)
        {
            switch (_port)
            {
                case PORT_UART_CONTROL:
                    var target = (_value & 64) == 0 ? UARTTargets.ESP : UARTTargets.Pi;
                    Target = target;
                    //Debug.WriteLine("Switched UART to " + target.ToString());
                    // We are transparently logging without handling the write, so return false
                    return false;
                case PORT_UART_TX:
                    if (Target == UARTTargets.ESP)
                    {
                        espPort.Write(new byte[] { _value }, 0, 1);
                        return true;
                    }
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
                    if (Target == UARTTargets.ESP)
                    {
                        _isvalid = true;
                        if (readBuffer.Count > 0)
                        {
                            lock (sync)
                            {
                                return readBuffer.Dequeue();
                            }
                        }
                        else
                            return 0xff;
                    }
                    UART_RX_Internal = true;
                    byte val = CSpect.InPort(PORT_UART_RX);
                    UART_RX_Internal = false;
                    //Debug.WriteLine("RX: " + val.ToString("X2"));
                    _isvalid = true;
                    return val;
                case PORT_UART_TX:
                    if (UART_TX_Internal)
                    {
                        // If this callback was triggered by the CSpect.InPort read from our own plugin,
                        // then return without handling the callback.
                        _isvalid = false;
                        return 0;
                    }
                    if (Target == UARTTargets.ESP)
                    {
                        _isvalid = true;
                        if (readBuffer.Count > 0)
                            return 1;
                        else
                            return 0;
                    }
                    UART_RX_Internal = true;
                    byte val2 = CSpect.InPort(PORT_UART_TX);
                    UART_RX_Internal = false;
                    //Debug.WriteLine("RX: " + val.ToString("X2"));
                    _isvalid = true;
                    return val2;
            }
            // Don't handle any reads we didn't register for
            _isvalid = false;
            return 0xff;
        }

        public void ESPWork()
        {
            while(true)
            {
                if (threadCancel)
                    return;
                bool read;
                var b = espPort.ReadByte(out read);
                if (read)
                {
                    lock(sync)
                    {
                        readBuffer.Enqueue(b);
                    }
                }
                Thread.Sleep(1);
            }
        }
    }
}

