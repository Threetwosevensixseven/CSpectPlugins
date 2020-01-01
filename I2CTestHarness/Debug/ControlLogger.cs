using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Plugins.RTC.Debug;

namespace I2CTestHarness.Debug
{
    public class ControlLogger : ILogger
    {
        private Control control;

        public ControlLogger(Control Control)
        {
            control = Control;
        }

        public void Append(string Text)
        {
            if (control == null)
                return;
            else if (control is TextBoxBase)
                ((TextBoxBase)control).AppendText(Text ?? "");
            else
                control.Text += (Text ?? "");
        }

        public void AppendLine(string Text)
        {
            if (control == null)
                return;
            else if (control is TextBoxBase)
                ((TextBoxBase)control).AppendText((Text ?? "") + "\r\n");
            else
                control.Text += (Text ?? "") + "\r\n";
        }
    }
}
