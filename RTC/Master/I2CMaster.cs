using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Slave;
using Plugins.RTC.Plugin;

namespace Plugins.RTC.Master
{
    public class I2CMaster
    {
        private Logger debug;
        private DS1307Device clock;
        private I2CStates lastState;
        private int lastStep;
        private int lastBit;
        private byte lastByte;
        private bool firstAction;
        private List<byte> bytes;

        public I2CMaster(Logger Logger)
        {
            debug = Logger;
            clock = new DS1307Device(Logger, true);
            firstAction = true;
            lastState = I2CStates.Stopped;
            lastStep = lastBit = -1;
            bytes = new List<byte>();
        }

        public byte Process(I2CActions Action, I2CLines Line, byte Value)
        {
            // For writes we process the input value and ignore the return value
            // For reads we ignore the input value and calculate a return value
            byte rv = Value;

            // Log the starting state (makes for a neater VS debug log to do it here instead of the constructor).
            if (firstAction)
            {
                debug.Log(Plugin.LogLevels.I2CState, "State = " + lastState.ToString());
                firstAction = false;
            }

            // Debug raw signals
            if (Action == I2CActions.Write)
                debug.Log(LogLevels.I2CRaw, Action.ToString().PadRight(6) + Line.ToString().PadRight(4) + " = 0x" + Value.ToString("x2"));
            else
                debug.Log(LogLevels.I2CRaw, Action.ToString().PadRight(6) + Line.ToString().PadRight(4));

            // Start Sequence
            if ((lastState == I2CStates.Started || lastState == I2CStates.Stopped) && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 1)
            {
                // Step 1
                debug.Log(LogLevels.I2CSequence, "Start sequence step 1");
                lastState = I2CStates.Starting;
                lastStep = 1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.Starting && Action == I2CActions.Write && Line == I2CLines.DATA && Value == 1 && lastStep == 1)
            {
                // Step 2
                debug.Log(LogLevels.I2CSequence, "Start sequence step 2");
                lastStep = 2;
            }
            else if (lastState == I2CStates.Starting && Action == I2CActions.Write && Line == I2CLines.DATA && Value == 0 && lastStep == 2)
            {
                // Step 3
                debug.Log(LogLevels.I2CSequence, "Start sequence step 3");
                lastStep = 3;
            }
            else if (lastState == I2CStates.Starting && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 0 && lastStep == 3)
            {
                // Step 3
                debug.Log(LogLevels.I2CSequence, "Start sequence step 4, completed");
                lastState = I2CStates.Started;
                lastStep = -1;
                bytes.Clear();
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }

            // Stop Sequence
            else if (lastState == I2CStates.Started && Action == I2CActions.Write && Line == I2CLines.DATA && Value == 0)
            {
                // Step 1
                debug.Log(LogLevels.I2CSequence, "Stop sequence step 1");
                lastState = I2CStates.Stopping;
                lastStep = 1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.Stopping && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 1 && lastStep == 1)
            {
                // Step 1
                debug.Log(LogLevels.I2CSequence, "Stop sequence step 2");
                lastState = I2CStates.Stopping;
                lastStep = 2;
            }
            else if (lastState == I2CStates.Stopping && Action == I2CActions.Write && Line == I2CLines.DATA && Value == 1 && lastStep == 2)
            {
                // Step 3
                debug.Log(LogLevels.I2CSequence, "Stop sequence step 3, completed");
                lastState = I2CStates.Stopped;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
                debug.Log(LogLevels.I2CState, "Received Bytes: " + bytes.ToLogString());
                clock.Process(bytes);
            }
            else if (lastState == I2CStates.Starting)
            {
                debug.Log(LogLevels.I2CSequence, "Invalid start sequence, aborted");
                lastState = I2CStates.Stopped;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.Stopping)
            {
                debug.Log(LogLevels.I2CSequence, "Invalid stop sequence, aborted");
                lastState = I2CStates.Stopped;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }

            // Receive Byte
            else if (lastState == I2CStates.Started && Action == I2CActions.Write && Line == I2CLines.DATA && lastStep < 0)
            {
                // Bit Step a (first bit)
                if (lastBit < 0 || lastBit >= 8)
                    lastBit = 0;
                byte bit = Convert.ToByte(Value & 1); // Mask off rightmost bit
                byte newByte = Convert.ToByte(bit << (7 - lastBit));
                lastByte = newByte;
                debug.Log(LogLevels.I2CSequence, "Receiving byte, bit " + lastBit + "a (" + bit + ", " + newByte.ToString("x2") + ")");
                lastState = I2CStates.ProcessingByte;
                lastStep = 0;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Write && Line == I2CLines.DATA && lastStep < 0)
            {
                // Bit Step a (subsequent bits)
                byte bit = Convert.ToByte(Value & 1); // Mask off rightmost bit
                if (lastBit == 8)
                {
                    debug.Log(LogLevels.I2CSequence, "Receiving byte, bit " + lastBit + "a (" + bit + ")");
                    if (bit != 1)
                    {
                        debug.Log(LogLevels.I2CSequence, "Invalid receive byte ACK, aborted");
                        lastState = I2CStates.Stopped;
                        lastStep = -1;
                        debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
                    }
                }
                else
                {
                    byte newByte = Convert.ToByte(bit << (7 - lastBit));
                    lastByte = Convert.ToByte(lastByte | newByte);
                    debug.Log(LogLevels.I2CSequence, "Receiving byte, bit " + lastBit + "a (" + bit + ", " + newByte.ToString("x2") + ")");
                }
                lastState = I2CStates.ProcessingByte;
                lastStep = 0;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 1 && lastStep == 0)
            {
                // Bit Step b
                debug.Log(LogLevels.I2CSequence, "Receiving byte, bit " + lastBit + "b");
                lastStep = 1;
            }
            else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 0 && lastStep == 1)
            {
                // Bit Step c
                debug.Log(LogLevels.I2CSequence, "Receiving byte, bit " + lastBit + "c");
                if (lastBit == 8)
                {
                    // We received a complete byte
                    bytes.Add(lastByte);
                    debug.Log(LogLevels.I2CState, "Received Byte = 0x" + lastByte.ToString("x2"));
                    // Ready to receive new byte or stop
                    lastState = I2CStates.Started;
                    debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
                }
                lastStep = -1;
                lastBit++;
            }

