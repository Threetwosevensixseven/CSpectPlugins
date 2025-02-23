using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.UARTReplacement
{
    /// <summary>
    /// This class encapsulates a .NET serial port, together with the logic to change the baud
    /// according to instructions received from ythe Next UART I/O ports.
    /// </summary>
    public class SerialPort : IDisposable
    {
        private System.IO.Ports.SerialPort port;
        private int clock = 27000000; // CSpect defaults to HDMI timings
        private int prescaler = 234;  // Next baud defaults to 115200 (more accurately 115384 with integer division)
        private bool enableEspGpio = false;
        private bool enablePiGpio4Output = false;
        private bool enablePiGpio5Output = false;
        private UARTTargets target;
        private string logPrefix;
        private byte prescalerTop3Bits = 0x00;
        private System.IO.Ports.SerialDataReceivedEventHandler handler;
        private Thread readThread;

        /// <summary>
        /// Creates an instance of the SerialPort class.
        /// </summary>
        /// <param name="PortName">The serial port name to bind to, for example COM1.</param>
        public SerialPort(string PortName, UARTTargets Target, System.IO.Ports.SerialDataReceivedEventHandler dataReceivedHandler)
        {
            try
            {
                target = Target;
                handler = dataReceivedHandler;
                logPrefix = target.ToString().Substring(0, 1) + "." + (PortName ?? "").Trim() + ".";
                if (string.IsNullOrWhiteSpace(PortName))
                {
                    port = null;
                    Console.Write(UARTReplacement_Device.PluginName);
                    Console.WriteLine(Target.ToString() + " UART not configured, disabling.");
                    return;
                }
                port = new System.IO.Ports.SerialPort();
                int oldBaud = -1;
                port.PortName = PortName;
                int baud = Baud;
                port.BaudRate = baud > 0 ? baud : 115200;
                port.Parity = System.IO.Ports.Parity.None;
                port.DataBits = 8;
                port.StopBits = System.IO.Ports.StopBits.One;
                port.Handshake = System.IO.Ports.Handshake.None;
                LogClock(oldBaud, baud, false);
                LogPrescaler(oldBaud, baud, "");
                if (dataReceivedHandler != null)
                {
                    if (IsRunningOnMono())
                    {
                        Console.Write(UARTReplacement_Device.PluginName);
                        Console.WriteLine($"Running on Mono, using RX polling thread for {Target.ToString()}.");
                        port.Open();
                        readThread = new Thread(new ThreadStart(this.ReadThread));
                        readThread.Start();
                    }
                    else
                    {
                        port.DataReceived += dataReceivedHandler;
                        port.Open();
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                port = null;
                if (ex.Message.Contains("does not exist"))
                {
                    Console.Write(UARTReplacement_Device.PluginName);
                    Console.WriteLine("Port " + (PortName ?? "").Trim() 
                        + " does not exist, disabling " + Target.ToString() + " UART.");
                }
                else
                {
                    Console.Error.Write(UARTReplacement_Device.PluginName);
                    Console.Error.WriteLine(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                port = null;
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Writes a specified number of bytes to the serial port using data from a buffer.
        /// </summary>
        /// <param name="buffer">The byte array that contains the data to write to the port.</param>
        /// <param name="offset">The zero-based byte offset in the buffer parameter at which to begin 
        /// copying bytes to the port.</param>
        /// <param name="count">The number of bytes to write.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                if (port != null)
                    port.Write(buffer, offset, count);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// If there is a byte available in the UART buffer return it, otherwise a value representing no data.
        /// </summary>
        /// <param name="Success">Output parameter indicating whether the result was a valid byte or no data.</param>
        /// <returns>A byte from the UART buffer, or a value representing no data.</returns>
        public byte ReadByte(out bool Success)
        {
            try
            {
                if (port != null && port.BytesToRead > 0)
                {
                    var b = Convert.ToByte(port.ReadByte());
                    Success = true;
                    return b;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
            Success = false;
            return 0x00;
        }

        /// <summary>
        /// Given a raw byte read from REG_VIDEO_TIMING, and another raw byte written to PORT_UART_CONTROL,
        /// updates the clock and potentially also bits 16:14 of the prescaler.
        /// </summary>
        /// <param name="BaudByte">Read from REG_VIDEO_TIMING, used to update the clock.</param>
        /// <param name="VideoTimingByte">Written to PORT_UART_CONTROL, used to update bits 16:14 of the prescaler.</param>
        /// <returns></returns>
        public int SetPrescalerAndClock(byte BaudByte, byte VideoTimingByte)
        {
            int oldBaud = Baud;
            try
            {
                if ((BaudByte & 0x10) == 0x10)
                {
                    // Get bits 14..16 of the new prescaler
                    prescalerTop3Bits = (byte)(BaudByte & 0b111);
                    //Console.WriteLine(UARTReplacement_Device.PluginName + "Prescaler 17..14: " + ToBin3(prescalerTop3Bits));
                }
                if (port == null)
                    return 0;
                int mode = VideoTimingByte & 7;
                switch (mode)
                {
                    case 0:
                        clock = 28000000;
                        break;
                    case 1:
                        clock = 28571429;
                        break;
                    case 2:
                        clock = 29464286;
                        break;
                    case 3:
                        clock = 30000000;
                        break;
                    case 4:
                        clock = 31000000;
                        break;
                    case 5:
                        clock = 32000000;
                        break;
                    case 6:
                        clock = 33000000;
                        break;
                    case 7:
                        clock = 27000000;
                        break;
                    default:
                        clock = 28000000;
                        break;
                }
                if ((BaudByte & 0x10) == 0x10)
                {
                    int newBits = prescalerTop3Bits << 14;
                    // Mask out everything of the existing prescaler except bits 14..16
                    int oldBits = prescaler & 0x3fff;
                    // Combine the two sets of bits
                    prescaler = oldBits | newBits;
                    port.BaudRate = Baud;
                    LogClock(oldBaud, Baud, false);
                    LogPrescaler(oldBaud, Baud, "16:14");
                }
                else
                {
                    port.BaudRate = Baud;
                    LogClock(oldBaud, Baud);
                }
                return clock;
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Given a raw byte written to PORT_UART_RX, parses the high bit to decide whether it represents a change
        /// to bits 6:0 or 13:7, and updates the prescaler accordingly.
        /// </summary>
        /// <param name="BaudByte">The raw byte written to I/O port PORT_UART_RX.</param>
        /// <returns>Returns the newly recalculated baud.</returns>
        public int SetPrescaler(byte BaudByte)
        {
            int oldBaud = Baud;
            try
            {
                if (port == null)
                    return 0;
                if ((BaudByte & 0x80) == 0)
                {
                    // Get bits 0..6 of the new prescaler
                    int newBits = BaudByte & 0x7f;
                    // Mask out everything of the existing prescaler except bits 0..6
                    int oldBits = prescaler & 0x1ff80;
                    // Combine the two sets of bits
                    prescaler = oldBits | newBits;
                    port.BaudRate = Baud == 1928571 ? 2000000 : Baud;
                    LogPrescaler(oldBaud, Baud, "6:0");
                }
                else
                {
                    // Get bits 7..13 of the new prescaler
                    int newBits = (BaudByte & 0x7f) << 7;
                    // Mask out everything of the existing prescaler except bits 7..13
                    int oldBits = prescaler & 0x1c07f;
                    // Combine the two sets of bits
                    prescaler = oldBits | newBits;
                    port.BaudRate = Baud == 1928571 ? 2000000 : Baud;
                    LogPrescaler(oldBaud, Baud, "13:7");
                    return Baud;
                }
                return Baud;
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Baud is always calculated dynamically from the clock and prescaler, using integer division.
        /// </summary>
        public int Baud
        {
            get
            {
                try
                {
                    // 
                    if (prescaler == 0)
                        return 0;
                    return Convert.ToInt32(Math.Truncate((Convert.ToDecimal(clock) / prescaler)));
                }
                catch (Exception ex)
                {
                    Console.Error.Write(UARTReplacement_Device.PluginName);
                    Console.Error.WriteLine(ex.ToString());
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the value for a PORT_UART_CONTROL (0x153b) read.
        /// Bit 6 is 0 for ESP and 1 for Pi. Bits 2..0 are the top three bits of the prescaler.
        /// All other bits are 0.
        /// </summary>
        /// <returns>The byte value to be returned by a PORT_UART_CONTROL (0x153b) read.</returns>
        public byte GetUartControlValue()
        {
            return (byte)((int)target | prescalerTop3Bits);
        }

        /// <summary>
        /// Handle nextreg 2 for Espressif ESP reset USB conventions.
        /// When nextreg 2, 128 is invoked, set serial RTS to drive ESP /RST low.
        /// When nextreg 2, 0 is invoked, clear serial RETS to release ESP /RST high.
        /// </summary>
        /// <param name="ResetByte">A nextreg 2 control byte value.</param>
        public void EspReset(byte ResetByte)
        {
            try
            {
                if (port is null) return;
                // bit 7 = Indicates the reset signal to the expansion bus and esp is asserted
                bool resetESP = (ResetByte & 128) == 128;
                if (resetESP)
                {
                    Console.WriteLine(UARTReplacement_Device.PluginName + "RTS=" + resetESP + " (drive /RST low)");
                    port.RtsEnable = true;
                }
                else
                {
                    Console.WriteLine(UARTReplacement_Device.PluginName + "RTS=" + resetESP + " (release /RST high)");
                    port.RtsEnable = false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Handle nextreg 0xa8.
        /// When nextreg 0xa8, 1 is invoked, enable ESP GPIO0.
        /// When nextreg 0xa8, 0 is invoked, disable ESP GPIO0.
        /// </summary>
        /// <param name="EnableByte">A nextreg 0xa8 control byte value.</param>
        public void EnableEspGpio(byte EnableByte)
        {
            try
            {
                // bit 0 = ESP GPIO0 output enable
                enableEspGpio = (EnableByte & 1) == 1;
                Console.WriteLine(UARTReplacement_Device.PluginName + "EnableEspGpio=" + enableEspGpio);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Handle nextreg 0xa9.
        /// When nextreg 0xa9, 1 is invoked, set DTR to drive ESP /GPIO0 low.
        /// When nextreg 0xa9, 0 is invoked, clear DTR to release ESP /GPIO0 high.
        /// </summary>
        /// <param name="GpioByte">A nextreg 0xa9 control byte value.</param>
        public void SetEspGpio(byte GpioByte)
        {
            try
            {
                if (enableEspGpio || port is null)
                    return;

                // bit 0 = Read / Write ESP GPIO0 (hard reset = 1)
                bool gpio0 = !((GpioByte & 1) == 1);
                if (gpio0)
                {
                    Console.WriteLine(UARTReplacement_Device.PluginName + "ESP.DTR=" + gpio0 + " (drive /GPIO0 low)");
                    port.DtrEnable = true;
                }
                else
                {
                    Console.WriteLine(UARTReplacement_Device.PluginName + "ESP.DTR=" + gpio0 + " (release /GPIO0 high)");
                    port.DtrEnable = false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Handle nextreg 0x90.
        /// When nextreg 0x90, 16 is invoked, enable Pi GPIO4.
        /// When nextreg 0x90, 0 is invoked, disable Pi GPIO4.
        /// When nextreg 0x90, 32 is invoked, enable Pi GPIO5.
        /// When nextreg 0x90, 0 is invoked, disable Pi GPIO5.
        /// </summary>
        /// <param name="EnableByte">A nextreg 0x90 control byte value.</param>
        public void EnablePiGpio(byte EnableByte)
        {
            try
            {
                // bit 4 = Pi GPIO4 output enable
                bool old = enablePiGpio4Output;
                enablePiGpio4Output = (EnableByte & 16) == 16;
                if (enablePiGpio4Output != old)
                    Console.WriteLine(UARTReplacement_Device.PluginName + "EnablePiGpio4Output=" + enablePiGpio4Output);
                // bit 5 = Pi GPIO5 output enable
                old = enablePiGpio5Output;
                enablePiGpio5Output = (EnableByte & 32) == 32;
                if (enablePiGpio5Output != old)
                    Console.WriteLine(UARTReplacement_Device.PluginName + "EnablePiGpio5Output=" + enablePiGpio5Output);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Handle nextreg 0x98.
        /// When nextreg 0x98, 16 is invoked, set DTR to drive Pi /GPIO4 low.
        /// When nextreg 0x98, 0 is invoked, clear DTR to release Pi /GPIO4 high.
        /// When nextreg 0x98, 32 is invoked, set RTS to drive Pi /GPIO5 low.
        /// When nextreg 0x98, 0 is invoked, clear RTS to release Pi /GPIO5 high.
        /// </summary>
        /// <param name="GpioByte">A nextreg 0x98 control byte value.</param>
        public void SetPiGpio(byte GpioByte)
        {
            try
            {
                if (port is null)
                    return;
                if (enablePiGpio4Output)
                {
                    // bit 4 = Read / Write Pi GPIO4 (soft reset = all 0)
                    bool gpio4 = ((GpioByte & 16) == 16);
                    if (gpio4 && port.DtrEnable != gpio4)
                    {
                        Console.WriteLine(UARTReplacement_Device.PluginName + "Pi.DTR=" + gpio4 + " (drive /GPIO4 low)");
                        port.DtrEnable = true;
                    }
                    else if (!gpio4 && port.DtrEnable != gpio4)
                    {
                        Console.WriteLine(UARTReplacement_Device.PluginName + "Pi.DTR=" + gpio4 + " (release /GPIO4 high)");
                        port.DtrEnable = false;
                    }
                }
                if (enablePiGpio5Output)
                {
                    // bit 5 = Read / Write Pi GPIO5 (soft reset = all 0)
                    bool gpio5 = ((GpioByte & 32) == 32);
                    if (gpio5 && port.RtsEnable != gpio5)
                    {
                        Console.WriteLine(UARTReplacement_Device.PluginName + "Pi.RTS=" + gpio5 + " (drive /GPIO5 low)");
                        port.RtsEnable = true;
                    }
                    else if (!gpio5 && port.RtsEnable != gpio5)
                    {
                        Console.WriteLine(UARTReplacement_Device.PluginName + "Pi.RTS=" + gpio5 + " (release /GPIO5 high)");
                        port.RtsEnable = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Returns true if serial port is enabled.
        /// If any errors occurred at startup opening the port, this will return false.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return port != null;
            }
        }

        /// <summary>
        /// Convenience method to log the clock and calculated baud to the debug console, every time the video timing clock changes.
        /// </summary>
        /// <param name="LogBaud">
        /// Optionally choose not to log the calculated baud, if you know the prescaler is about to be changed
        /// and logged straight afterwareds.
        /// </param>
        private void LogClock(int oldBaud, int newBaud, bool LogBaud = true)
        {
            try
            {
                if (port == null)
                    return;
                //Console.WriteLine(UARTReplacement_Device.PluginName + "Clock=" + clock);
                if (LogBaud && newBaud != oldBaud)
                    Console.WriteLine(UARTReplacement_Device.PluginName + "Setting " + target.ToString() + " baud to " + Baud);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Convenience method to log the prescaler and calculated baud to the debug console, every time the prescaler changes.
        /// </summary>
        /// <param name="BitsChanged"></param>
        private void LogPrescaler(int oldBaud, int newBaud, string BitsChanged)
        {
            try
            {
                if (port == null)
                    return;
                string bstr = Convert.ToString(prescaler, 2).PadLeft(17, '0');
                //Console.WriteLine(UARTReplacement_Device.PluginName + "Prescaler=" + bstr.Substring(0, 3) + " " + bstr.Substring(3, 7) + " " + bstr.Substring(10, 7)
                //    + " (" + prescaler + (string.IsNullOrWhiteSpace(BitsChanged) ? "" : ", changed bits " + BitsChanged) + ")");
                if (newBaud != oldBaud)
                    Console.WriteLine(UARTReplacement_Device.PluginName + "Setting " + target.ToString() + " baud to " + Baud);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTReplacement_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Get string representation of the top three bits of a byte.
        /// </summary>
        /// <param name="value">A value in 0xbNNN format.</param>
        /// <returns></returns>
        private string ToBin3(byte value)
        {
            string val = Convert.ToString(value, 2).PadLeft(8, '0').ToLower();
            return "0b" + val.Substring(5);
        }

        /// <summary>
        /// Returns true if CSpect and the plugin are running inside Mono instead of the .NET 4.x Framework.
        /// This will return true for linux and MacOS.
        /// It can also return true on Windows if you install Mono from https://www.mono-project.com/download/stable/#download-win
        /// and start CSpect with: mono CSpect.exe
        /// </summary>
        /// <returns>True if CSpect and the plugin are running inside Mono</returns>
        private static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        /// <summary>
        /// When running on mono, continuously invoke the plugin's callback handler
        /// for data received events.
        /// </summary>
        private void ReadThread()
        {
            try
            {
                while (true)
                {
                    handler.Invoke(port, null);
                    // Don't sleep here, it will throttle the receive rate.
                    // Yield will prevent 100% CPU.
                    Thread.Yield();
                }
            }
            catch (ThreadInterruptedException)
            {
                // Normal at dispose
            }
            catch (Exception ex)
            {
                // Normal at dispose
                if (!isDisposing)
                {
                    // Still throw any other exception if we're not disposing.
                    Console.Error.Write(UARTReplacement_Device.PluginName);
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private bool isDisposing = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    isDisposing = disposing;
                    readThread?.Interrupt();
                    if (port != null)
                    {
                        if (port.IsOpen)
                            port.Close();
                        port.Dispose();
                        port = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SerialPort() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
