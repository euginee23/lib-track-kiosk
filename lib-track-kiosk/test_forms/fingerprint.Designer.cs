namespace lib_track_kiosk.test_forms
{
    partial class fingerprint
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
            status_txt = new RichTextBox();
            initializeFingerprint_btn = new Button();
            scanFingerprint_btn = new Button();
            saveFingerprint_btn = new Button();
            verifyFingerprint_btn = new Button();
            fingerprint_pictureBox = new PictureBox();
            cmbIdx = new ComboBox();
            groupBox1 = new GroupBox();
            label1 = new Label();
            label2 = new Label();
            fingerprintData_dgv = new DataGridView();
            Column1 = new DataGridViewCheckBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            Column5 = new DataGridViewTextBoxColumn();
            Column6 = new DataGridViewTextBoxColumn();
            label3 = new Label();
            removeFingerprint_btn = new Button();
            loadDBFingerprints_btn = new Button();
            loadCacheFingerprints_btn = new Button();
            ((System.ComponentModel.ISupportInitialize)fingerprint_pictureBox).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)fingerprintData_dgv).BeginInit();
            SuspendLayout();
            // 
            // status_txt
            // 
            status_txt.Location = new Point(12, 312);
            status_txt.Name = "status_txt";
            status_txt.Size = new Size(398, 59);
            status_txt.TabIndex = 0;
            status_txt.Text = "";
            // 
            // initializeFingerprint_btn
            // 
            initializeFingerprint_btn.ForeColor = Color.FromArgb(0, 0, 192);
            initializeFingerprint_btn.Location = new Point(12, 41);
            initializeFingerprint_btn.Name = "initializeFingerprint_btn";
            initializeFingerprint_btn.Size = new Size(155, 41);
            initializeFingerprint_btn.TabIndex = 1;
            initializeFingerprint_btn.Text = "Initialize Fingerprint";
            initializeFingerprint_btn.UseVisualStyleBackColor = true;
            initializeFingerprint_btn.Click += initializeFingerprint_btn_Click;
            // 
            // scanFingerprint_btn
            // 
            scanFingerprint_btn.ForeColor = SystemColors.ActiveCaptionText;
            scanFingerprint_btn.Location = new Point(12, 88);
            scanFingerprint_btn.Name = "scanFingerprint_btn";
            scanFingerprint_btn.Size = new Size(155, 41);
            scanFingerprint_btn.TabIndex = 2;
            scanFingerprint_btn.Text = "Scan Fingerprint";
            scanFingerprint_btn.UseVisualStyleBackColor = true;
            scanFingerprint_btn.Click += scanFingerprint_btn_Click;
            // 
            // saveFingerprint_btn
            // 
            saveFingerprint_btn.ForeColor = Color.Green;
            saveFingerprint_btn.Location = new Point(12, 135);
            saveFingerprint_btn.Name = "saveFingerprint_btn";
            saveFingerprint_btn.Size = new Size(155, 41);
            saveFingerprint_btn.TabIndex = 3;
            saveFingerprint_btn.Text = "Save Fingerprint";
            saveFingerprint_btn.UseVisualStyleBackColor = true;
            saveFingerprint_btn.Click += saveFingerprint_btn_Click;
            // 
            // verifyFingerprint_btn
            // 
            verifyFingerprint_btn.ForeColor = Color.FromArgb(192, 192, 0);
            verifyFingerprint_btn.Location = new Point(12, 182);
            verifyFingerprint_btn.Name = "verifyFingerprint_btn";
            verifyFingerprint_btn.Size = new Size(155, 41);
            verifyFingerprint_btn.TabIndex = 4;
            verifyFingerprint_btn.Text = "Verify Fingerprint";
            verifyFingerprint_btn.UseVisualStyleBackColor = true;
            verifyFingerprint_btn.Click += verifyFingerprint_btn_Click;
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
            // cmbIdx
            // 
            cmbIdx.FormattingEnabled = true;
            cmbIdx.Location = new Point(66, 12);
            cmbIdx.Name = "cmbIdx";
            cmbIdx.Size = new Size(101, 23);
            cmbIdx.TabIndex = 6;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(fingerprint_pictureBox);
            groupBox1.Location = new Point(173, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(237, 287);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            groupBox1.Text = "Fingerprint";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 294);
            label1.Name = "label1";
            label1.Size = new Size(45, 15);
            label1.TabIndex = 8;
            label1.Text = "Status :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 15);
            label2.Name = "label2";
            label2.Size = new Size(48, 15);
            label2.TabIndex = 9;
            label2.Text = "Device :";
            // 
            // fingerprintData_dgv
            // 
            fingerprintData_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            fingerprintData_dgv.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6 });
            fingerprintData_dgv.Location = new Point(416, 27);
            fingerprintData_dgv.Name = "fingerprintData_dgv";
            fingerprintData_dgv.RowHeadersVisible = false;
            fingerprintData_dgv.Size = new Size(825, 344);
            fingerprintData_dgv.TabIndex = 10;
            // 
            // Column1
            // 
            Column1.HeaderText = "Check";
            Column1.Name = "Column1";
            Column1.Width = 50;
            // 
            // Column2
            // 
            Column2.HeaderText = "ID";
            Column2.Name = "Column2";
            Column2.Width = 50;
            // 
            // Column3
            // 
            Column3.HeaderText = "Finger Type";
            Column3.Name = "Column3";
            // 
            // Column4
            // 
            Column4.HeaderText = "First Name";
            Column4.Name = "Column4";
            Column4.Width = 200;
            // 
            // Column5
            // 
            Column5.HeaderText = "Last Name";
            Column5.Name = "Column5";
            Column5.Width = 200;
            // 
            // Column6
            // 
            Column6.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Column6.HeaderText = "Fingerprint Data";
            Column6.Name = "Column6";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(416, 9);
            label3.Name = "label3";
            label3.Size = new Size(95, 15);
            label3.TabIndex = 11;
            label3.Text = "Fingerprint Data:";
            // 
            // removeFingerprint_btn
            // 
            removeFingerprint_btn.Location = new Point(1050, 377);
            removeFingerprint_btn.Name = "removeFingerprint_btn";
            removeFingerprint_btn.Size = new Size(86, 23);
            removeFingerprint_btn.TabIndex = 13;
            removeFingerprint_btn.Text = "Remove";
            removeFingerprint_btn.UseVisualStyleBackColor = true;
            removeFingerprint_btn.Click += removeFingerprint_btn_Click;
            // 
            // loadDBFingerprints_btn
            // 
            loadDBFingerprints_btn.Location = new Point(12, 229);
            loadDBFingerprints_btn.Name = "loadDBFingerprints_btn";
            loadDBFingerprints_btn.Size = new Size(155, 62);
            loadDBFingerprints_btn.TabIndex = 14;
            loadDBFingerprints_btn.Text = "Load Fingerprints from Database";
            loadDBFingerprints_btn.UseVisualStyleBackColor = true;
            loadDBFingerprints_btn.Click += loadDBFingerprints_btn_Click;
            // 
            // loadCacheFingerprints_btn
            // 
            loadCacheFingerprints_btn.Location = new Point(1142, 377);
            loadCacheFingerprints_btn.Name = "loadCacheFingerprints_btn";
            loadCacheFingerprints_btn.Size = new Size(99, 23);
            loadCacheFingerprints_btn.TabIndex = 15;
            loadCacheFingerprints_btn.Text = "Load Cache";
            loadCacheFingerprints_btn.UseVisualStyleBackColor = true;
            loadCacheFingerprints_btn.Click += loadCacheFingerprints_btn_Click;
            // 
            // fingerprint
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1253, 410);
            Controls.Add(loadCacheFingerprints_btn);
            Controls.Add(loadDBFingerprints_btn);
            Controls.Add(verifyFingerprint_btn);
            Controls.Add(saveFingerprint_btn);
            Controls.Add(scanFingerprint_btn);
            Controls.Add(removeFingerprint_btn);
            Controls.Add(label3);
            Controls.Add(fingerprintData_dgv);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(groupBox1);
            Controls.Add(cmbIdx);
            Controls.Add(initializeFingerprint_btn);
            Controls.Add(status_txt);
            Name = "fingerprint";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fingerprint";
            FormClosing += fingerprint_FormClosing;
            Load += fingerprint_Load;
            ((System.ComponentModel.ISupportInitialize)fingerprint_pictureBox).EndInit();
            groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)fingerprintData_dgv).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox status_txt;
        private Button initializeFingerprint_btn;
        private Button scanFingerprint_btn;
        private Button saveFingerprint_btn;
        private Button verifyFingerprint_btn;
        private PictureBox fingerprint_pictureBox;
        private ComboBox cmbIdx;
        private GroupBox groupBox1;
        private Label label1;
        private Label label2;
        private DataGridView fingerprintData_dgv;
        private Label label3;
        private Button removeFingerprint_btn;
        private DataGridViewCheckBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private DataGridViewTextBoxColumn Column6;
        private Button loadDBFingerprints_btn;
        private Button loadCacheFingerprints_btn;
    }
}