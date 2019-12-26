using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.UARTLogger
{
    public class UARTBuffer : IDisposable
    {
        Settings settings;
        private Queue<byte> buffer;
        private UARTStates state;
        private UARTTargets target;
        private object sync;
        private Timer timer;
        private FileLogger espLogger;
        private FileLogger piLogger;

        public UARTBuffer(Settings Settings)
        {
            settings = Settings;
            buffer = new Queue<byte>();
            state = UARTStates.Reading;
            target = UARTTargets.ESP;
            sync = new object();
            timer = new Timer(timerElapsed, this, settings.FlushLogsAfterSecs * 1000, Timeout.Infinite);
            espLogger = new FileLogger(Settings, UARTTargets.ESP);
            piLogger = new FileLogger(Settings, UARTTargets.Pi);
        }

        public void Log(byte Value, UARTStates NewState)
        {
            timer.Change(settings.FlushLogsAfterSecs * 1000, Timeout.Infinite);
            if (state != NewState)
            {
                Flush();
                state = NewState;
            }
            buffer.Enqueue(Value);
        }

        public void ChangeUARTType(UARTTargets NewTarget)
        {
            timer.Change(settings.FlushLogsAfterSecs * 1000, Timeout.Infinite);
            if (NewTarget != target)
            {
                Flush();
                target = NewTarget;
            }
        }

        private void Flush()
        {
            lock (sync)
            {
                if (!settings.EnableESPLogging && target == UARTTargets.ESP)
                {
                    buffer = new Queue<byte>();
                    return;
                }
                else if (!settings.EnablePiLogging && target == UARTTargets.Pi)
                {
                    buffer = new Queue<byte>();
                    return;
                }

                if (buffer.Count > 0)
                {
                    // Write header
                    string action = state == UARTStates.Reading ? "Read " : "Written ";
                    string prep = state == UARTStates.Reading ? "from " : "to ";
                    string plural = buffer.Count == 1 ? "" : "s";
                    var now = DateTime.Now;
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    sb.Append("[");
                    sb.Append(now.ToShortDateString());
                    sb.Append(" ");
                    sb.Append(now.ToLongTimeString());
                    sb.Append("] ");
                    sb.Append(action);
                    sb.Append(buffer.Count);
                    sb.Append(" byte");
                    sb.Append(plural);
                    sb.Append(" ");
                    sb.Append(prep);
                    sb.Append(target.ToString());
                    sb.AppendLine(":");

                    // Write data rows
                    int rowCount = 0;
                    string hex = "";
                    string asc = "";
                    while (buffer.Count > 0)
                    {
                        if (rowCount == 0)
                        {
                            hex = "    ";
                            asc = "";
                        }
                        byte b = buffer.Dequeue();
                        hex += b.ToString("x2") + " ";
                        asc += SpectrumCharset.ToASCII(b);
                        rowCount++;
                        if (rowCount >= 16 || buffer.Count == 0)
                        {
                            sb.Append(hex.PadRight(54));
                            sb.Append(asc);
                            sb.AppendLine();
                            rowCount = 0;
                        }
                    }

                    // Log result
                    var text = sb.ToString();
                    if (target == UARTTargets.ESP)
                        espLogger.Write(text);
                    else if (target == UARTTargets.Pi)
                        piLogger.Write(text);
                }
            }
        }

        private void timerElapsed(object Buffer)
        {
            if (Buffer is UARTBuffer)
                ((UARTBuffer)Buffer).Flush();
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    Flush();
                    if (espLogger != null)
                    {
                        espLogger.Dispose();
                        espLogger = null;
                    }
                    if (piLogger != null)
                    {
                        piLogger.Dispose();
                        piLogger = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~UARTBuffer() {
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
        #endregion IDisposable
    }
}
