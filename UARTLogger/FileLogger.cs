using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UARTLogger
{
    public class FileLogger : IDisposable
    {
        private FileStream fileStream;
        private StreamWriter fileWriter;

        public FileLogger(Settings Settings, UARTTargets Target)
        {
            if (Target == UARTTargets.ESP && (Settings == null || !Settings.EnableESPLogging))
                return;
            if (Target == UARTTargets.Pi && (Settings == null || !Settings.EnablePiLogging))
                return;
            string fn = Target == UARTTargets.ESP ? Settings.ESPLogFile : Settings.PiLogFile;
            var mode = Settings.TruncateLogsOnStartup ? FileMode.Create : FileMode.Append;
            fileStream = File.Open(fn, mode, FileAccess.Write, FileShare.ReadWrite);
            fileWriter = new StreamWriter(fileStream);
            fileWriter.AutoFlush = true;
        }

        public void Write(String Value)
        {
            if (fileWriter != null && Value != null)
                fileWriter.Write(Value);
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
                    fileWriter.Dispose();
                    fileStream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileLogger() {
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
