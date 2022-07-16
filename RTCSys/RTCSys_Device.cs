using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;

namespace RTCSys
{
    public class RTCSys_Device : iPlugin
    {
        private const byte REG_00_MACHINE_ID = 0x00;
        private const byte REG_0E_CORE_VER = 0x0E;
        private const byte REG_7F_USER_0 = 0x7F;
        private const byte INIT_MAGIC_1 = 0x12;
        private const byte INIT_MAGIC_2 = 0x34;
        private iCSpect CSpect;
        private RTCStates State = RTCStates.Uninitialised;
        private bool Internal = false;
        private DateTime Now;

        public List<sIO> Init(iCSpect _CSpect)
        {
            CSpect = _CSpect;
            State = RTCStates.Uninitialised;
            Internal = false;
            Now = DateTime.Now;
            var ports = new List<sIO>();
            ports.Add(new sIO(REG_00_MACHINE_ID, eAccess.NextReg_Write));
            ports.Add(new sIO(REG_00_MACHINE_ID, eAccess.NextReg_Read));
            ports.Add(new sIO(REG_0E_CORE_VER, eAccess.NextReg_Write));
            ports.Add(new sIO(REG_0E_CORE_VER, eAccess.NextReg_Read));
            ports.Add(new sIO(REG_7F_USER_0, eAccess.NextReg_Write));
            ports.Add(new sIO(REG_7F_USER_0, eAccess.NextReg_Read));
            return ports;
        }

        public byte Read(eAccess _type, int _address, int _id, out bool _isvalid)
        {
            if (_type == eAccess.NextReg_Read && _address == REG_00_MACHINE_ID)
            {
                State = RTCStates.Uninitialised;
                Debug.WriteLine("State = " + State.ToString());
            }
            else if (_type == eAccess.NextReg_Read && _address == REG_0E_CORE_VER)
            {
                State = RTCStates.Uninitialised;
                Debug.WriteLine("State = " + State.ToString());

            }
            else if (_type == eAccess.NextReg_Read && _address == REG_7F_USER_0)
            {
                if (State == RTCStates.ReadingDateLSB)
                {
                    // Bits 0–4: Day of the month (1–31)
                    // Bits 5–8: Month(1 = January, 2 = February, etc.) [5-7 in LSB]
                    byte val = Convert.ToByte((Now.Day & 0x1F) | ((Now.Month & 0x07) << 5));
                    Debug.WriteLine("Returning Date LSB = 0x" + val.ToString("X2"));
                    State = RTCStates.ReadingDateMSB;
                    Debug.WriteLine("State = " + State.ToString());
                    _isvalid = true;
                    return val;
                }
                else if (State == RTCStates.ReadingDateMSB)
                {
                    // Bits  5–8: Month(1 = January, 2 = February, etc.) [8 in MSB]
                    // Bits 9-15: Year offset from 1980 (add 1980 to get actual year)
                    byte val = Convert.ToByte(((Now.Month & 0x08) >> 3) | (((Now.Year - 1980) > 0 ? (Now.Year - 1980) : 0) << 1));
                    Debug.WriteLine("Returning Date MSB = 0x" + val.ToString("X2"));
                    State = RTCStates.ReadingTimeLSB;
                    Debug.WriteLine("State = " + State.ToString());
                    _isvalid = true;
                    return val;
                }
                else if (State == RTCStates.ReadingTimeLSB)
                {
                    // Bits  0–4: Second divided by 2
                    // Bits 5–10: Minute(0–59)  [5-7 in LSB]
                    byte val = Convert.ToByte(((Now.Second / 2) & 0x1F) | ((Now.Minute & 0x07) << 5));
                    Debug.WriteLine("Returning Time LSB = 0x" + val.ToString("X2"));
                    State = RTCStates.ReadingTimeMSB;
                    Debug.WriteLine("State = " + State.ToString());
                    _isvalid = true;
                    return val;
                }
                else if (State == RTCStates.ReadingTimeMSB)
                {
                    // Bits  5–10: Minute(0–59)  [8-10 in MSB]
                    // Bits 11–15: Hour (0–23 on a 24-hour clock)
                    byte val = Convert.ToByte(((Now.Minute & 0x38) >> 3) | (Now.Hour << 3));
                    Debug.WriteLine("Returning Time MSB = 0x" + val.ToString("X2"));
                    State = RTCStates.ReadingSeconds;
                    Debug.WriteLine("State = " + State.ToString());
                    _isvalid = true;
                    return val;
                }
                else if (State == RTCStates.ReadingSeconds)
                {
                    // Bits 0-7: Seconds (0–59)
                    byte val = Convert.ToByte(Now.Second);
                    Debug.WriteLine("Returning Seconds = 0x" + val.ToString("X2"));
                    State = RTCStates.Uninitialised;
                    Debug.WriteLine("State = " + State.ToString());
                    _isvalid = true;
                    return val;
                }
                else
                {
                    State = RTCStates.Uninitialised;
                }
                Debug.WriteLine("State = " + State.ToString());
            }
            _isvalid = false;
            return 0xff;
        }

        public bool Write(eAccess _type, int _port, int _id, byte _value)
        {
            if (_type == eAccess.NextReg_Write && _port == REG_00_MACHINE_ID)
            {
                if (State == RTCStates.Uninitialised && _value == INIT_MAGIC_1)
                    State = RTCStates.Initialised;
                else
                    State = RTCStates.Uninitialised;
                Debug.WriteLine("State = " + State.ToString());
                return false;
            }
            else if (_type == eAccess.NextReg_Write && _port == REG_0E_CORE_VER)
            {
                if (State == RTCStates.Initialised && _value == INIT_MAGIC_2)
                {
                    State = RTCStates.ReadingDateLSB;
                    Now = DateTime.Now;
                }
                else
                    State = RTCStates.Uninitialised;
                Debug.WriteLine("State = " + State.ToString());
                return false;
            }
            else if (_type == eAccess.NextReg_Write && _port == REG_7F_USER_0)
            {
                if (Internal)
                {
                    return false;
                }
                State = RTCStates.Uninitialised;
                Debug.WriteLine("State = " + State.ToString());
                Internal = true;
                CSpect.SetNextRegister(REG_7F_USER_0, _value);
                Internal = false;
            }
            return false;
        }

        public void Tick()
        {
        }

        public void Quit()
        {
        }

        public bool KeyPressed(int _id)
        {
            return false;
        }

        public void Reset()
        {
        }
    }
}
