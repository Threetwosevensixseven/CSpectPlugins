using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    /// <summary>
    /// The II2CDevice interface represents a minimal set of common features shared between the I2CMaster and I2CSlave classes, 
    /// along with any concrete slave implementations.
    /// It's primary purpose is to allow the I2CBus to talk to both master and slaves as if they were alike, while
    /// still being able to tell which is master and which are slaves. It also allows the master and slave to avoid sharing
    /// a common base class.
    /// </summary>
    public interface II2CDevice
    {
        byte SlaveAddress { get; }
        byte WriteAddress { get; }
        byte ReadAddress { get; }
        string DeviceName { get; }
        void Tick(bool NewSDA, bool NewSCL, bool OldSDA, bool OldSCL);
        bool IsMaster { get; }
        void Log(string Text);
    }

    /// <summary>
    /// The UpdateLogEventHandler delegate represents an abstract logging callback that the I2CBus, I2CMaster 
    /// and I2CSlave classes can invoke to pass back information to be logged.
    /// </summary>
    /// <param name="Text">The text to be logged.</param>
    public delegate void UpdateLogEventHandler(string Text);
}
