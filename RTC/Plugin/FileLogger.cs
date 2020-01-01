using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.RTC.Plugin
{
    public class FileLogger : IDisposable
    {
        private FileStream fileStream;
        private StreamWriter fileWriter;

        public FileLogger(Settings Settings, LogTargets Target)
        {
            if (Target == LogTargets.Master && (Settings == null || !Settings.EnableMasterLogging))
                return;
            if (Target == LogTargets.Slave && (Settings == null || !Settings.EnableSlaveLogging))
                return;
            string fn = Target == LogTargets.Master ? Settings.MasterLogFile : Settings.SlaveLogFile;
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

        public void WriteLine(String Value)
        {
            if (fileWriter != null && Value != null)
                fileWriter.Write(Value + "\r\n");
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
                    if (fileWriter != null)
                    {
                        fileWriter.Dispose();
                        fileWriter = null;
                    }
                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }
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
