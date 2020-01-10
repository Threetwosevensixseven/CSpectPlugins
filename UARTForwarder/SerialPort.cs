using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTForwarder
{
    public class SerialPort : IDisposable
    {
        private System.IO.Ports.SerialPort port;

        public SerialPort(string PortName, int BaudRate)
        {
            try
            {
                port = new System.IO.Ports.SerialPort();
                port.PortName = PortName;
                port.BaudRate = BaudRate;
                port.Parity = System.IO.Ports.Parity.None;
                port.DataBits = 8;
                port.StopBits = System.IO.Ports.StopBits.One;
                port.Handshake = System.IO.Ports.Handshake.None;
                //port.ReadTimeout = 1;
                //port.WriteTimeout = 500;
                port.Open();
            }
            catch
            {
                port = null;
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            //try
            //{
                if (port != null)
                    port.Write(buffer, offset, count);
            //}
            //catch (TimeoutException)
            //{
            //}
        }

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
