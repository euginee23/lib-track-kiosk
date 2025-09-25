namespace lib_track_kiosk
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void goToFingerprint_btn_Click(object sender, EventArgs e)
        {
            test_forms.fingerprint fingerprintForm = new test_forms.fingerprint();
            fingerprintForm.Show();
        }

        private void databaseConnectionTest_btn_Click(object sender, EventArgs e)
        {
            test_forms.DatabaseConnectionTest databaseConnectionTestForm = new test_forms.DatabaseConnectionTest();
            databaseConnectionTestForm.Show();
        }

        private void scanQRTest_btn_Click(object sender, EventArgs e)
        {
            test_forms.ScanQRTest scanQRTest = new test_forms.ScanQRTest();
            scanQRTest.Show();
        }
    }
}
