using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Debug;

namespace RTC.I2C
{
    /// <summary>
    /// This abstract implementation of an I2C slave is intended to be subclassed for each different concrete slave 
    /// device implementation.
    /// Most of the public methods and properties in the abstract slave class interface with the I2CBus or I2CMaster classes.
    /// The abstract and protected members interface with the concrete slave devices.
    /// Methods whose name begins with On*, such as OnByteRead or OnByteWritten, are notifications for the concrete
    /// slave devices.
    /// </summary>
    public abstract class I2CSlave : II2CDevice
    {
        private I2CBus bus;
        private ILogger log;
        private bool lastSCL;
        private bool lastSDA;
        private int lastBit;
        private byte currentByte;
        private DataDirection currentDirection;
        private int bytesSinceStart;
        private CommandStates lastState;
        private CommandStates currentState;
        private bool justStarted;
        private bool justStopped;
        private bool justAckNacked;
        private List<bool> historySCL;
        private List<bool> historySDA;
        private int specialStarting;
        private int specialStopping;

        public I2CSlave(I2CBus Bus, ILogger Logger = null)
        {
            bus = Bus;
            log = Logger;
            lastState = currentState = CommandStates.Stopped;
            bus.Register(this);
            Log("State: " + currentState.ToString());
            justStarted = justAckNacked = false;
            currentDirection = DataDirection.Write;
            historySCL = new List<bool>();
            historySDA = new List<bool>();
            specialStarting = 0;
            specialStopping = 0;

        }

        public abstract byte SlaveAddress { get; }

        public abstract string DeviceName { get; }

        protected abstract void OnTransactionChanged(CommandStates NewState);

        protected abstract byte OnByteRead();

        protected abstract bool OnByteWritten(byte Byte);

        public byte WriteAddress
        {
            get
            {
                return Convert.ToByte((SlaveAddress << 1) & 255);
            }
        }

        public byte ReadAddress
        {
            get
            {
                return Convert.ToByte(((SlaveAddress << 1) & 255) | 1);
            }
        }

        public bool IsMaster { get { return false; } }

        public void Log(string Text)
        {
            if (log != null)
                log.AppendLine(Text);
        }

        protected void LogBus(bool SDA, bool SCL)
        {
            Log("    SDA=" + (SDA ? "1" : "0") + ", SCL=" + (SCL ? "1" : "0"));
        }

        public bool HasAddress(byte Address, ref DataDirection Direction)
        {
            // The Address passed in is shifted one bit to the left, 
            // with the new bit 0 set for reads, and unset for writes.
            // Shift it one bit to the right to match this device's address.
            byte match = Convert.ToByte(Address >> 1);
            Direction = (Address & 1) == 1 ? DataDirection.Read : DataDirection.Write;
            return match == SlaveAddress;
        }

        private void SendACK()
        {
            Log("Tx ACK  bit 8=0");
            bus.SetSDA(this, false); // Master should sample on next falling clock edge
        }

        private void SendNACK()
        {
            Log("Tx NACK bit 8=1");
            bus.SetSDA(this, true); // Master should sample on next falling clock edge
        }

