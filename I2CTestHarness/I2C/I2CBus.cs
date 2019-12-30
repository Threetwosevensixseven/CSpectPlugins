using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CTestHarness.I2C
{
    /// <summary>
    /// The I2CBus is an active I2C bus manager class that glues together an I2CMaster and zero, one or more concrete 
    /// I2CSlave device implementations. 
    /// At heart, it has two lines, an SCL clock line and an SDA data line, which is pulled low or released high by
    /// both master and slaves, according to some bus arbitration rules.
    /// It has more logic and responsibilities than a hardware two-wire bus would have, but it suits a passive I2C 
    /// implementation such as this, where the clock is intended to be bitbanged by emulator I/O instead of free-running
    /// and driven from an external oscillator.
    /// </summary>
    public class I2CBus
    {
        private bool sda;
        private bool scl;
        private List<II2CDevice> devices;
        private bool started;

        public I2CBus()
        {
            devices = new List<II2CDevice>();
            started = false;
        }

        public void Register(II2CDevice Device)
        {
            if (started)
                throw new InvalidOperationException("Cannot add devices when I2C bus is started");
            if (Device.IsMaster)
                devices.Add(Device);
            else
                devices.Insert(0, Device); // Better to put slaves at the beginning of the callback list, as they get more traffic
            #if DEBUG
            Device.Log(Device.DeviceName);
            if (Device.IsMaster)
                Device.Log("Device is the bus master");
            else
                Device.Log("Device is a slave at address 0x" + Device.SlaveAddress.ToString("X2"));
            #endif
        }

        public void Start()
        {
            if (devices.Count(d => d.IsMaster) != 1)
                throw new InvalidOperationException("I2C bus must always have a single master");
            sda = scl = true;
            #if DEBUG
            if (!started)
                foreach (var device in devices)
                    device.Log("Bus started");
            #endif
            started = true;
        }

        public void Stop()
        {
            #if DEBUG
            if (started)
                foreach (var device in devices)
                    device.Log("Bus stopped");
            #endif
            started = false;
        }

        public bool SDA
        {
            get
            {
                if (!started)
                    throw new InvalidOperationException("Cannot read I2C bus before starting");
                return sda;
            }
        }

        public bool SCL
        {
            get
            {
                if (!started)
                    throw new InvalidOperationException("Cannot read I2C bus before starting");
                return scl;
            }
        }

        public void SetSDA(II2CDevice Sender, bool NewValue)
        {
            if (!started)
                throw new InvalidOperationException("Cannot write to I2C bus before starting");
            foreach (var device in devices)
            {
                if (device != Sender)
                    device.Tick(NewValue, scl, sda, scl);
            }
            sda = NewValue;
        }

        public void SetSCL(II2CDevice Sender, bool NewValue)
        {
            if (!started)
                throw new InvalidOperationException("Cannot write to I2C bus before starting");
            if (!Sender.IsMaster)
                throw new InvalidOperationException("Only I2C master can control SCL");
            if (scl == NewValue)
                return;
            foreach (var device in devices)
            {
                if (device != Sender)
                    device.Tick(sda, NewValue, sda, scl);
            }
            scl = NewValue;
        }

        public override string ToString()
        {
            return "    SDA=" + (sda ? "1" : "0") + ", SCL=" + (scl ? "1" : "0");
        }
    }
}
