namespace lib_track_kiosk.test_forms
{
    partial class ScanQRTest
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
            camera_cmbx = new ComboBox();
            label1 = new Label();
            enable_btn = new Button();
            camera_pbx = new PictureBox();
            output_rtbx = new RichTextBox();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)camera_pbx).BeginInit();
            SuspendLayout();
            // 
            // camera_cmbx
            // 
            camera_cmbx.FormattingEnabled = true;
            camera_cmbx.Location = new Point(69, 12);
            camera_cmbx.Name = "camera_cmbx";
            camera_cmbx.Size = new Size(286, 23);
            camera_cmbx.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(51, 15);
            label1.TabIndex = 1;
            label1.Text = "Camera:";
            // 
            // enable_btn
            // 
            enable_btn.Location = new Point(361, 12);
            enable_btn.Name = "enable_btn";
            enable_btn.Size = new Size(75, 23);
            enable_btn.TabIndex = 2;
            enable_btn.Text = "Enable";
            enable_btn.UseVisualStyleBackColor = true;
            enable_btn.Click += enable_btn_Click;
            // 
            // camera_pbx
            // 
            camera_pbx.Location = new Point(12, 41);
            camera_pbx.Name = "camera_pbx";
            camera_pbx.Size = new Size(424, 457);
            camera_pbx.SizeMode = PictureBoxSizeMode.Zoom;
            camera_pbx.TabIndex = 3;
            camera_pbx.TabStop = false;
            // 
            // output_rtbx
            // 
            output_rtbx.Location = new Point(442, 41);
            output_rtbx.Name = "output_rtbx";
            output_rtbx.Size = new Size(461, 457);
            output_rtbx.TabIndex = 4;
            output_rtbx.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(442, 16);
            label2.Name = "label2";
            label2.Size = new Size(34, 15);
            label2.TabIndex = 5;
            label2.Text = "Data:";
            // 
            // ScanQRTest
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(919, 512);
            Controls.Add(label2);
            Controls.Add(output_rtbx);
            Controls.Add(camera_pbx);
            Controls.Add(enable_btn);
            Controls.Add(label1);
            Controls.Add(camera_cmbx);
            Name = "ScanQRTest";
            Text = "ScanQRTest";
            FormClosing += ScanQRTest_FormClosing;
            Load += ScanQRTest_Load;
            ((System.ComponentModel.ISupportInitialize)camera_pbx).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox camera_cmbx;
        private Label label1;
        private Button enable_btn;
        private PictureBox camera_pbx;
        private RichTextBox output_rtbx;
        private Label label2;
    }
}