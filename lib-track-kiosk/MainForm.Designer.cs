namespace lib_track_kiosk
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panel1 = new Panel();
            panel2 = new Panel();
            label1 = new Label();
            pictureBox4 = new PictureBox();
            pictureBox2 = new PictureBox();
            panel3 = new Panel();
            panel4 = new Panel();
            panelContainer = new Panel();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.Maroon;
            panel1.Location = new Point(-1, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(26, 1084);
            panel1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Maroon;
            panel2.Controls.Add(label1);
            panel2.Controls.Add(pictureBox4);
            panel2.Controls.Add(pictureBox2);
            panel2.Location = new Point(-2, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1933, 76);
            panel2.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(100, 20);
            label1.Name = "label1";
            label1.Size = new Size(348, 32);
            label1.TabIndex = 0;
            label1.Text = "WMSU Lib-Track | CodeHub.Site";
            // 
            // pictureBox4
            // 
            pictureBox4.Image = (Image)resources.GetObject("pictureBox4.Image");
            pictureBox4.Location = new Point(16, 9);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(78, 57);
            pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox4.TabIndex = 22;
            pictureBox4.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(1834, 3);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(64, 70);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 12;
            pictureBox2.TabStop = false;
            pictureBox2.Click += ShutDown_Click;
            // 
            // panel3
            // 
            panel3.BackColor = Color.Maroon;
            panel3.Location = new Point(1894, 75);
            panel3.Name = "panel3";
            panel3.Size = new Size(40, 1019);
            panel3.TabIndex = 1;
            // 
            // panel4
            // 
            panel4.BackColor = Color.Maroon;
            panel4.Location = new Point(-1, 1054);
            panel4.Name = "panel4";
            panel4.Size = new Size(1935, 27);
            panel4.TabIndex = 2;
            // 
            // panelContainer
            // 
            panelContainer.BackColor = Color.White;
            panelContainer.Location = new Point(25, 75);
            panelContainer.Name = "panelContainer";
            panelContainer.Size = new Size(1869, 979);
            panelContainer.TabIndex = 11;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(panelContainer);
            FormBorderStyle = FormBorderStyle.None;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private Panel panel4;
        private PictureBox pictureBox2;
        private Panel panelContainer;
        private PictureBox pictureBox4;
        private Label label1;
    }
}