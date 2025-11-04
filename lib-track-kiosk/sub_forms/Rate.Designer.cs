namespace lib_track_kiosk.sub_forms
{
    partial class Rate
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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            panel6 = new Panel();
            panel1 = new Panel();
            panel2 = new Panel();
            panel3 = new Panel();
            starSelection_flp = new FlowLayoutPanel();
            label1 = new Label();
            comment_rtbx = new RichTextBox();
            label2 = new Label();
            close_btn = new Guna.UI2.WinForms.Guna2Button();
            submit_btn = new Guna.UI2.WinForms.Guna2Button();
            SuspendLayout();
            // 
            // panel6
            // 
            panel6.BackColor = Color.Maroon;
            panel6.Location = new Point(0, 0);
            panel6.Name = "panel6";
            panel6.Size = new Size(1058, 25);
            panel6.TabIndex = 29;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Maroon;
            panel1.Location = new Point(0, 553);
            panel1.Name = "panel1";
            panel1.Size = new Size(1116, 25);
            panel1.TabIndex = 30;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Maroon;
            panel2.Location = new Point(0, 24);
            panel2.Name = "panel2";
            panel2.Size = new Size(25, 573);
            panel2.TabIndex = 31;
            // 
            // panel3
            // 
            panel3.BackColor = Color.Maroon;
            panel3.Location = new Point(1033, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(25, 585);
            panel3.TabIndex = 32;
            // 
            // starSelection_flp
            // 
            starSelection_flp.Location = new Point(104, 98);
            starSelection_flp.Name = "starSelection_flp";
            starSelection_flp.Size = new Size(842, 144);
            starSelection_flp.TabIndex = 33;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(206, 54);
            label1.Name = "label1";
            label1.Size = new Size(621, 30);
            label1.TabIndex = 34;
            label1.Text = "Please rate this book/research paper based on your experience.";
            // 
            // comment_rtbx
            // 
            comment_rtbx.Location = new Point(42, 287);
            comment_rtbx.Name = "comment_rtbx";
            comment_rtbx.Size = new Size(966, 183);
            comment_rtbx.TabIndex = 35;
            comment_rtbx.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(42, 254);
            label2.Name = "label2";
            label2.Size = new Size(216, 30);
            label2.TabIndex = 36;
            label2.Text = "Comment: (Optional)";
            // 
            // close_btn
            // 
            close_btn.BorderRadius = 5;
            close_btn.BorderThickness = 1;
            close_btn.CustomizableEdges = customizableEdges5;
            close_btn.DisabledState.BorderColor = Color.DarkGray;
            close_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            close_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            close_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            close_btn.FillColor = Color.FromArgb(192, 0, 0);
            close_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            close_btn.ForeColor = Color.White;
            close_btn.Location = new Point(726, 496);
            close_btn.Name = "close_btn";
            close_btn.ShadowDecoration.CustomizableEdges = customizableEdges6;
            close_btn.Size = new Size(131, 38);
            close_btn.TabIndex = 37;
            close_btn.Text = "Not Now";
            close_btn.Click += close_btn_Click;
            // 
            // submit_btn
            // 
            submit_btn.BorderRadius = 5;
            submit_btn.BorderThickness = 1;
            submit_btn.CustomizableEdges = customizableEdges7;
            submit_btn.DisabledState.BorderColor = Color.DarkGray;
            submit_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            submit_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            submit_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            submit_btn.FillColor = Color.FromArgb(0, 64, 0);
            submit_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            submit_btn.ForeColor = Color.White;
            submit_btn.Location = new Point(877, 496);
            submit_btn.Name = "submit_btn";
            submit_btn.ShadowDecoration.CustomizableEdges = customizableEdges8;
            submit_btn.Size = new Size(131, 38);
            submit_btn.TabIndex = 38;
            submit_btn.Text = "Submit";
            submit_btn.Click += submit_btn_Click;
            // 
            // Rate
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1054, 578);
            Controls.Add(submit_btn);
            Controls.Add(close_btn);
            Controls.Add(label2);
            Controls.Add(comment_rtbx);
            Controls.Add(label1);
            Controls.Add(starSelection_flp);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(panel6);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Rate";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Rate";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel6;
        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private FlowLayoutPanel starSelection_flp;
        private Label label1;
        private RichTextBox comment_rtbx;
        private Label label2;
        private Guna.UI2.WinForms.Guna2Button close_btn;
        private Guna.UI2.WinForms.Guna2Button submit_btn;
    }
}