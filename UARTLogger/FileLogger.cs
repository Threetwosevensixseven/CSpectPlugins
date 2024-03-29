﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.UARTLogger
{
    public class FileLogger : IDisposable
    {
        private FileStream fileStream;
        private StreamWriter fileWriter;

        public FileLogger(Settings Settings, UARTTargets Target)
        {
            try
            {
                if (Target == UARTTargets.ESP && (Settings == null || !Settings.EnableESPLogging))
                {
                    Console.WriteLine(UARTLogger_Device.PluginName + "Disabling " + Target.ToString() + " logging.");
                    return;
                }
                if (Target == UARTTargets.Pi && (Settings == null || !Settings.EnablePiLogging))
                {
                    Console.WriteLine(UARTLogger_Device.PluginName + "Disabling " + Target.ToString() + " logging.");
                    return;
                }
                string fn = Target == UARTTargets.ESP ? Settings.ESPLogFile : Settings.PiLogFile;
                var mode = Settings.TruncateLogsOnStartup ? FileMode.Create : FileMode.Append;
                fileStream = File.Open(fn, mode, FileAccess.Write, FileShare.ReadWrite);
                fileWriter = new StreamWriter(fileStream);
                fileWriter.AutoFlush = true;
                Console.WriteLine(UARTLogger_Device.PluginName + "Logging " + Target.ToString() + " traffic to " + fn + ".");

            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTLogger_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        public void Write(String Value)
        {
            try
            {
                if (fileWriter != null && Value != null)
                    fileWriter.Write(Value);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTLogger_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            try
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
            catch (Exception ex)
            {
                Console.Error.Write(UARTLogger_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
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
            try
            {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Console.Error.Write(UARTLogger_Device.PluginName);
                Console.Error.WriteLine(ex.ToString());
            }
        }
        #endregion
    }
}
