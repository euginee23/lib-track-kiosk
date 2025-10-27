namespace lib_track_kiosk.sub_user_controls
{
    partial class UC_LookBooks
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            books_FlowLayoutPanel = new FlowLayoutPanel();
            panel1 = new Panel();
            search_txtBox = new TextBox();
            label1 = new Label();
            panel2 = new Panel();
            panel3 = new Panel();
            scrollDown_btn = new Guna.UI2.WinForms.Guna2Button();
            scrollUp_btn = new Guna.UI2.WinForms.Guna2Button();
            panel4 = new Panel();
            panel5 = new Panel();
            genre_cmbx = new ComboBox();
            label2 = new Label();
            panel6 = new Panel();
            author_cmbx = new ComboBox();
            label3 = new Label();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
            SuspendLayout();
            // 
            // books_FlowLayoutPanel
            // 
            books_FlowLayoutPanel.BackColor = SystemColors.Window;
            books_FlowLayoutPanel.BorderStyle = BorderStyle.Fixed3D;
            books_FlowLayoutPanel.Location = new Point(15, 15);
            books_FlowLayoutPanel.Name = "books_FlowLayoutPanel";
            books_FlowLayoutPanel.Size = new Size(1703, 760);
            books_FlowLayoutPanel.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ActiveCaption;
            panel1.Controls.Add(search_txtBox);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(821, 68);
            panel1.TabIndex = 1;
            // 
            // search_txtBox
            // 
            search_txtBox.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            search_txtBox.Location = new Point(112, 15);
            search_txtBox.Name = "search_txtBox";
            search_txtBox.Size = new Size(696, 39);
            search_txtBox.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(13, 18);
            label1.Name = "label1";
            label1.Size = new Size(93, 32);
            label1.TabIndex = 1;
            label1.Text = "Search:";
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ActiveCaption;
            panel2.Controls.Add(panel3);
            panel2.Controls.Add(scrollDown_btn);
            panel2.Controls.Add(scrollUp_btn);
            panel2.Location = new Point(12, 96);
            panel2.Name = "panel2";
            panel2.Size = new Size(101, 791);
            panel2.TabIndex = 0;
            // 
            // panel3
            // 
            panel3.BackColor = Color.Gray;
            panel3.Location = new Point(44, 112);
            panel3.Name = "panel3";
            panel3.Size = new Size(11, 564);
            panel3.TabIndex = 0;
            // 
            // scrollDown_btn
            // 
            scrollDown_btn.BackColor = Color.Silver;
            scrollDown_btn.BorderRadius = 5;
            scrollDown_btn.CustomizableEdges = customizableEdges1;
            scrollDown_btn.DisabledState.BorderColor = Color.DarkGray;
            scrollDown_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            scrollDown_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            scrollDown_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            scrollDown_btn.FillColor = Color.FromArgb(0, 64, 64);
            scrollDown_btn.Font = new Font("Segoe UI", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            scrollDown_btn.ForeColor = Color.White;
            scrollDown_btn.Location = new Point(13, 700);
            scrollDown_btn.Name = "scrollDown_btn";
            scrollDown_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            scrollDown_btn.Size = new Size(76, 75);
            scrollDown_btn.TabIndex = 5;
            scrollDown_btn.Text = "↓";
            scrollDown_btn.Click += scrollDown_btn_Click;
            // 
            // scrollUp_btn
            // 
            scrollUp_btn.BackColor = Color.Silver;
            scrollUp_btn.BorderRadius = 5;
            scrollUp_btn.CustomizableEdges = customizableEdges3;
            scrollUp_btn.DisabledState.BorderColor = Color.DarkGray;
            scrollUp_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            scrollUp_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            scrollUp_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            scrollUp_btn.FillColor = Color.FromArgb(0, 64, 64);
            scrollUp_btn.Font = new Font("Segoe UI", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            scrollUp_btn.ForeColor = Color.White;
            scrollUp_btn.Location = new Point(13, 15);
            scrollUp_btn.Name = "scrollUp_btn";
            scrollUp_btn.ShadowDecoration.CustomizableEdges = customizableEdges4;
            scrollUp_btn.Size = new Size(76, 75);
            scrollUp_btn.TabIndex = 4;
            scrollUp_btn.Text = "↑";
            scrollUp_btn.Click += scrollUp_btn_Click;
            // 
            // panel4
            // 
            panel4.BackColor = SystemColors.ActiveBorder;
            panel4.Controls.Add(books_FlowLayoutPanel);
            panel4.Location = new Point(124, 96);
            panel4.Name = "panel4";
            panel4.Size = new Size(1731, 791);
            panel4.TabIndex = 6;
            // 
            // panel5
            // 
            panel5.BackColor = SystemColors.ActiveCaption;
            panel5.Controls.Add(genre_cmbx);
            panel5.Controls.Add(label2);
            panel5.Location = new Point(1396, 12);
            panel5.Name = "panel5";
            panel5.Size = new Size(459, 68);
            panel5.TabIndex = 7;
            // 
            // genre_cmbx
            // 
            genre_cmbx.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            genre_cmbx.FormattingEnabled = true;
            genre_cmbx.Location = new Point(106, 14);
            genre_cmbx.Name = "genre_cmbx";
            genre_cmbx.Size = new Size(341, 40);
            genre_cmbx.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(14, 18);
            label2.Name = "label2";
            label2.Size = new Size(86, 32);
            label2.TabIndex = 1;
            label2.Text = "Genre:";
            // 
            // panel6
            // 
            panel6.BackColor = SystemColors.ActiveCaption;
            panel6.Controls.Add(author_cmbx);
            panel6.Controls.Add(label3);
            panel6.Location = new Point(850, 12);
            panel6.Name = "panel6";
            panel6.Size = new Size(530, 68);
            panel6.TabIndex = 8;
            // 
            // author_cmbx
            // 
            author_cmbx.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            author_cmbx.FormattingEnabled = true;
            author_cmbx.Location = new Point(113, 15);
            author_cmbx.Name = "author_cmbx";
            author_cmbx.Size = new Size(404, 40);
            author_cmbx.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(14, 18);
            label3.Name = "label3";
            label3.Size = new Size(97, 32);
            label3.TabIndex = 1;
            label3.Text = "Author:";
            // 
            // UC_LookBooks
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel6);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "UC_LookBooks";
            Size = new Size(1869, 902);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel books_FlowLayoutPanel;
        private Panel panel1;
        private TextBox search_txtBox;
        private Label label1;
        private Panel panel2;
        private Guna.UI2.WinForms.Guna2Button scrollUp_btn;
        private Guna.UI2.WinForms.Guna2Button scrollDown_btn;
        private Panel panel3;
        private Panel panel4;
        private Panel panel5;
        private ComboBox genre_cmbx;
        private Label label2;
        private Panel panel6;
        private ComboBox author_cmbx;
        private Label label3;
    }
}
