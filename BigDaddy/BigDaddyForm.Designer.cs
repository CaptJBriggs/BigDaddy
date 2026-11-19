namespace BigDaddy
{
    partial class BigDaddyForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            btnValidate = new Button();
            lblCountdown = new Label();
            txtShortBreakDuration = new BigDaddyTimeBox();
            txtLongBreakInterval = new BigDaddyTimeBox();
            txtLongBreakDuration = new BigDaddyTimeBox();
            txtShortBreakInterval = new BigDaddyTimeBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(116, 17);
            label1.Name = "label1";
            label1.Size = new Size(109, 15);
            label1.TabIndex = 0;
            label1.Text = "Short Break Interval";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(109, 77);
            label2.Name = "label2";
            label2.Size = new Size(116, 15);
            label2.TabIndex = 1;
            label2.Text = "Short Break Duration";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(117, 128);
            label3.Name = "label3";
            label3.Size = new Size(108, 15);
            label3.TabIndex = 2;
            label3.Text = "Long Break Interval";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(110, 173);
            label4.Name = "label4";
            label4.Size = new Size(115, 15);
            label4.TabIndex = 3;
            label4.Text = "Long Break Duration";
            // 
            // btnValidate
            // 
            btnValidate.Location = new Point(286, 275);
            btnValidate.Name = "btnValidate";
            btnValidate.Size = new Size(75, 23);
            btnValidate.TabIndex = 8;
            btnValidate.Text = "Validate Settings";
            btnValidate.UseVisualStyleBackColor = true;
            btnValidate.Click += btnValidate_Click;
            // 
            // lblCountdown
            // 
            lblCountdown.AutoSize = true;
            lblCountdown.Location = new Point(300, 236);
            lblCountdown.Name = "lblCountdown";
            lblCountdown.Size = new Size(49, 15);
            lblCountdown.TabIndex = 9;
            lblCountdown.Text = "00:00:00";
            lblCountdown.Click += lblCountdown_Click;
            // 
            // txtShortBreakDuration
            // 
            txtShortBreakDuration.Location = new Point(421, 74);
            txtShortBreakDuration.Name = "txtShortBreakDuration";
            txtShortBreakDuration.Size = new Size(100, 23);
            txtShortBreakDuration.TabIndex = 10;
            // 
            // txtLongBreakInterval
            // 
            txtLongBreakInterval.Location = new Point(421, 125);
            txtLongBreakInterval.Name = "txtLongBreakInterval";
            txtLongBreakInterval.Size = new Size(100, 23);
            txtLongBreakInterval.TabIndex = 11;
            // 
            // txtLongBreakDuration
            // 
            txtLongBreakDuration.Location = new Point(421, 173);
            txtLongBreakDuration.Name = "txtLongBreakDuration";
            txtLongBreakDuration.Size = new Size(100, 23);
            txtLongBreakDuration.TabIndex = 12;
            // 
            // txtShortBreakInterval
            // 
            txtShortBreakInterval.Location = new Point(421, 17);
            txtShortBreakInterval.Name = "txtShortBreakInterval";
            txtShortBreakInterval.Size = new Size(100, 23);
            txtShortBreakInterval.TabIndex = 13;
            // 
            // BigDaddyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(txtShortBreakInterval);
            Controls.Add(txtLongBreakDuration);
            Controls.Add(txtLongBreakInterval);
            Controls.Add(txtShortBreakDuration);
            Controls.Add(lblCountdown);
            Controls.Add(btnValidate);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "BigDaddyForm";
            Text = "Big Daddy";
            Load += BigDaddyForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private BigDaddyTimeBox txtShortBreakInterval;
        private BigDaddyTimeBox txtShortBreakDuration;
        private BigDaddyTimeBox txtLongBreakInterval;
        private BigDaddyTimeBox txtLongBreakDuration;
        private Button btnValidate;
        private Label lblCountdown;
    }
}
