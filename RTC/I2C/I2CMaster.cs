using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins.RTC.Plugin;

namespace Plugins.RTC.I2C
{
    public class I2CMaster
    {
        private Plugin.Logger debug;
        private I2CStates lastState;
        private int lastStep;
        bool firstAction;


        public I2CMaster(Plugin.Logger Logger)
        {
            debug = Logger;
            firstAction = true;
            lastState = I2CStates.Unknown;
            lastStep = -1;
        }

        public void Process(I2CActions Action, I2CLines Line, byte Value)
        {
            // Log the starting state (makes for a neater VS debug log to do it here instead of the constructor).
            if (firstAction)
            {
                debug.Log(Plugin.LogLevels.I2CState, "State = " + lastState.ToString());
                firstAction = false;
            }

            // Debug raw signals
            debug.Log(LogLevels.I2CRaw, Action.ToString().PadRight(6) + Line.ToString().PadRight(4) + " = 0x" + Value.ToString("x2"));

            // Start Sequence
            if (lastState == I2CStates.Unknown && Action == I2CActions.Write && Line == I2CLines.SCL && Value == 1)
            {
                // Step 1
                debug.Log(LogLevels.I2CSequence, "Start sequence step 1, beginning");
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
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }
            else if (lastState == I2CStates.Starting)
            {
                debug.Log(LogLevels.I2CSequence, "Invalid start sequence, aborted");
                lastState = I2CStates.Unknown;
                lastStep = -1;
                debug.Log(LogLevels.I2CState, "State = " + lastState.ToString());
            }

        }
    }
}
