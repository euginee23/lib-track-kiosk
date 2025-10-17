namespace lib_track_kiosk.user_control_forms
{
    partial class UC_LookSearch
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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            exitLook_btn = new Guna.UI2.WinForms.Guna2Button();
            books_btn = new Guna.UI2.WinForms.Guna2Button();
            research_papers_btn = new Guna.UI2.WinForms.Guna2Button();
            lookSearchMain_panel = new Panel();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // exitLook_btn
            // 
            exitLook_btn.BorderRadius = 5;
            exitLook_btn.BorderThickness = 1;
            exitLook_btn.CustomizableEdges = customizableEdges1;
            exitLook_btn.DisabledState.BorderColor = Color.DarkGray;
            exitLook_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            exitLook_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            exitLook_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            exitLook_btn.FillColor = Color.FromArgb(192, 0, 0);
            exitLook_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            exitLook_btn.ForeColor = Color.White;
            exitLook_btn.Location = new Point(1713, 15);
            exitLook_btn.Name = "exitLook_btn";
            exitLook_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            exitLook_btn.Size = new Size(140, 46);
            exitLook_btn.TabIndex = 17;
            exitLook_btn.Text = "CLOSE";
            exitLook_btn.Click += exitLook_btn_Click;
            // 
            // books_btn
            // 
            books_btn.BorderRadius = 5;
            books_btn.BorderThickness = 1;
            books_btn.CustomizableEdges = customizableEdges3;
            books_btn.DisabledState.BorderColor = Color.DarkGray;
            books_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            books_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            books_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            books_btn.FillColor = Color.FromArgb(64, 64, 0);
            books_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            books_btn.ForeColor = Color.White;
            books_btn.Location = new Point(19, 15);
            books_btn.Name = "books_btn";
            books_btn.ShadowDecoration.CustomizableEdges = customizableEdges4;
            books_btn.Size = new Size(192, 46);
            books_btn.TabIndex = 19;
            books_btn.Text = "BOOKS";
            books_btn.Click += books_btn_Click;
            // 
            // research_papers_btn
            // 
            research_papers_btn.BorderRadius = 5;
            research_papers_btn.BorderThickness = 1;
            research_papers_btn.CustomizableEdges = customizableEdges5;
            research_papers_btn.DisabledState.BorderColor = Color.DarkGray;
            research_papers_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            research_papers_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            research_papers_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            research_papers_btn.FillColor = Color.Olive;
            research_papers_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            research_papers_btn.ForeColor = Color.White;
            research_papers_btn.Location = new Point(232, 15);
            research_papers_btn.Name = "research_papers_btn";
            research_papers_btn.ShadowDecoration.CustomizableEdges = customizableEdges6;
            research_papers_btn.Size = new Size(192, 46);
            research_papers_btn.TabIndex = 20;
            research_papers_btn.Text = "RESEARCHES";
            research_papers_btn.Click += research_papers_btn_Click;
            // 
            // lookSearchMain_panel
            // 
            lookSearchMain_panel.Location = new Point(0, 77);
            lookSearchMain_panel.Name = "lookSearchMain_panel";
            lookSearchMain_panel.Size = new Size(1869, 902);
            lookSearchMain_panel.TabIndex = 21;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ActiveCaption;
            panel1.Controls.Add(exitLook_btn);
            panel1.Controls.Add(books_btn);
            panel1.Controls.Add(research_papers_btn);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1869, 79);
            panel1.TabIndex = 22;
            // 
            // UC_LookSearch
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lookSearchMain_panel);
            Controls.Add(panel1);
            Name = "UC_LookSearch";
            Size = new Size(1869, 979);
            Load += UC_LookSearch_Load;
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button exitLook_btn;
        private Guna.UI2.WinForms.Guna2Button books_btn;
        private Guna.UI2.WinForms.Guna2Button research_papers_btn;
        private Panel lookSearchMain_panel;
        private Panel panel1;
    }
}
