using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using I2CTestHarness.I2C;

namespace I2CTestHarness
{
    public partial class frmMain : Form
    {
        private I2CBus Bus;
        private I2CMaster Master;
        private I2CSlave DS1307;
        private bool first;
        private bool running;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            first = true;
            running = false;
            Bus = new I2CBus();
            Master = new I2CMaster(Bus, LogMaster);
            DS1307 = new DS1307(Bus, LogSlave);
            Bus.Start();
            #if !DEBUG
            txtMaster.AppendText("LOGGING DISABLED IN RELEASE BUILD\r\n");
            txtSlave.AppendText("LOGGING DISABLED IN RELEASE BUILD\r\n");
            #endif
        }

        private void LogMaster(string Text)
        {
            txtMaster.AppendText((Text ?? "") + "\r\n");
        }

        private void LogSlave(string Text)
        {
            txtSlave.AppendText((Text ?? "") + "\r\n");
        }

        private void btnGetTime_Click(object sender, EventArgs e)
        {
            if (running) return;
            running = true;
            lblStatus.Text = "Running...";
            Application.DoEvents();
            #if DEBUG
            if (!first)
            {
                txtMaster.AppendText("--------------------------------\r\n");
                txtSlave.AppendText( "--------------------------------\r\n");
            }
            #endif
            first = false;
            bool success = true;
            Master.CMD_START();                                      // Start
            success = success && Master.CMD_TX(DS1307.WriteAddress); // Write DS1307
            success = success && Master.CMD_TX(0x3e);                // Set reg = 62 (Z)
            success = success && Master.CMD_START();                 // Restart
            success = success && Master.CMD_TX(DS1307.ReadAddress);  // Read DS1307
            Master.CMD_STOP();
            if (success)
                lblStatus.Text = "Success";
            else
                lblStatus.Text = "Failed, received NACK";
            running = false;
        }
    }
}
