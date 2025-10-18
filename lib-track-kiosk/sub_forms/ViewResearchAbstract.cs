using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.sub_forms
{
    public partial class ViewResearchAbstract : Form
    {
        public ViewResearchAbstract()
        {
            InitializeComponent();
        }

        // Method to set the abstract text
        public void SetAbstract(string abstractText)
        {
            abstract_rtbx.Text = abstractText;
        }

        private void closeAbstract_btn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void scrollUp_btn_Click(object sender, EventArgs e)
        {
            abstract_rtbx.SelectionStart = 0;
            abstract_rtbx.ScrollToCaret();
        }

        private void scrollDown_btn_Click(object sender, EventArgs e)
        {
            abstract_rtbx.SelectionStart = abstract_rtbx.Text.Length;
            abstract_rtbx.ScrollToCaret();
        }
    }

}