            // Receive byte    
            else if ((lastState == I2CStates.Stopped || lastState == I2CStates.Started) && Action == I2CActions.Read && Line == I2CLines.DATA)
            {
                // Bit Step a (first bit)
                if (lastBit < 0 || lastBit >= 8)
                    lastBit = 0;
                lastByte = clock.Read();
                rv = Convert.ToByte((lastByte >> (7 - lastBit)) & 1); // Mask off rightmost bit
                debug.Log(LogLevels.I2CSequence, "Sending byte, bit " + lastBit + "a (" + rv + ", " + lastByte.ToString("x2") + ")");
                lastState = I2CStates.ProcessingByte;
                lastStep = 0;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Read && Line == I2CLines.DATA && lastBit >= 0 && lastBit <= 5)
            {
                // Bit Step b (bits 1 to 6)
                lastBit++;
                lastByte = clock.Read();
                rv = Convert.ToByte((lastByte >> (7 - lastBit)) & 1); // Mask off rightmost bit
                debug.Log(LogLevels.I2CSequence, "Sending byte, bit " + lastBit + "b (" + rv + ", " + lastByte.ToString("x2") + ")");
            }
            else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Read && Line == I2CLines.DATA && lastBit > 5)
            {
                // Bit Step c (bit 7)
                lastBit++;
                lastByte = clock.Read();
                rv = Convert.ToByte((lastByte >> (7 - lastBit)) & 1); // Mask off rightmost bit
                debug.Log(LogLevels.I2CSequence, "Sending byte, bit " + lastBit + "c (" + rv + ", " + lastByte.ToString("x2") + ")");
                lastState = I2CStates.Started;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            //else if (lastState == I2CStates.ProcessingByte && Action == I2CActions.Read && Line == I2CLines.DATA && lastStep == 1)
            //{

            //}


            // Catch invalid sequences
            else if (lastState == I2CStates.Started)
            {
                debug.Log(LogLevels.I2CSequence, "Invalid receive byte sequence, aborted");
                lastState = I2CStates.Stopped;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }

            return rv;
        }
    }
}
