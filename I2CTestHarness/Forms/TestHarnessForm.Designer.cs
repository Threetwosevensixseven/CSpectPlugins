namespace I2CTestHarness
{
    partial class TestHarnessForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestHarnessForm));
            this.btnGetTime = new System.Windows.Forms.Button();
            this.pnlLogs = new System.Windows.Forms.TableLayoutPanel();
            this.txtMaster = new System.Windows.Forms.TextBox();
            this.txtBus = new System.Windows.Forms.TextBox();
            this.txtSlave = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnRtcSys = new System.Windows.Forms.Button();
            this.pnlLogs.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGetTime
            // 
            this.btnGetTime.Location = new System.Drawing.Point(12, 12);
            this.btnGetTime.Name = "btnGetTime";
            this.btnGetTime.Size = new System.Drawing.Size(75, 23);
            this.btnGetTime.TabIndex = 0;
            this.btnGetTime.Text = "Get Time";
            this.btnGetTime.UseVisualStyleBackColor = true;
            this.btnGetTime.Click += new System.EventHandler(this.btnGetTime_Click);
            // 
            // pnlLogs
            // 
            this.pnlLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlLogs.ColumnCount = 3;
            this.pnlLogs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33112F));
            this.pnlLogs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33445F));
            this.pnlLogs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33445F));
            this.pnlLogs.Controls.Add(this.txtMaster, 0, 0);
            this.pnlLogs.Controls.Add(this.txtBus, 1, 0);
            this.pnlLogs.Controls.Add(this.txtSlave, 2, 0);
            this.pnlLogs.Location = new System.Drawing.Point(12, 41);
            this.pnlLogs.Name = "pnlLogs";
            this.pnlLogs.RowCount = 1;
            this.pnlLogs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlLogs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 265F));
            this.pnlLogs.Size = new System.Drawing.Size(885, 265);
            this.pnlLogs.TabIndex = 1;
            // 
            // txtMaster
            // 
            this.txtMaster.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.txtMaster.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMaster.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMaster.ForeColor = System.Drawing.Color.Chartreuse;
            this.txtMaster.Location = new System.Drawing.Point(3, 3);
            this.txtMaster.Multiline = true;
            this.txtMaster.Name = "txtMaster";
            this.txtMaster.ReadOnly = true;
            this.txtMaster.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMaster.Size = new System.Drawing.Size(288, 259);
            this.txtMaster.TabIndex = 1;
            // 
            // txtBus
            // 
            this.txtBus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.txtBus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBus.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBus.ForeColor = System.Drawing.Color.Chartreuse;
            this.txtBus.Location = new System.Drawing.Point(297, 3);
            this.txtBus.Multiline = true;
            this.txtBus.Name = "txtBus";
            this.txtBus.ReadOnly = true;
            this.txtBus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBus.Size = new System.Drawing.Size(289, 259);
            this.txtBus.TabIndex = 0;
            // 
            // txtSlave
            // 
            this.txtSlave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.txtSlave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSlave.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSlave.ForeColor = System.Drawing.Color.Chartreuse;
            this.txtSlave.Location = new System.Drawing.Point(592, 3);
            this.txtSlave.Multiline = true;
            this.txtSlave.Name = "txtSlave";
            this.txtSlave.ReadOnly = true;
            this.txtSlave.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSlave.Size = new System.Drawing.Size(290, 259);
            this.txtSlave.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(174, 17);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 1;
            // 
            // btnRtcSys
            // 
            this.btnRtcSys.Location = new System.Drawing.Point(93, 12);
            this.btnRtcSys.Name = "btnRtcSys";
            this.btnRtcSys.Size = new System.Drawing.Size(75, 23);
            this.btnRtcSys.TabIndex = 2;
            this.btnRtcSys.Text = "RTC.SYS";
            this.btnRtcSys.UseVisualStyleBackColor = true;
            this.btnRtcSys.Click += new System.EventHandler(this.btnRtcSys_Click);
            // 
            // TestHarnessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(909, 318);
            this.Controls.Add(this.btnRtcSys);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pnlLogs);
            this.Controls.Add(this.btnGetTime);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(640, 192);
            this.Name = "TestHarnessForm";
            this.Text = "I2C Test Harness";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.pnlLogs.ResumeLayout(false);
            this.pnlLogs.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetTime;
        private System.Windows.Forms.TableLayoutPanel pnlLogs;
        private System.Windows.Forms.TextBox txtMaster;
        private System.Windows.Forms.TextBox txtSlave;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtBus;
        private System.Windows.Forms.Button btnRtcSys;
    }
}

