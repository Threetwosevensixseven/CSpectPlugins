﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin;

namespace i2C_Sample
{

    // **********************************************************************
    /// <summary>
    ///     A simple, empty i2C device
    /// </summary>
    // **********************************************************************
    public class i2C_NULL_Device : iPlugin
    {        
        public const int PORT_CLOCK = 0x103b;
        public const int PORT_DATA = 0x113b;

        public iCSpect CSpect;
        // **********************************************************************
        /// <summary>
        ///     Init the device
        /// </summary>
        /// <returns>
        ///     List of ports we're registering
        /// </returns>
        // **********************************************************************
        public List<sIO> Init(iCSpect _CSpect)
        {
            Debug.WriteLine("NULL i2C device Added");

            CSpect = _CSpect;            

            // create a list of the ports we're interested in
            List<sIO> ports = new List<sIO>();
            ports.Add(new sIO(PORT_CLOCK, eAccess.Port_Read));
            ports.Add(new sIO(PORT_DATA, eAccess.Port_Read));
            ports.Add(new sIO(PORT_CLOCK, eAccess.Port_Write));
            ports.Add(new sIO(PORT_DATA, eAccess.Port_Write));
            return ports;
        }

        // **********************************************************************
        /// <summary>
        ///     Quit the device - free up anything we need to
        /// </summary>
        // **********************************************************************
        public void Quit()
        {            
        }


        // **********************************************************************
        /// <summary>
        ///     Write a value to one of the registered ports
        /// </summary>
        /// <param name="_port">the port being written to</param>
        /// <param name="_value">the value to write</param>
        // **********************************************************************
        public bool Write(eAccess _type, int _port, int _id, byte _value)
        {
            switch (_port)
            {
                case PORT_CLOCK: break;
                case PORT_DATA: break;
            }
            return true;
        }


        // **********************************************************************
        /// <summary>
        ///     
        /// </summary>
        /// <param name="_port"></param>
        /// <param name="_isvalid"></param>
        /// <returns></returns>
        // **********************************************************************
        public byte Read(eAccess _type, int _port, int _id, out bool _isvalid)
        {
            switch (_port)
            {
                case PORT_CLOCK: _isvalid = true; return 0xff;
                case PORT_DATA: _isvalid = true; return 0xff;
            }
            _isvalid = false;
            return 0;
        }

        public void Tick()
        {
        }

        public bool KeyPressed(int _id)
        {
            return false;
        }

        public void Reset()
        {
        }

        public void OSTick()
        {
            throw new NotImplementedException();
        }
    }
}
