namespace lib_track_kiosk.sub_forms
{
    partial class ScanBookQR
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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            panel2 = new Panel();
            guna2HtmlLabel7 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            groupBox3 = new GroupBox();
            camera_pbx = new PictureBox();
            panel9 = new Panel();
            status_label = new Label();
            guna2Button3 = new Guna.UI2.WinForms.Guna2Button();
            panel1 = new Panel();
            panel6 = new Panel();
            panel5 = new Panel();
            panel3 = new Panel();
            panel4 = new Panel();
            panel2.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)camera_pbx).BeginInit();
            panel9.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ControlDark;
            panel2.Controls.Add(guna2HtmlLabel7);
            panel2.Location = new Point(40, 48);
            panel2.Name = "panel2";
            panel2.Size = new Size(609, 54);
            panel2.TabIndex = 26;
            // 
            // guna2HtmlLabel7
            // 
            guna2HtmlLabel7.BackColor = Color.Transparent;
            guna2HtmlLabel7.Font = new Font("Segoe UI", 21.75F, FontStyle.Italic, GraphicsUnit.Point, 0);
            guna2HtmlLabel7.ForeColor = Color.Black;
            guna2HtmlLabel7.Location = new Point(6, 5);
            guna2HtmlLabel7.Name = "guna2HtmlLabel7";
            guna2HtmlLabel7.Size = new Size(594, 42);
            guna2HtmlLabel7.TabIndex = 20;
            guna2HtmlLabel7.Text = "Please scan the Book QR in front of the camera";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(camera_pbx);
            groupBox3.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            groupBox3.Location = new Point(127, 22);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(353, 323);
            groupBox3.TabIndex = 21;
            groupBox3.TabStop = false;
            groupBox3.Text = "QR Code";
            // 
            // camera_pbx
            // 
            camera_pbx.Location = new Point(6, 34);
            camera_pbx.Name = "camera_pbx";
            camera_pbx.Size = new Size(341, 283);
            camera_pbx.SizeMode = PictureBoxSizeMode.StretchImage;
            camera_pbx.TabIndex = 5;
            camera_pbx.TabStop = false;
            // 
            // panel9
            // 
            panel9.BackColor = SystemColors.ActiveCaption;
            panel9.Controls.Add(status_label);
            panel9.Controls.Add(guna2Button3);
            panel9.Location = new Point(40, 501);
            panel9.Name = "panel9";
            panel9.Size = new Size(609, 71);
            panel9.TabIndex = 28;
            // 
            // status_label
            // 
            status_label.AutoSize = true;
            status_label.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            status_label.Location = new Point(15, 25);
            status_label.Name = "status_label";
            status_label.Size = new Size(16, 21);
            status_label.TabIndex = 46;
            status_label.Text = "-";
            // 
            // guna2Button3
            // 
            guna2Button3.BorderRadius = 5;
            guna2Button3.BorderThickness = 1;
            guna2Button3.CustomizableEdges = customizableEdges1;
            guna2Button3.DisabledState.BorderColor = Color.DarkGray;
            guna2Button3.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button3.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button3.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button3.FillColor = Color.FromArgb(192, 0, 0);
            guna2Button3.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button3.ForeColor = Color.White;
            guna2Button3.Location = new Point(437, 10);
            guna2Button3.Name = "guna2Button3";
            guna2Button3.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Button3.Size = new Size(160, 51);
            guna2Button3.TabIndex = 20;
            guna2Button3.Text = "CANCEL";
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ControlDark;
            panel1.Controls.Add(groupBox3);
            panel1.Location = new Point(40, 117);
            panel1.Name = "panel1";
            panel1.Size = new Size(609, 369);
            panel1.TabIndex = 27;
            // 
            // panel6
            // 
            panel6.BackColor = Color.Maroon;
            panel6.Location = new Point(0, 0);
            panel6.Name = "panel6";
            panel6.Size = new Size(744, 25);
            panel6.TabIndex = 29;
            // 
            // panel5
            // 
            panel5.BackColor = Color.Maroon;
            panel5.Location = new Point(0, 24);
            panel5.Name = "panel5";
            panel5.Size = new Size(20, 602);
            panel5.TabIndex = 30;
            // 
            // panel3
            // 
            panel3.BackColor = Color.Maroon;
            panel3.Location = new Point(18, 597);
            panel3.Name = "panel3";
            panel3.Size = new Size(744, 25);
            panel3.TabIndex = 30;
            // 
            // panel4
            // 
            panel4.BackColor = Color.Maroon;
            panel4.Location = new Point(671, 25);
            panel4.Name = "panel4";
            panel4.Size = new Size(20, 602);
            panel4.TabIndex = 31;
            // 
            // ScanBookQR
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(690, 622);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel5);
            Controls.Add(panel6);
            Controls.Add(panel2);
            Controls.Add(panel9);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ScanBookQR";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ScanBookQR";
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)camera_pbx).EndInit();
            panel9.ResumeLayout(false);
            panel9.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel2;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel7;
        private GroupBox groupBox3;
        private PictureBox camera_pbx;
        private Panel panel9;
        private Label status_label;
        private Guna.UI2.WinForms.Guna2Button guna2Button3;
        private Panel panel1;
        private Panel panel6;
        private Panel panel5;
        private Panel panel3;
        private Panel panel4;
    }
}