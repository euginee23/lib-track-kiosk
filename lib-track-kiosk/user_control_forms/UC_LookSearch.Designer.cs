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
            exitLook_btn = new Guna.UI2.WinForms.Guna2Button();
            guna2Button4 = new Guna.UI2.WinForms.Guna2Button();
            SuspendLayout();
            // 
            // exitLook_btn
            // 
            exitLook_btn.BorderRadius = 20;
            exitLook_btn.BorderThickness = 1;
            exitLook_btn.CustomizableEdges = customizableEdges1;
            exitLook_btn.DisabledState.BorderColor = Color.DarkGray;
            exitLook_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            exitLook_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            exitLook_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            exitLook_btn.FillColor = Color.FromArgb(192, 0, 0);
            exitLook_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            exitLook_btn.ForeColor = Color.White;
            exitLook_btn.Location = new Point(1815, 4);
            exitLook_btn.Name = "exitLook_btn";
            exitLook_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            exitLook_btn.Size = new Size(50, 51);
            exitLook_btn.TabIndex = 17;
            exitLook_btn.Text = "X";
            exitLook_btn.Click += exitLook_btn_Click;
            // 
            // guna2Button4
            // 
            guna2Button4.BorderRadius = 20;
            guna2Button4.BorderThickness = 1;
            guna2Button4.CustomizableEdges = customizableEdges3;
            guna2Button4.DisabledState.BorderColor = Color.DarkGray;
            guna2Button4.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button4.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button4.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button4.FillColor = Color.FromArgb(128, 64, 0);
            guna2Button4.Font = new Font("Segoe UI", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button4.ForeColor = Color.White;
            guna2Button4.Location = new Point(3, 4);
            guna2Button4.Name = "guna2Button4";
            guna2Button4.ShadowDecoration.CustomizableEdges = customizableEdges4;
            guna2Button4.Size = new Size(782, 159);
            guna2Button4.TabIndex = 18;
            guna2Button4.Text = "LOOK FOR BOOKS / RESEARCH PAPERS";
            // 
            // UC_LookSearch
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(guna2Button4);
            Controls.Add(exitLook_btn);
            Name = "UC_LookSearch";
            Size = new Size(1869, 979);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button exitLook_btn;
        private Guna.UI2.WinForms.Guna2Button guna2Button4;
    }
}
