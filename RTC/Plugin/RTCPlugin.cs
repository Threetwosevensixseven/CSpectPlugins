using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;
using Plugins.RTC.Debug;
using RTC.I2C;

namespace Plugins.RTC.Plugin
{
    public class RTCPlugin : iPlugin
    {
        private const short PORT_SCL = 0x103b;
        private const short PORT_SDA = 0x113b;
        private iCSpect CSpect;
        private I2CBus Bus;
        private I2CMaster Master;
        private I2CSlave DS1307;
        private Settings Settings;
        private ILogger BusLogger;
        private ILogger MasterLogger;
        private ILogger SlaveLogger;

        public List<sIO> Init(iCSpect _CSpect)
        {
            CSpect = _CSpect;
            Settings = Settings.Load();
            BusLogger = new FileLogger(Settings, LogTargets.Bus);
            MasterLogger = new FileLogger(Settings, LogTargets.Master);
            SlaveLogger = new FileLogger(Settings, LogTargets.Slave);
            Bus = new I2CBus(BusLogger);
            Master = new I2CMaster(Bus, MasterLogger);
            DS1307 = new DS1307(Bus, SlaveLogger);
            Bus.Start();
            var ports = new List<sIO>();
            ports.Add(new sIO(PORT_SCL, eAccess.Port_Read));
            ports.Add(new sIO(PORT_SCL, eAccess.Port_Write));
            ports.Add(new sIO(PORT_SDA, eAccess.Port_Read));
            ports.Add(new sIO(PORT_SDA, eAccess.Port_Write));
            return ports;
        }

        public void Quit()
        {
        }

        public byte Read(eAccess _type, int _address, out bool _isvalid)
        {
            // Only handle the two Next I/O ports corresponding to the I2C SCL and Data lines
            if (_type == eAccess.Port_Read && _address == PORT_SCL)
            {
                _isvalid = true;
                return Convert.ToByte(Bus.SCL ? 1 : 0);
            }
            else if (_type == eAccess.Port_Read && _address == PORT_SDA)
            {
                _isvalid = true;
                return Convert.ToByte(Bus.SDA ? 1 : 0);
            }
            _isvalid = false;
            return 0;
        }

        public bool Write(eAccess _type, int _port, byte _value)
        {
            if (_type == eAccess.Port_Write && _port == PORT_SCL)
            {
                bool bit = (_value & 1) == 1;
                Bus.SetSCL(Master, bit);
                return true;
            }
            else if (_type == eAccess.Port_Write && _port == PORT_SDA)
            {
                bool bit = (_value & 1) == 1;
                Bus.SetSDA(Master, bit);
                return true;
            }
            return false;
        }

        private void LogMaster(string Text)
        {
            MasterLogger.AppendLine(Text);
        }

        private void LogSlave(string Text)
        {
            SlaveLogger.AppendLine(Text);
        }
    }
}
