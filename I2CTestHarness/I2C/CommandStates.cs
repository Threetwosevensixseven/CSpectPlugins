using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    /// <summary>
    /// Command states are used by the abstract I2CSlave class to represent the various states it transitions between,
    /// as it receives raw SCL and SDA signals from the I2CBus.
    /// Command states can also be reused by concrete slave implementations to manage their own higher-level states
    /// they transition between, as the I2CSlave class notifies via the On* notification methods.
    /// </summary>
    public enum CommandStates
    {
        Stopped,
        Started,
        TransferringByte
    }
}
