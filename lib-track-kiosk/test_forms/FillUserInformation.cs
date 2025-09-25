using lib_track_kiosk.configs;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.test_forms
{
    public partial class FillUserInformation : Form
    {
        private byte[] fingerprintTemplate;

        public FillUserInformation(byte[] template)
        {
            InitializeComponent();
            fingerprintTemplate = template;
        }

        private void save_btn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(firstName_txt.Text) ||
                string.IsNullOrWhiteSpace(lastName_txt.Text) ||
                fingerprintTemplate == null)
            {
                MessageBox.Show("Please fill in all fields and make sure fingerprint is captured.");
                return;
            }

            try
            {
                Database db = new Database();
                string connStr = $"server={db.Host};port={db.Port};database={db.Name};user={db.User};password={db.Password}";

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // 1. Insert into users table
                    string insertUser = "INSERT INTO users (first_name, last_name, created_at) VALUES (@fn, @ln, NOW());";
                    MySqlCommand cmdUser = new MySqlCommand(insertUser, conn);
                    cmdUser.Parameters.AddWithValue("@fn", firstName_txt.Text.Trim());
                    cmdUser.Parameters.AddWithValue("@ln", lastName_txt.Text.Trim());
                    cmdUser.ExecuteNonQuery();

                    long userId = cmdUser.LastInsertedId;

                    // 2. Insert fingerprint with that user_id
                    string insertFP = "INSERT INTO fingerprints (user_id, finger_type, data, created_at) VALUES (@uid, @type, @data, NOW());";
                    MySqlCommand cmdFP = new MySqlCommand(insertFP, conn);
                    cmdFP.Parameters.AddWithValue("@uid", userId);
                    cmdFP.Parameters.AddWithValue("@type", fingerType_cmbx.Text.Trim());
                    cmdFP.Parameters.Add("@data", MySqlDbType.Blob).Value = fingerprintTemplate;
                    cmdFP.ExecuteNonQuery();

                    MessageBox.Show("User and fingerprint saved successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message);
            }
        }
    }
}
