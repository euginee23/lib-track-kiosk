namespace lib_track_kiosk.user_control_forms
{
    partial class UC_RegisterFingerprint
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
            guna2Button3 = new Guna.UI2.WinForms.Guna2Button();
            exitRegisterFingerprint_btn = new Guna.UI2.WinForms.Guna2Button();
            SuspendLayout();
            // 
            // guna2Button3
            // 
            guna2Button3.BackColor = Color.Transparent;
            guna2Button3.BorderRadius = 20;
            guna2Button3.BorderThickness = 1;
            guna2Button3.CustomizableEdges = customizableEdges1;
            guna2Button3.DisabledState.BorderColor = Color.DarkGray;
            guna2Button3.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button3.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button3.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button3.FillColor = Color.Gray;
            guna2Button3.Font = new Font("Segoe UI", 18F, FontStyle.Italic, GraphicsUnit.Point, 0);
            guna2Button3.ForeColor = Color.White;
            guna2Button3.Location = new Point(3, 3);
            guna2Button3.Name = "guna2Button3";
            guna2Button3.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Button3.Size = new Size(455, 76);
            guna2Button3.TabIndex = 17;
            guna2Button3.Text = "Tap Here to Register Fingerprint";
            // 
            // exitRegisterFingerprint_btn
            // 
            exitRegisterFingerprint_btn.BorderRadius = 20;
            exitRegisterFingerprint_btn.BorderThickness = 1;
            exitRegisterFingerprint_btn.CustomizableEdges = customizableEdges3;
            exitRegisterFingerprint_btn.DisabledState.BorderColor = Color.DarkGray;
            exitRegisterFingerprint_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            exitRegisterFingerprint_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            exitRegisterFingerprint_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            exitRegisterFingerprint_btn.FillColor = Color.FromArgb(192, 0, 0);
            exitRegisterFingerprint_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            exitRegisterFingerprint_btn.ForeColor = Color.White;
            exitRegisterFingerprint_btn.Location = new Point(1816, 3);
            exitRegisterFingerprint_btn.Name = "exitRegisterFingerprint_btn";
            exitRegisterFingerprint_btn.ShadowDecoration.CustomizableEdges = customizableEdges4;
            exitRegisterFingerprint_btn.Size = new Size(50, 51);
            exitRegisterFingerprint_btn.TabIndex = 18;
            exitRegisterFingerprint_btn.Text = "X";
            exitRegisterFingerprint_btn.Click += exitRegisterFingerprint_btn_Click;
            // 
            // UC_RegisterFingerprint
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(exitRegisterFingerprint_btn);
            Controls.Add(guna2Button3);
            Name = "UC_RegisterFingerprint";
            Size = new Size(1869, 979);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button guna2Button3;
        private Guna.UI2.WinForms.Guna2Button exitRegisterFingerprint_btn;
    }
}
