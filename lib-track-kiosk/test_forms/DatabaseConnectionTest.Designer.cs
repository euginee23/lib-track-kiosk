namespace lib_track_kiosk.test_forms
{
    partial class DatabaseConnectionTest
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
            testConnection_btn = new Button();
            databaseTables_txt = new RichTextBox();
            label1 = new Label();
            label2 = new Label();
            connectionStatus_txt = new Label();
            SuspendLayout();
            // 
            // testConnection_btn
            // 
            testConnection_btn.ForeColor = Color.FromArgb(0, 192, 0);
            testConnection_btn.Location = new Point(12, 12);
            testConnection_btn.Name = "testConnection_btn";
            testConnection_btn.Size = new Size(143, 44);
            testConnection_btn.TabIndex = 0;
            testConnection_btn.Text = "Test Connection";
            testConnection_btn.UseVisualStyleBackColor = true;
            testConnection_btn.Click += testConnection_btn_Click;
            // 
            // databaseTables_txt
            // 
            databaseTables_txt.Location = new Point(12, 89);
            databaseTables_txt.Name = "databaseTables_txt";
            databaseTables_txt.Size = new Size(438, 310);
            databaseTables_txt.TabIndex = 1;
            databaseTables_txt.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 71);
            label1.Name = "label1";
            label1.Size = new Size(46, 15);
            label1.TabIndex = 2;
            label1.Text = "Tables :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(170, 27);
            label2.Name = "label2";
            label2.Size = new Size(42, 15);
            label2.TabIndex = 3;
            label2.Text = "Status:";
            // 
            // connectionStatus_txt
            // 
            connectionStatus_txt.AutoSize = true;
            connectionStatus_txt.Location = new Point(218, 27);
            connectionStatus_txt.Name = "connectionStatus_txt";
            connectionStatus_txt.Size = new Size(12, 15);
            connectionStatus_txt.TabIndex = 4;
            connectionStatus_txt.Text = "?";
            // 
            // DatabaseConnectionTest
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(462, 411);
            Controls.Add(connectionStatus_txt);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(databaseTables_txt);
            Controls.Add(testConnection_btn);
            Name = "DatabaseConnectionTest";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DatabaseConnectionTest";
            FormClosing += DatabaseConnectionTest_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button testConnection_btn;
        private RichTextBox databaseTables_txt;
        private Label label1;
        private Label label2;
        private Label connectionStatus_txt;
    }
}