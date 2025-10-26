namespace lib_track_kiosk.sub_forms
{
    partial class PostAssessmentSurvey
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PostAssessmentSurvey));
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            pictureBox3 = new PictureBox();
            pictureBox4 = new PictureBox();
            surveyPanelNav = new Panel();
            close_btn = new Guna.UI2.WinForms.Guna2Button();
            panel1 = new Panel();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(658, 19);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(117, 114);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(21, 19);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(117, 114);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 1;
            pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(163, 12);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(461, 128);
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.TabIndex = 2;
            pictureBox3.TabStop = false;
            // 
            // pictureBox4
            // 
            pictureBox4.Image = (Image)resources.GetObject("pictureBox4.Image");
            pictureBox4.Location = new Point(152, 130);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(487, 128);
            pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox4.TabIndex = 3;
            pictureBox4.TabStop = false;
            // 
            // surveyPanelNav
            // 
            surveyPanelNav.BorderStyle = BorderStyle.Fixed3D;
            surveyPanelNav.Location = new Point(216, 285);
            surveyPanelNav.Name = "surveyPanelNav";
            surveyPanelNav.Size = new Size(350, 350);
            surveyPanelNav.TabIndex = 6;
            // 
            // close_btn
            // 
            close_btn.BorderRadius = 5;
            close_btn.CustomizableEdges = customizableEdges1;
            close_btn.DisabledState.BorderColor = Color.DarkGray;
            close_btn.DisabledState.CustomBorderColor = Color.DarkGray;
            close_btn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            close_btn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            close_btn.FillColor = Color.Maroon;
            close_btn.Font = new Font("Segoe UI", 9F);
            close_btn.ForeColor = Color.White;
            close_btn.Location = new Point(664, 4);
            close_btn.Name = "close_btn";
            close_btn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            close_btn.Size = new Size(129, 37);
            close_btn.TabIndex = 11;
            close_btn.Text = "Close";
            close_btn.Click += close_btn_Click;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ActiveCaption;
            panel1.Controls.Add(label1);
            panel1.Controls.Add(close_btn);
            panel1.Location = new Point(2, 697);
            panel1.Name = "panel1";
            panel1.Size = new Size(796, 44);
            panel1.TabIndex = 12;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(10, 7);
            label1.Name = "label1";
            label1.Size = new Size(450, 30);
            label1.TabIndex = 12;
            label1.Text = "Kindly scan the QR code to answer our Suvey.";
            // 
            // PostAssessmentSurvey
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ButtonHighlight;
            ClientSize = new Size(800, 743);
            Controls.Add(panel1);
            Controls.Add(surveyPanelNav);
            Controls.Add(pictureBox4);
            Controls.Add(pictureBox3);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "PostAssessmentSurvey";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PostAssessmentSurvey";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private PictureBox pictureBox4;
        private Panel surveyPanelNav;
        private Guna.UI2.WinForms.Guna2Button close_btn;
        private Panel panel1;
        private Label label1;
    }
}