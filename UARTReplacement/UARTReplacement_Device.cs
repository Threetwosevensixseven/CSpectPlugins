using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Plugin;

namespace Plugins.UARTReplacement
{
    public class UARTReplacement_Device : iPlugin
    {
        private const ushort PORT_UART_TX = 0x133b;
        private const ushort PORT_UART_RX = 0x143b;
        private const ushort PORT_UART_CONTROL = 0x153b;
        private const byte REG_RESET = 0x02;
        private const byte REG_VIDEO_TIMING = 0x11;
        private const byte REG_ESP_GPIO_ENABLE = 0xa8;
        private const byte REG_ESP_GPIO = 0xa9;

        private iCSpect CSpect;
        private UARTTargets Target;
        private SerialPort espPort;
        private Settings settings;
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
            settings = Settings.Load();
            espPort = new SerialPort(settings.PortName);
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
            ports.Add(new sIO(PORT_UART_RX, eAccess.Port_Write));
            ports.Add(new sIO(PORT_UART_RX, eAccess.Port_Read));
            ports.Add(new sIO(PORT_UART_CONTROL, eAccess.Port_Write));
            ports.Add(new sIO(REG_RESET, eAccess.NextReg_Write));
            ports.Add(new sIO(REG_ESP_GPIO_ENABLE, eAccess.NextReg_Write));
            ports.Add(new sIO(REG_ESP_GPIO, eAccess.NextReg_Write));
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
                    // Writes to this port contain siwtches between the ESP and Pi UARTs (we only handle the former),  
                    // and also potential changes to the prescaler which cause the UART baud to change.
                    // We might not need to query REG_VIDEO_TIMING every single time the baud or UART selector changes,
                    // But just in case it can vary, let's do it. It doesn't seem to cause performance issues to do so.
                    var target = (_value & 64) == 0 ? UARTTargets.ESP : UARTTargets.Pi;
                    Target = target;
                    if (Target == UARTTargets.ESP)
                        espPort.SetPrescalerAndClock(_value, CSpect.GetNextRegister(REG_VIDEO_TIMING));
                    // We are transparently logging without handling the write, so return false
                    return false;
                case PORT_UART_TX:
                    if (Target == UARTTargets.ESP)
                    {
                        // This is an outgoing UART byte which should be sent to our buffered UART implementation
                        espPort.Write(new byte[] { _value }, 0, 1);
                        // If we are actively handling the write, return true.
                        return true;
                    }
                    // If we are not handling the write, return false so CSpect's own Pi UART will handle it.
                    return false;
                case PORT_UART_RX:
                    if (Target == UARTTargets.ESP)
                    {
                        // Writes to this port represent changes to the prescaler which cause the UART baud to change
                        espPort.SetPrescaler(_value);
                        return true;
                    }
                    return false;
                case REG_RESET:
                    espPort.Reset(_value);
                    // Always handle this nextreg
                    return true;
                case REG_ESP_GPIO_ENABLE:
                    espPort.EnableEspGpio(_value);
                    // Always handle this nextreg
                    return true;
                case REG_ESP_GPIO:
                    espPort.SetEspGpio(_value);
                    // Always handle this nextreg
                    return true;
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
                        // This is an incoming UART byte which should be read from our buffered UART implementation
                        // Z80 code will only call this if the status flag indicates data is ready when reading PORT_UART_TX.
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
                    // If not already handled, any remaining reads from this port represent data reads
                    // from the Pi UART. So pass this through to let CSpect handle it (which initiates
                    // another recursive call to this method, which we handle above).
                    UART_RX_Internal = true;
                    byte val = CSpect.InPort(PORT_UART_RX);
                    UART_RX_Internal = false;
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
                        // Reads from this port return a status flag in bit 0 indicating whether 
                        // there is any data available to read from the UART buffer.
                        _isvalid = true;
                        if (readBuffer.Count > 0)
                            return 1;
                        else
                            return 0;
                    }
                    // If not already handled, any remaining reads from this port represent data ready status flag
                    // reads from the Pi UART. So pass this through to let CSpect handle it (which initiates
                    // another recursive call to this method, which we handle above).
                    UART_RX_Internal = true;
                    byte val2 = CSpect.InPort(PORT_UART_TX);
                    UART_RX_Internal = false;
                    _isvalid = true;
                    return val2;
            }
            // Don't handle any reads we didn't register for
            _isvalid = false;
            return 0xff;
        }


        // This method is runs on a background thread to continuously read all available bytes
        // proactively from the plugin's serial port, and fill the UART read buffer with them.
        // Access to the buffer from both threads is synchronised with a mutex via the lock() construct.
        // This method It may not offer the best performance, but so far I have not needed to optimise it.
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

