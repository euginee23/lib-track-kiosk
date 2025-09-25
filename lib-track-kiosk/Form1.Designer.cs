namespace lib_track_kiosk
{
    partial class Form1
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
            goToFingerprint_btn = new Button();
            databaseConnectionTest_btn = new Button();
            scanQRTest_btn = new Button();
            SuspendLayout();
            // 
            // goToFingerprint_btn
            // 
            goToFingerprint_btn.Location = new Point(34, 27);
            goToFingerprint_btn.Name = "goToFingerprint_btn";
            goToFingerprint_btn.Size = new Size(162, 66);
            goToFingerprint_btn.TabIndex = 0;
            goToFingerprint_btn.Text = "Fingerprint Test";
            goToFingerprint_btn.UseVisualStyleBackColor = true;
            goToFingerprint_btn.Click += goToFingerprint_btn_Click;
            // 
            // databaseConnectionTest_btn
            // 
            databaseConnectionTest_btn.Location = new Point(222, 27);
            databaseConnectionTest_btn.Name = "databaseConnectionTest_btn";
            databaseConnectionTest_btn.Size = new Size(162, 66);
            databaseConnectionTest_btn.TabIndex = 1;
            databaseConnectionTest_btn.Text = "Database Connection Test";
            databaseConnectionTest_btn.UseVisualStyleBackColor = true;
            databaseConnectionTest_btn.Click += databaseConnectionTest_btn_Click;
            // 
            // scanQRTest_btn
            // 
            scanQRTest_btn.Location = new Point(406, 27);
            scanQRTest_btn.Name = "scanQRTest_btn";
            scanQRTest_btn.Size = new Size(162, 66);
            scanQRTest_btn.TabIndex = 2;
            scanQRTest_btn.Text = "Scan QR Test";
            scanQRTest_btn.UseVisualStyleBackColor = true;
            scanQRTest_btn.Click += scanQRTest_btn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(scanQRTest_btn);
            Controls.Add(databaseConnectionTest_btn);
            Controls.Add(goToFingerprint_btn);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button goToFingerprint_btn;
        private Button databaseConnectionTest_btn;
        private Button scanQRTest_btn;
    }
}
