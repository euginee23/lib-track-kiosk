namespace lib_track_kiosk.user_control_forms
{
    partial class UC_Return
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
            exitReturn_btn = new Guna.UI2.WinForms.Guna2Button();
            guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
            SuspendLayout();
            // 
            // exitReturn_btn
            // 
            exitReturn_btn.BorderRadius = 20;
            exitReturn_btn.BorderThickness = 1;
            exitReturn_btn.CustomizableEdges = customizableEdges1;
            exitReturn_btn.DisabledState.BorderColor = Color.DarkGray;
            exitReturn_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            exitReturn_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            exitReturn_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            exitReturn_btn.FillColor = Color.FromArgb(192, 0, 0);
            exitReturn_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            exitReturn_btn.ForeColor = Color.White;
            exitReturn_btn.Location = new Point(1815, 4);
            exitReturn_btn.Name = "exitReturn_btn";
            exitReturn_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            exitReturn_btn.Size = new Size(50, 51);
            exitReturn_btn.TabIndex = 16;
            exitReturn_btn.Text = "X";
            exitReturn_btn.Click += exitReturn_btn_Click;
            // 
            // guna2Button1
            // 
            guna2Button1.BorderRadius = 20;
            guna2Button1.BorderThickness = 1;
            guna2Button1.CustomizableEdges = customizableEdges3;
            guna2Button1.DisabledState.BorderColor = Color.DarkGray;
            guna2Button1.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button1.FillColor = Color.Teal;
            guna2Button1.Font = new Font("Segoe UI", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button1.ForeColor = Color.White;
            guna2Button1.Location = new Point(3, 4);
            guna2Button1.Name = "guna2Button1";
            guna2Button1.ShadowDecoration.CustomizableEdges = customizableEdges4;
            guna2Button1.Size = new Size(782, 159);
            guna2Button1.TabIndex = 17;
            guna2Button1.Text = "RETURN";
            // 
            // UC_Return
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(guna2Button1);
            Controls.Add(exitReturn_btn);
            Name = "UC_Return";
            Size = new Size(1869, 979);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button exitReturn_btn;
        private Guna.UI2.WinForms.Guna2Button guna2Button1;
    }
}
