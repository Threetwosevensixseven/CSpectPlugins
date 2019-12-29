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

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            first = true;
            Bus = new I2CBus();
            Master = new I2CMaster(Bus, LogMaster);
            DS1307 = new DS1307(Bus, LogSlave);
            Bus.Start();
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
            btnGetTime.Enabled = false;
            if (!first)
            {
                txtMaster.AppendText("--------------------------------\r\n");
                txtSlave.AppendText( "--------------------------------\r\n");
            }
            first = false;
            Master.CMD_START();
            Master.CMD_TX(0xd0);
            Master.CMD_START();
            Master.CMD_STOP();
            btnGetTime.Enabled = true;
        }
    }
}