        public void Tick(bool NewSDA, bool NewSCL, bool OldSDA, bool OldSCL)
        {
            historySCL.Insert(0, NewSCL);
            historySDA.Insert(0, NewSDA);
            lastSDA = OldSDA;
            lastSCL = OldSCL;
            LogBus(NewSDA, NewSCL);

            if (currentState == CommandStates.TransferringByte || currentState == CommandStates.Started)
            {
                if (specialStarting <= 0 && NewSCL)
                    specialStarting = 1;
                else if (specialStarting == 1 && NewSDA)
                    specialStarting = 2;
                else if (specialStarting == 2 && !NewSDA)
                    specialStarting = 3;
                else if (specialStarting == 3 && !NewSCL)
                    specialStarting = 4;
                else
                    specialStarting = 0;
            }

            if (currentState == CommandStates.TransferringByte || currentState == CommandStates.Started)
            {
                if (specialStopping <= 0 && !NewSDA)
                    specialStopping = 1;
                else if (specialStopping == 1 && NewSCL)
                    specialStopping = 2;
                else if (specialStopping == 2 && NewSDA)
                    specialStopping = 3;
                else
                    specialStopping = 0;
            }


            // Process CMD_START
            // A change in the state of the data line, from HIGH to LOW, while the clock is HIGH, defines a START condition.
            // Trigger on falling edge of data
            if (specialStopping == 3 && specialStarting < 2)
            {
                specialStopping = 0;
                specialStarting = 0;
                Log("Rx CMD_STOP");
                justStopped = true;
                justStarted = justAckNacked = false;
                lastState = currentState;
                currentDirection = DataDirection.Write;
                currentState = CommandStates.Stopped;
                Log("State: " + currentState.ToString());
                OnTransactionChanged(CommandStates.Stopped);
            }
            if (specialStarting == 4)
            {
                Log("Rx CMD_START");
                specialStopping = 0;
                specialStarting = 0;
                justStarted = true;
                justStopped = justAckNacked = false;
                bytesSinceStart = 0;
                lastState = currentState;
                currentState = CommandStates.Started;
                Log("State: " + currentState.ToString());
                OnTransactionChanged(CommandStates.Started);
            }
            else if (!justStopped && (currentState == CommandStates.Stopped || currentState == CommandStates.Started) && NewSCL && lastSDA && !NewSDA)
            {
                Log("Rx CMD_START");
                justStarted = true;
                justStopped = justAckNacked = false;
                bytesSinceStart = 0;
                lastState = currentState;
                currentState = CommandStates.Started;
                Log("State: " + currentState.ToString());
                OnTransactionChanged(CommandStates.Started);
            }
            else if ((currentState == CommandStates.TransferringByte || currentState == CommandStates.Started) && OldSCL && NewSCL && lastSDA && !NewSDA)
            {
                Log("Rx CMD_START");
                justStarted = true;
                justStopped = justAckNacked = false;
                bytesSinceStart = 0;
                lastState = currentState;
                currentState = CommandStates.Started;
                Log("State: " + currentState.ToString());
                OnTransactionChanged(CommandStates.Started);
            }

            // Process CMD_STOP
            // A change in the state of the data line, from LOW to HIGH, while the clock line is HIGH, defines a STOP condition.
            // Trigger on rising edge of data
            //else if (currentState == CommandStates.TransferringByte && NewSCL && lastSCL && !lastSDA && NewSDA)
            //{
            //    Log("Rx CMD_STOP");
            //    justStopped = true;
            //    justStarted = justAckNacked = false;
            //    lastState = currentState;
            //    currentDirection = DataDirection.Write;
            //    currentState = CommandStates.Stopped;
            //    Log("State: " + currentState.ToString());
            //    OnTransactionChanged(CommandStates.Stopped);
            //}

            // Receive data bit
            // Sample data, triggering on falling edge of clock
            else if (!justStarted && !justAckNacked && currentState == CommandStates.Started && lastSCL && !NewSCL)
            {
                if (currentDirection == DataDirection.Write)
                {
                    Log("Rx CMD_TX");
                    justStarted = justAckNacked = justStopped = false;
                    lastBit = 0;
                    currentByte = Convert.ToByte((NewSDA ? 1 : 0) << (7 - lastBit));
                    Log("Rx data bit " + lastBit + "=" + (NewSDA ? "1" : "0"));
                    lastState = currentState;
                    currentState = CommandStates.TransferringByte;
                    Log("State: " + currentState.ToString());
                }
                else
                {   // currentDirection == DataDirection.Read
                    Log("Rx CMD_RX");
                    justStarted = justAckNacked = justStopped = false;
                    lastState = currentState;
                    currentState = CommandStates.TransferringByte;
                    Log("State: " + currentState.ToString());
                    lastBit = 0;
                    currentByte = OnByteRead();
                    bool bit = ((currentByte >> (7 - lastBit)) & 1) == 1;
                    Log("Tx data bit " + lastBit + "=" + (bit ? "1" : "0"));
                    bus.SetSDA(this, bit);
                }
            }
            else if (currentState == CommandStates.TransferringByte && !lastSCL && NewSCL && lastBit >= 0 && lastBit <= 6)
            {
                if (currentDirection == DataDirection.Write)
                {
                    lastBit++;
                    justStarted = justAckNacked = justStopped = false;
                    currentByte = Convert.ToByte(currentByte | ((NewSDA ? 1 : 0) << (7 - lastBit)));
                    Log("Rx data bit " + lastBit + "=" + (NewSDA ? "1" : "0"));
                }
                else
                {   // currentDirection == DataDirection.Read
                    lastBit++;
                    justStarted = justAckNacked = justStopped = false;
                    bool bit = ((currentByte >> (7 - lastBit)) & 1) == 1;
                    Log("Tx data bit " + lastBit + "=" + (bit ? "1" : "0"));
                    bus.SetSDA(this, bit);
                }
            }
            else if (currentState == CommandStates.TransferringByte && !lastSCL && NewSCL && lastBit == 7)
            {
                if (currentDirection == DataDirection.Write)
                {
                    lastBit++;
                    justStarted = justStopped = false;
                    justAckNacked = true;
                    Log("Rx byte=0x" + currentByte.ToString("X2"));
                    if (bytesSinceStart == 0)
                    {
                        // First byte since (re)start is always a slave address plus direction
                        bool isMine = HasAddress(currentByte, ref currentDirection);
                        if (isMine)
                        {
                            //if (currentDirection == DataDirection.Read)
                            //    Debugger.Break();
                            Log("Data address 0x" + (currentByte >> 1).ToString("X2") + " matches slave address");
                            Log("Accepting data " + currentDirection.ToString().ToUpper() + "s to " + DeviceName);
                            SendACK(); // Send an ACK to participate in the rest of the transaction
                            bytesSinceStart++;
                            lastState = currentState;
                            currentState = CommandStates.Started;
                            Log("State: " + currentState.ToString());
                            OnTransactionChanged(CommandStates.Started);
                        }
                        else
                        {
                            Log("Data address 0x" + (currentByte >> 1).ToString("X2") + " is for another slave");
                            Log("Ignoring further data until next CMD_START");
                            // Don't sent an ACK or NACK because we were only eavesdropping
                            lastState = currentState;
                            currentState = CommandStates.Stopped;
                            currentDirection = DataDirection.Write;
                            Log("State: " + currentState.ToString());
                            OnTransactionChanged(CommandStates.Stopped);
                        }
                    }
                    else
                    {
                        // Second byte after start is usually a register address, but it depends on the concrete slave device.
                        // We can't assume this, so send all subsequent bytes to the slave and let it decide how to handle.
                        bool ack = OnByteWritten(currentByte);
                        // Relay this decision back to the I2C master
                        if (ack)
                            SendACK(); // Send an ACK to continue participating in the rest of the transaction
                        else
                        {
                            currentDirection = DataDirection.Write;
                            OnTransactionChanged(CommandStates.Stopped);
                            SendNACK(); // Send a NACK to abort the transaction
                        }
                    }
                }
                else
                {   // currentDirection == DataDirection.Read
                    Log("Waiting for ACK/NACK...");
                    bool ack = !bus.SDA; // Sample ACK/NACK from master
                    if (ack)
                    {
                        Log("Rx ACK  bit 8=0");
                        bytesSinceStart++;
                        lastState = currentState;
                        currentState = CommandStates.Started;
                        justAckNacked = justStarted = true;
                        justStopped = false;
                        Log("State: " + currentState.ToString());
                        OnTransactionChanged(CommandStates.Started);
                    }
                    else
                    {
                        Log("Rx NACK bit 8=1");
                        bytesSinceStart++;
                        lastState = currentState;
                        currentState = CommandStates.Stopped;
                        currentDirection = DataDirection.Write;
                        justAckNacked = justStopped = true;
                        justStarted = false;
                        Log("State: " + currentState.ToString());
                        OnTransactionChanged(CommandStates.Stopped);
                    }
                }
            }

            // Transmit data bit
            //if (currentDirection == DataDirection.Read)
            //{

            //}


            // Catch-all, tidy up transition states
            else
            {
                justStarted = justStopped = justAckNacked = false;
            }

            //if (currentState == CommandStates.ReceivingByte && NewSCL && (NewSDA != lastSDA))
            //{
            //    Log("Data cannot change when clock is high!");
                //throw new InvalidOperationException("Data cannot change when clock is high!");
            //}
        }
    }
}
