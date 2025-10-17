using lib_track_kiosk.panel_forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_LookSearch : UserControl
    {
        public UC_LookSearch()
        {
            InitializeComponent();
        }

        private async void AddSubUserControl(UserControl uc)
        {
            lookSearchMain_panel.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            lookSearchMain_panel.Controls.Add(uc);
            uc.BringToFront();

            uc.Visible = false;
            await Task.Delay(100);
            uc.Visible = true;
        }

        private void exitLook_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Welcome welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
            }
        }

        private void books_btn_Click(object sender, EventArgs e)
        {
            var lookBooks = new sub_user_controls.UC_LookBooks();
            AddSubUserControl(lookBooks);
        }

        private void research_papers_btn_Click(object sender, EventArgs e)
        {
            var lookResearch = new sub_user_controls.UC_LookResearchPapers();
            AddSubUserControl(lookResearch);
        }

        private void UC_LookSearch_Load(object sender, EventArgs e)
        {
            var defaultView = new sub_user_controls.UC_LookBooks();
            AddSubUserControl(defaultView);
        }
    }
}
