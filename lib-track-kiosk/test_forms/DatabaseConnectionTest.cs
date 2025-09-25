using System;
using System.Data;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using MySql.Data.MySqlClient;

namespace lib_track_kiosk.test_forms
{
    public partial class DatabaseConnectionTest : Form
    {
        private MySqlConnection? connection;

        public DatabaseConnectionTest()
        {
            InitializeComponent();
        }

        private void testConnection_btn_Click(object sender, EventArgs e)
        {
            Database dbConfig = new Database();

            string connectionString = $"Server={dbConfig.Host};" +
                                      $"Database={dbConfig.Name};" +
                                      $"User Id={dbConfig.User};" +
                                      $"Password={dbConfig.Password};" +
                                      $"Port={dbConfig.Port};";

            connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                connectionStatus_txt.Text = "Connection Successful";

                DataTable tables = connection.GetSchema("Tables");

                databaseTables_txt.Text = string.Join(Environment.NewLine,
                    tables.AsEnumerable().Select(row => row["TABLE_NAME"].ToString()));
            }
            catch (Exception ex)
            {
                connectionStatus_txt.Text = $"Connection Failed: {ex.Message}";

                connection?.Dispose();
                connection = null;
            }
        }

        private void DatabaseConnectionTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connection != null)
            {
                try
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error closing connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection = null;
                }
            }
        }
    }
}