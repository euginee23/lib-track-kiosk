namespace lib_track_kiosk.sub_forms
{
    partial class ViewSelectedBook
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
            panel6 = new Panel();
            panel1 = new Panel();
            panel5 = new Panel();
            panel2 = new Panel();
            panel3 = new Panel();
            groupBox3 = new GroupBox();
            bookCover_picturebox = new PictureBox();
            bookInfo_flp = new FlowLayoutPanel();
            close_btn = new Guna.UI2.WinForms.Guna2Button();
            panel3.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bookCover_picturebox).BeginInit();
            SuspendLayout();
            // 
            // panel6
            // 
            panel6.BackColor = Color.Maroon;
            panel6.Location = new Point(0, 0);
            panel6.Name = "panel6";
            panel6.Size = new Size(1225, 25);
            panel6.TabIndex = 29;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Maroon;
            panel1.Location = new Point(0, 649);
            panel1.Name = "panel1";
            panel1.Size = new Size(1225, 25);
            panel1.TabIndex = 30;
            // 
            // panel5
            // 
            panel5.BackColor = Color.Maroon;
            panel5.Location = new Point(0, 23);
            panel5.Name = "panel5";
            panel5.Size = new Size(20, 661);
            panel5.TabIndex = 31;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Maroon;
            panel2.Location = new Point(1201, 24);
            panel2.Name = "panel2";
            panel2.Size = new Size(20, 648);
            panel2.TabIndex = 32;
            // 
            // panel3
            // 
            panel3.BackColor = SystemColors.ControlDark;
            panel3.Controls.Add(groupBox3);
            panel3.Location = new Point(26, 31);
            panel3.Name = "panel3";
            panel3.Size = new Size(431, 610);
            panel3.TabIndex = 33;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(bookCover_picturebox);
            groupBox3.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            groupBox3.Location = new Point(3, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(425, 604);
            groupBox3.TabIndex = 21;
            groupBox3.TabStop = false;
            groupBox3.Text = "Book Cover";
            // 
            // bookCover_picturebox
            // 
            bookCover_picturebox.Location = new Point(6, 34);
            bookCover_picturebox.Name = "bookCover_picturebox";
            bookCover_picturebox.Size = new Size(413, 564);
            bookCover_picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            bookCover_picturebox.TabIndex = 5;
            bookCover_picturebox.TabStop = false;
            // 
            // bookInfo_flp
            // 
            bookInfo_flp.Location = new Point(463, 68);
            bookInfo_flp.Name = "bookInfo_flp";
            bookInfo_flp.Size = new Size(732, 573);
            bookInfo_flp.TabIndex = 34;
            // 
            // close_btn
            // 
            close_btn.BorderRadius = 5;
            close_btn.BorderThickness = 1;
            close_btn.CustomizableEdges = customizableEdges1;
            close_btn.DisabledState.BorderColor = Color.DarkGray;
            close_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            close_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            close_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            close_btn.FillColor = Color.FromArgb(192, 0, 0);
            close_btn.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            close_btn.ForeColor = Color.White;
            close_btn.Location = new Point(1141, 31);
            close_btn.Name = "close_btn";
            close_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            close_btn.Size = new Size(54, 31);
            close_btn.TabIndex = 35;
            close_btn.Text = "X";
            // 
            // ViewSelectedBook
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1220, 668);
            Controls.Add(close_btn);
            Controls.Add(bookInfo_flp);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel5);
            Controls.Add(panel1);
            Controls.Add(panel6);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ViewSelectedBook";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ViewSelectedBook";
            panel3.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)bookCover_picturebox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel6;
        private Panel panel1;
        private Panel panel5;
        private Panel panel2;
        private Panel panel3;
        private GroupBox groupBox3;
        private PictureBox bookCover_picturebox;
        private FlowLayoutPanel bookInfo_flp;
        private Guna.UI2.WinForms.Guna2Button close_btn;
    }
}