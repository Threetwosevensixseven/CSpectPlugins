using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Creates an instance of the SerialPort class.
        /// </summary>
        /// <param name="PortName">The serial port name to bind to, for example COM1.</param>
        public SerialPort(string PortName)
        {
            try
            {
                port = new System.IO.Ports.SerialPort();
                port.PortName = PortName;
                int baud = Baud;
                port.BaudRate = baud > 0 ? baud : 115200;
                port.Parity = System.IO.Ports.Parity.None;
                port.DataBits = 8;
                port.StopBits = System.IO.Ports.StopBits.One;
                port.Handshake = System.IO.Ports.Handshake.None;
                LogClock(false);
                LogPrescaler("");
                port.Open();
            }
            catch (Exception /*ex*/)
            {
                port = null;
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
            if (port != null)
                port.Write(buffer, offset, count);
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
                if (port != null)
                {
                    var b = Convert.ToByte(port.ReadByte());
                    Success = true;
                    return b;
                }
            }
            catch
            {
            }
            Success = false;
            return 0xff;
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
                // Get bits 14..16 of the new prescaler
                int newBits = (BaudByte & 0x07) << 14;
                // Mask out everything of the existing prescaler except bits 14..16
                int oldBits = prescaler & 0x3fff;
                // Combine the two sets of bits
                prescaler = oldBits | newBits;
                port.BaudRate = Baud;
                LogClock(false);
                LogPrescaler("16:14");
            }
            else
            {
                port.BaudRate = Baud;
                LogClock();
            }
            return clock;
        }

        /// <summary>
        /// Given a raw byte written to PORT_UART_RX, parses the high bit to decide whether it represents a change
        /// to bits 6:0 or 13:7, and updates the prescaler accordingly.
        /// </summary>
        /// <param name="BaudByte">The raw byte written to I/O port PORT_UART_RX.</param>
        /// <returns>Returns the newly recalculated baud.</returns>
        public int SetPrescaler(byte BaudByte)
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
                port.BaudRate = Baud;
                LogPrescaler("6:0");
            }
            else
            {
                // Get bits 7..13 of the new prescaler
                int newBits = (BaudByte & 0x7f) << 7;
                // Mask out everything of the existing prescaler except bits 7..13
                int oldBits = prescaler & 0x1c07f;
                // Combine the two sets of bits
                prescaler = oldBits | newBits;
                port.BaudRate = Baud;
                LogPrescaler("13:7");
            }
            return Baud;
        }

        /// <summary>
        /// Baud is always calculated dynamically from the clock and prescaler, using integer division.
        /// </summary>
        public int Baud
        {
            get
            {
                // 
                if (prescaler == 0)
                    return 0;
                return Convert.ToInt32(Math.Truncate((Convert.ToDecimal(clock) / prescaler)));
            }
        }

        public void Reset(byte ResetByte)
        {
            if (port is null) return;
            // bit 7 = Indicates the reset signal to the expansion bus and esp is asserted
            bool resetESP = (ResetByte & 128) == 128;
            if (resetESP)
            {
                Debug.WriteLine("RTS:       " + resetESP + " (drive /RST low)");
                port.RtsEnable = true;
            }
            else
            {
                Debug.WriteLine("RTS:       " + resetESP + " (release /RST high)");
                port.RtsEnable = false;
            }
        }

        public void EnableEspGpio(byte EnableByte)
        {
            // bit 0 = GPIO0 output enable
            bool enableEspGpio = (EnableByte & 1) == 1;
            Debug.WriteLine("EnableGPIO:" + enableEspGpio);
        }

        public void SetEspGpio(byte GpioByte)
        {
            if (enableEspGpio || port is null)
                return;

            // bit 0 = Read / Write ESP GPIO0 (hard reset = 1)
            bool gpio0 = !((GpioByte & 1) == 1);
            if (gpio0)
            {
                Debug.WriteLine("DTR:       " + gpio0 + " (drive /GPIO0 low)");
                port.DtrEnable = true;
            }
            else
            {
                Debug.WriteLine("DTR:       " + gpio0 + " (release /GPIO0 high)");
                port.DtrEnable = false;
            }
        }

        /// <summary>
        /// Convenience method to log the clock and calculated baud to the debug console, every time the video timing clock changes.
        /// </summary>
        /// <param name="LogBaud">
        /// Optionally choose not to log the calculated baud, if you know the prescaler is about to be changed
        /// and logged straight afterwareds.
        /// </param>
        private void LogClock(bool LogBaud = true)
        {
            if (port == null)
                return;
            Debug.WriteLine("Clock:     " + clock);
            if (LogBaud)
                Debug.WriteLine("Baud:      " + Baud);
        }

        /// <summary>
        /// Convenience method to log the prescaler and calculated baud to the debug console, every time the prescaler changes.
        /// </summary>
        /// <param name="BitsChanged"></param>
        private void LogPrescaler(string BitsChanged)
        {
            if (port == null)
                return;
            string bstr = Convert.ToString(prescaler, 2).PadLeft(17, '0');
            Debug.WriteLine("Prescaler: " + bstr.Substring(0, 3) + " " + bstr.Substring(3, 7) + " " + bstr.Substring(10, 7)
                + " (" + prescaler + (string.IsNullOrWhiteSpace(BitsChanged) ? "" : ", changed bits " + BitsChanged) + ")");
            Debug.WriteLine("Baud:      " + Baud);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
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
