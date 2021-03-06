﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using I2CTestHarness.Debug;
using Plugins.RTC.Debug;
using RTC.I2C;

namespace I2CTestHarness
{
    /// <summary>
    /// This is a simple logging test harness intended for interactive testing of the I2CBus, I2CMaster and I2CSlave classes,
    /// along with any concrete slave implementations.
    /// The twin textboxes are hooked up to a master and single slave in the Debug configuration, and receive messages from
    /// these, along with the bus.
    /// In Release configuration I2CBus, I2CMaster and I2CSlave have logging disabled to improve emulator performance.
    /// </summary>
    public partial class TestHarnessForm : Form
    {
        private I2CBus Bus;
        private I2CMaster Master;
        private I2CSlave DS1307;
        private ILogger MasterLogger;
        private ILogger BusLogger;
        private ILogger SlaveLogger;
        private bool first;
        private bool running;

        public TestHarnessForm()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            first = true;
            running = false;
            BusLogger = new ControlLogger(txtBus);
            MasterLogger = new ControlLogger(txtMaster);
            SlaveLogger = new ControlLogger(txtSlave);
            Bus = new I2CBus(BusLogger);
            Master = new I2CMaster(Bus, MasterLogger);
            DS1307 = new DS1307(Bus, SlaveLogger);
            Bus.Start();
        }

        private void btnGetTime_Click(object sender, EventArgs e)
        {
            if (running) return;
            running = true;
            lblStatus.Text = "Running...";
            Application.DoEvents();
            if (!first)
            {
                txtMaster.AppendText("--------------------------------\r\n");
                txtSlave.AppendText( "--------------------------------\r\n");
                txtBus.AppendText("--------------------------------\r\n");
            }
            first = false;
            bool success = true;
            Master.CMD_START();                                      // Start
            success = success && Master.CMD_TX(DS1307.WriteAddress); // Write DS1307
            success = success && Master.CMD_TX(0x3e);                // Set reg = 62 (Z)
            success = success && Master.CMD_START();                 // Restart
            success = success && Master.CMD_TX(DS1307.ReadAddress);  // Read DS1307
            //txtMaster.Text = txtSlave.Text = "";
            var bytes = ReadBytes(9);
            string sig = Encoding.ASCII.GetString(bytes, 0, 2);
            var dt = RTC.I2C.DS1307.ConvertDateTime(bytes, 2);
            //AppendByte(ref test, Master.CMD_RX());                   // Read reg = 62 (Z)
            //AppendByte(ref test, Master.CMD_RX(true));               // Read reg = 63 (X)
            Master.CMD_STOP();
            //Master.CMD_START();                                      // Start
            if (success)
                lblStatus.Text = "Success: " + sig + " " + dt.ToShortDateString() + " " + dt.ToLongTimeString();
            else
                lblStatus.Text = "Failed, received NACK";
            running = false;
        }

        private void AppendByte(ref string Text, byte Byte)
        {
            Text += (char)Byte;
        }

        private byte[] ReadBytes(int Count)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < Count; i++)
                bytes.Add(Master.CMD_RX(i == Count - 1));
            return bytes.ToArray();
        }

        private void btnRtcSys_Click(object sender, EventArgs e)
        {
            if (running) return;
            running = true;
            lblStatus.Text = "Running...";
            Application.DoEvents();
            if (!first)
            {
                txtMaster.AppendText("--------------------------------\r\n");
                txtSlave.AppendText("--------------------------------\r\n");
                txtBus.AppendText("--------------------------------\r\n");
            }
            first = false;
            bool success = true;
            Master.CMD_START();                                      // Start
            success = success && Master.CMD_TX(DS1307.WriteAddress); // Write DS1307
            success = success && Master.CMD_TX(0x3e);                // Set reg = 62 (Z)
            Master.CMD_START();                                      // Start
            success = success && Master.CMD_TX(DS1307.ReadAddress);  // Read DS1307
            var bytes = ReadBytes(1);
            if (success)
                lblStatus.Text = "Success: RTC.SYS";
            else
                lblStatus.Text = "Failed, received NACK";
            running = false;
        }
    }
}
