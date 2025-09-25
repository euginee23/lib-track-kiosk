namespace lib_track_kiosk.test_forms
{
    partial class FillUserInformation
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
            firstName_txt = new TextBox();
            label1 = new Label();
            lastName_txt = new TextBox();
            label2 = new Label();
            fingerType_cmbx = new ComboBox();
            label3 = new Label();
            save_btn = new Button();
            groupBox1 = new GroupBox();
            fingerprint_pictureBox = new PictureBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)fingerprint_pictureBox).BeginInit();
            SuspendLayout();
            // 
            // firstName_txt
            // 
            firstName_txt.Location = new Point(329, 22);
            firstName_txt.Name = "firstName_txt";
            firstName_txt.Size = new Size(254, 23);
            firstName_txt.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(260, 25);
            label1.Name = "label1";
            label1.Size = new Size(67, 15);
            label1.TabIndex = 1;
            label1.Text = "First Name:";
            // 
            // lastName_txt
            // 
            lastName_txt.Location = new Point(329, 51);
            lastName_txt.Name = "lastName_txt";
            lastName_txt.Size = new Size(254, 23);
            lastName_txt.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(261, 54);
            label2.Name = "label2";
            label2.Size = new Size(66, 15);
            label2.TabIndex = 1;
            label2.Text = "Last Name:";
            // 
            // fingerType_cmbx
            // 
            fingerType_cmbx.FormattingEnabled = true;
            fingerType_cmbx.Items.AddRange(new object[] { "Thumb", "Index Finger", "Middle Finger", "Ring Finger", "Baby \"Pinky\" Finger" });
            fingerType_cmbx.Location = new Point(329, 80);
            fingerType_cmbx.Name = "fingerType_cmbx";
            fingerType_cmbx.Size = new Size(254, 23);
            fingerType_cmbx.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(256, 83);
            label3.Name = "label3";
            label3.Size = new Size(71, 15);
            label3.TabIndex = 1;
            label3.Text = "Finger Type:";
            // 
            // save_btn
            // 
            save_btn.ForeColor = Color.Green;
            save_btn.Location = new Point(501, 270);
            save_btn.Name = "save_btn";
            save_btn.Size = new Size(82, 23);
            save_btn.TabIndex = 3;
            save_btn.Text = "Save";
            save_btn.UseVisualStyleBackColor = true;
            save_btn.Click += save_btn_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(fingerprint_pictureBox);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(237, 287);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            groupBox1.Text = "Fingerprint";
            // 
            // fingerprint_pictureBox
            // 
            fingerprint_pictureBox.Location = new Point(6, 22);
            fingerprint_pictureBox.Name = "fingerprint_pictureBox";
            fingerprint_pictureBox.Size = new Size(225, 259);
            fingerprint_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            fingerprint_pictureBox.TabIndex = 5;
            fingerprint_pictureBox.TabStop = false;
            // 
            // FillUserInformation
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(597, 314);
            Controls.Add(groupBox1);
            Controls.Add(save_btn);
            Controls.Add(fingerType_cmbx);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lastName_txt);
            Controls.Add(firstName_txt);
            Name = "FillUserInformation";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Save Fingerprint";
            groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)fingerprint_pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox firstName_txt;
        private Label label1;
        private TextBox lastName_txt;
        private Label label2;
        private ComboBox fingerType_cmbx;
        private Label label3;
        private Button save_btn;
        private GroupBox groupBox1;
        private PictureBox fingerprint_pictureBox;
    }
}