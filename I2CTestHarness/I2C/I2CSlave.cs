using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    public class I2CSlave : II2CDevice
    {
        private I2CBus bus;
        private UpdateLogEventHandler logCallback;
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

        public I2CSlave(I2CBus Bus, UpdateLogEventHandler LogCallback = null)
        {
            bus = Bus;
            logCallback = LogCallback;
            lastState = currentState = CommandStates.Stopped;
            bus.Register(this);
            Log("State: " + currentState.ToString());
            justStarted = justAckNacked = false;
        }

        public virtual byte SlaveAddress
        {
            get
            {
                throw new NotImplementedException("SlaveAddress must be implemented on derived class");
            }
        }

        public virtual string DeviceName
        {
            get
            {
                throw new NotImplementedException("DeviceName must be implemented on derived class");
            }
        }

        public bool IsMaster { get { return false; } }

        public void Log(string Text)
        {
            if (logCallback != null)
                logCallback(Text);
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

        public void Tick(bool NewSDA, bool NewSCL, bool OldSDA, bool OldSCL)
        {
            lastSDA = OldSDA;
            lastSCL = OldSCL;
            LogBus(NewSDA, NewSCL);

            // Process CMD_START
            // A change in the state of the data line, from HIGH to LOW, while the clock is HIGH, defines a START condition.
            // Trigger on falling edge of data
            if (!justStopped && (currentState == CommandStates.Stopped || currentState == CommandStates.Started) && NewSCL && lastSDA && !NewSDA)
            {
                Log("Rx CMD_START");
                justStarted = true;
                justStopped = justAckNacked = false;
                bytesSinceStart = 0;
                lastState = currentState;
                currentState = CommandStates.Started;
                Log("State: " + currentState.ToString());
            }

            // Process CMD_STOP
            // A change in the state of the data line, from LOW to HIGH, while the clock line is HIGH, defines a STOP condition.
            // Trigger on rising edge of data
            else if ((currentState == CommandStates.Started || currentState == CommandStates.ReceivingByte) && NewSCL && lastSCL && !lastSDA && NewSDA)
            {
                Log("Rx CMD_STOP");
                justStopped = true;
                justStarted = justAckNacked = false;
                lastState = currentState;
                currentState = CommandStates.Stopped;
                Log("State: " + currentState.ToString());
            }

            // Receive data bit
            // Sample data, triggering on falling edge of clock
            else if (!justStarted && !justAckNacked && currentState == CommandStates.Started && lastSCL && !NewSCL)
            {
                Log("Rx CMD_TX");
                justStarted = justAckNacked = justStopped = false;
                lastBit = 0;
                currentByte = Convert.ToByte((NewSDA ? 1 : 0) << (7 - lastBit));
                Log("Rx data bit " + lastBit + "=" + (NewSDA ? "1" : "0"));
                lastState = currentState;
                currentState = CommandStates.ReceivingByte;
                Log("State: " + currentState.ToString());
            }
            else if (currentState == CommandStates.ReceivingByte && !lastSCL && NewSCL && lastBit >= 0 && lastBit <= 6)
            {
                lastBit++;
                justStarted = justAckNacked = justStopped = false;
                currentByte = Convert.ToByte(currentByte | ((NewSDA ? 1 : 0) << (7 - lastBit)));
                Log("Rx data bit " + lastBit + "=" + (NewSDA ? "1" : "0"));
            }
            else if (currentState == CommandStates.ReceivingByte && !lastSCL && NewSCL && lastBit >= 7)
            {
                lastBit++;
                justStarted = justStopped = false;
                justAckNacked = true;
                Log("Tx ACK  bit " + lastBit + "=" + (NewSDA ? "1" : "0"));
                Log("Rx byte=0x" + currentByte.ToString("X2"));
                if (bytesSinceStart == 0)
                {
                    bool isMine = HasAddress(currentByte, ref currentDirection);
                    if (isMine)
                    {
                        Log("Data address 0x" + (currentByte >> 1).ToString("X2") + " matches slave address");
                        Log("Accepting data " + currentDirection.ToString().ToUpper() + "S to " + DeviceName);
                        bytesSinceStart++;
                        lastState = currentState;
                        currentState = CommandStates.Started;
                        Log("State: " + currentState.ToString());
                    }
                    else
                    {
                        Log("Data address 0x" + (currentByte >> 1).ToString("X2") + " is for another slave");
                        Log("Ignoring further data until next CMD_START");
                        lastState = currentState;
                        currentState = CommandStates.Stopped;
                        Log("State: " + currentState.ToString());
                    }
                }
            }
            else
            {
                justStarted = justStopped = justAckNacked = false;
            }

            if (currentState == CommandStates.ReceivingByte && NewSCL && (NewSDA != lastSDA))
            {
                Log("Data cannot change when clock is high!");
            }
        }
    }
}
