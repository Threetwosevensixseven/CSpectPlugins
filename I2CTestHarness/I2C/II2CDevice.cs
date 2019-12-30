using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    public delegate void UpdateLogEventHandler(string Text);

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
}
