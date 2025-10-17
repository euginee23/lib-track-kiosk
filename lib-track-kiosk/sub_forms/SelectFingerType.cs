using System;
using System.Windows.Forms;

namespace lib_track_kiosk.sub_forms
{
    public partial class SelectFingerType : Form
    {
        
        public string SelectedFinger { get; private set; } = null;

        public SelectFingerType()
        {
            InitializeComponent();
        }

        private void thumb_btn_Click(object sender, EventArgs e)
        {
            SelectedFinger = "Thumb";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void index_btn_Click(object sender, EventArgs e)
        {
            SelectedFinger = "Index";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void middle_btn_Click(object sender, EventArgs e)
        {
            SelectedFinger = "Middle";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ring_btn_Click(object sender, EventArgs e)
        {
            SelectedFinger = "Ring";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void pinky_btn_Click(object sender, EventArgs e)
        {
            SelectedFinger = "Pinky";
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void close_btn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
