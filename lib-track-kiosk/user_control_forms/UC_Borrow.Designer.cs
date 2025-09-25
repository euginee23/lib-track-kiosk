namespace lib_track_kiosk.user_control_forms
{
    partial class UC_Borrow
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
            guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
            exitBorrow_btn = new Guna.UI2.WinForms.Guna2Button();
            SuspendLayout();
            // 
            // guna2Button1
            // 
            guna2Button1.BorderRadius = 20;
            guna2Button1.BorderThickness = 1;
            guna2Button1.CustomBorderThickness = new Padding(3);
            guna2Button1.CustomizableEdges = customizableEdges1;
            guna2Button1.DisabledState.BorderColor = Color.DarkGray;
            guna2Button1.DisabledState.CustomBorderColor = Color.DarkGray;
            guna2Button1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            guna2Button1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            guna2Button1.FillColor = Color.Olive;
            guna2Button1.Font = new Font("Segoe UI", 36F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guna2Button1.ForeColor = Color.White;
            guna2Button1.Location = new Point(3, 4);
            guna2Button1.Name = "guna2Button1";
            guna2Button1.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Button1.Size = new Size(782, 159);
            guna2Button1.TabIndex = 13;
            guna2Button1.Text = "BORROW";
            // 
            // exitBorrow_btn
            // 
            exitBorrow_btn.BorderRadius = 20;
            exitBorrow_btn.BorderThickness = 1;
            exitBorrow_btn.CustomizableEdges = customizableEdges3;
            exitBorrow_btn.DisabledState.BorderColor = Color.DarkGray;
            exitBorrow_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            exitBorrow_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            exitBorrow_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            exitBorrow_btn.FillColor = Color.FromArgb(192, 0, 0);
            exitBorrow_btn.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            exitBorrow_btn.ForeColor = Color.White;
            exitBorrow_btn.Location = new Point(1815, 4);
            exitBorrow_btn.Name = "exitBorrow_btn";
            exitBorrow_btn.ShadowDecoration.CustomizableEdges = customizableEdges4;
            exitBorrow_btn.Size = new Size(50, 51);
            exitBorrow_btn.TabIndex = 17;
            exitBorrow_btn.Text = "X";
            exitBorrow_btn.Click += exitBorrow_btn_Click;
            // 
            // UC_Borrow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(exitBorrow_btn);
            Controls.Add(guna2Button1);
            Name = "UC_Borrow";
            Size = new Size(1869, 979);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button guna2Button1;
        private Guna.UI2.WinForms.Guna2Button exitBorrow_btn;
    }
}
