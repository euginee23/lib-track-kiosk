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

namespace lib_track_kiosk
{
    public partial class MainForm : Form
    {
        public Panel PanelContainer => panelContainer;

        public MainForm()
        {
            InitializeComponent();
        }

        public void addUserControl(UserControl userControl)
        {
            userControl.Dock = DockStyle.Fill;
            panelContainer.Controls.Clear();
            panelContainer.Controls.Add(userControl);
            userControl.BringToFront();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UC_Welcome ucWelcome = new UC_Welcome();
            addUserControl(ucWelcome);
        }

        private void ShutDown_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Settings has not yet been implemented.");
        }
    }
}