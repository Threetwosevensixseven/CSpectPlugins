using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using I2CTestHarness.Classes;

namespace I2CTestHarness
{
    public partial class frmMain : Form
    {
        I2CBus Bus;
        I2CMaster Master;
        I2CSlave DS1307;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Bus = new I2CBus();
            Master = new I2CMaster(Bus, LogMaster);
            DS1307 = new I2CSlave(Bus, LogSlave);
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
            //Bus.Start();
            Master.CMD_START();
            //Master.CMD_START();
            Master.CMD_STOP();
            //Master.CMD_STOP();
            btnGetTime.Enabled = true;
        }
    }
}
