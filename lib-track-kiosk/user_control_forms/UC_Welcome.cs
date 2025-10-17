using lib_track_kiosk.user_control_forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.panel_forms
{
    public partial class UC_Welcome : UserControl
    {
        public UC_Welcome()
        {
            InitializeComponent();
        }

        private void lookForBooksResearch_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_LookSearch lookSearchScreen = new UC_LookSearch();
                mainForm.addUserControl(lookSearchScreen);
            }
        }

        private void borrow_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Borrow borrowScreen = new UC_Borrow();
                mainForm.addUserControl(borrowScreen);
            }
        }

        private void return_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Return returnScreen = new UC_Return();
                mainForm.addUserControl(returnScreen);
            }
        }

        //private void registerFingerprint_btn_Click(object sender, EventArgs e)
        //{
        //    MainForm mainForm = (MainForm)this.ParentForm;
        //    if (mainForm != null)
        //    {
        //        UC_RegisterFingerprint registerFingerprintScreen = new UC_RegisterFingerprint();
        //        mainForm.addUserControl(registerFingerprintScreen);
        //    }
        //}

        private void registerFingerprint_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_UserLogin userLoginScreen = new UC_UserLogin();
                mainForm.addUserControl(userLoginScreen);
            }
        }
    }
}