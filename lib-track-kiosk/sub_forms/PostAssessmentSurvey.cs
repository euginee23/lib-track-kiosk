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
    public partial class PostAssessmentSurvey : Form
    {
        private List<UserControl> surveyControls;
        private int currentIndex = 0;

        public PostAssessmentSurvey()
        {
            InitializeComponent();
        }

        private void close_btn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
