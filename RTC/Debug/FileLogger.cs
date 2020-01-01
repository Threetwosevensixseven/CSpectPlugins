using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Plugin;

namespace Plugins.RTC.Debug
{
    public class FileLogger : ILogger, IDisposable
    {
        private FileStream fileStream;
        private StreamWriter fileWriter;

        public FileLogger(Settings Settings, LogTargets Target)
        {
            if (Target == LogTargets.Master && (Settings == null || !Settings.EnableMasterLogging))
                return;
            if (Target == LogTargets.Slave && (Settings == null || !Settings.EnableSlaveLogging))
                return;
            if (Target == LogTargets.Bus && (Settings == null || !Settings.EnableBusLogging))
                return;
            string fn;
            if (Target == LogTargets.Master)
                fn = Settings.MasterLogFile;
            else if (Target == LogTargets.Slave)
                fn = Settings.SlaveLogFile;
            else
                fn = Settings.BusLogFile;
            var mode = Settings.TruncateLogsOnStartup ? FileMode.Create : FileMode.Append;
            fileStream = File.Open(fn, mode, FileAccess.Write, FileShare.ReadWrite);
            fileWriter = new StreamWriter(fileStream);
            fileWriter.AutoFlush = true;
        }

        public void Append(String Value)
        {
            if (fileWriter != null && Value != null)
                fileWriter.Write(Value);
        }

        public void AppendLine(String Value)
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
