using lib_track_kiosk.configs;
using libzkfpcsharp;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.test_forms
{
    public partial class fingerprint : Form
    {
        IntPtr mDevHandle = IntPtr.Zero;
        int cbCapTmp = 2048;

        private byte[] FPBuffer;
        private byte[] CapTmp;

        private int mfpWidth = 0;
        private int mfpHeight = 0;

        private bool isScanOnly = false;

        IntPtr mDBHandle = IntPtr.Zero;
        bool bIsTimeToDie = false;

        private List<StoredFingerprint> storedFingerprints = new List<StoredFingerprint>();
        public fingerprint()
        {
            InitializeComponent();
        }

        private void fingerprint_Load(object sender, EventArgs e)
        {
            scanFingerprint_btn.Enabled = false;
            saveFingerprint_btn.Enabled = false;
            verifyFingerprint_btn.Enabled = false;
        }

        // BITMAP HELPER
        public static void GetBitmap(byte[] buffer, int width, int height, MemoryStream ms)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            System.Drawing.Imaging.ColorPalette cp = bmp.Palette;
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }
            bmp.Palette = cp;

            System.Drawing.Imaging.BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed
            );

            int stride = data.Stride;
            IntPtr ptr = data.Scan0;

            for (int y = 0; y < height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(buffer, y * width, ptr + y * stride, width);
            }

            bmp.UnlockBits(data);

            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        }

        // OPEN DEVICE FUNCTION
        private void OpenFingerprintDevice()
        {
            if (mDevHandle != IntPtr.Zero)
                return;

            // Open device
            mDevHandle = zkfp2.OpenDevice(cmbIdx.SelectedIndex);
            if (mDevHandle == IntPtr.Zero)
            {
                MessageBox.Show("Failed to open fingerprint device.");
                return;
            }

            // Init DB (REQUIRED for ExtractFromImage, DBMatch, etc.)
            mDBHandle = zkfp2.DBInit();
            if (mDBHandle == IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
                MessageBox.Show("Failed to initialize fingerprint DB.");
                return;
            }

            // Read image width/height
            byte[] paramValue = new byte[4];
            int size = 4;

            zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

            size = 4;
            zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

            // Allocate buffers once
            FPBuffer = new byte[mfpWidth * mfpHeight];
            CapTmp = new byte[2048];

            status_txt.Text = $"Device opened successfully. Width={mfpWidth}, Height={mfpHeight}";
        }

        // INITIALIZE FINGERPRINT
        private void initializeFingerprint_btn_Click(object sender, EventArgs e)
        {
            cmbIdx.Items.Clear();
            int ret = zkfperrdef.ZKFP_ERR_OK;

            if ((ret = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
            {
                int nCount = zkfp2.GetDeviceCount();
                if (nCount > 0)
                {
                    for (int i = 0; i < nCount; i++)
                    {
                        cmbIdx.Items.Add(i.ToString());
                    }
                    cmbIdx.SelectedIndex = 0;

                    OpenFingerprintDevice();

                    scanFingerprint_btn.Enabled = true;
                    saveFingerprint_btn.Enabled = true;
                    verifyFingerprint_btn.Enabled = true;

                    status_txt.Text = $"SDK Initialized successfully. {nCount} device(s) detected.";
                }
                else
                {
                    zkfp2.Terminate();
                    status_txt.Text = "No device connected!";
                }
            }
            else
            {
                status_txt.Text = "Initialize failed, ret=" + ret + " !";
            }
        }

        // SCAN FINGERPRINT FUNCTION
        private void scanFingerprint_btn_Click(object sender, EventArgs e)
        {
            if (mDevHandle == IntPtr.Zero)
            {
                MessageBox.Show("Device not opened. Please initialize and open the fingerprint device first.");
                return;
            }

            status_txt.Text = "Please place your finger on the scanner...";
            isScanOnly = true;

            Thread captureThread = new Thread(new ThreadStart(CaptureLoop));
            captureThread.IsBackground = true;
            captureThread.Start();
        }

        // CAPTURE LOOP WAIT FOR FINGERPRINT
        private void CaptureLoop()
        {
            while (isScanOnly)
            {
                cbCapTmp = 2048;
                int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);

                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        GetBitmap(FPBuffer, mfpWidth, mfpHeight, ms);
                        Bitmap bmp = new Bitmap(ms);

                        this.Invoke((Action)(() =>
                        {
                            fingerprint_pictureBox.Image = bmp;
                            status_txt.Text = "Fingerprint scanned successfully.";
                        }));
                    }

                    isScanOnly = false;
                    break;
                }

                Thread.Sleep(200);
            }
        }

        private void saveFingerprint_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (CapTmp != null && cbCapTmp > 0)
                {
                    byte[] templateToSave = new byte[cbCapTmp];
                    Array.Copy(CapTmp, templateToSave, cbCapTmp);

                    FillUserInformation fillForm = new FillUserInformation(templateToSave);
                    fillForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show("No fingerprint template captured. Please scan first.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error preparing fingerprint data: " + ex.Message);
            }
        }

        private void verifyFingerprint_btn_Click(object sender, EventArgs e)
        {
            if (mDevHandle == IntPtr.Zero || mDBHandle == IntPtr.Zero)
            {
                MessageBox.Show("Device/DB not opened. Initialize first.");
                return;
            }

            if (storedFingerprints.Count == 0)
            {
                MessageBox.Show("No fingerprints cached. Please load from DB first.");
                return;
            }

            status_txt.Text = "Please place your finger on the scanner...";
            bIsTimeToDie = false;

            Thread verifyThread = new Thread(() =>
            {
                bool matchFound = false;
                int matchedUserId = -1;
                string matchedName = "";
                DateTime startTime = DateTime.Now;

                while (!bIsTimeToDie && (DateTime.Now - startTime).TotalSeconds < 30)
                {
                    cbCapTmp = 2048;
                    int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                    if (ret == zkfp.ZKFP_ERR_OK)
                    {
                        foreach (var sf in storedFingerprints)
                        {
                            if (sf.Template != null && sf.Template.Length > 0)
                            {
                                int score = zkfp2.DBMatch(mDBHandle, CapTmp, sf.Template);
                                if (score > 0)
                                {
                                    matchedUserId = sf.UserId;
                                    matchedName = $"{sf.FirstName} {sf.LastName}";
                                    matchFound = true;
                                    break;
                                }
                            }
                        }

                        this.Invoke((Action)(() =>
                        {
                            if (matchFound)
                                status_txt.Text = $"Fingerprint match found! User ID={matchedUserId}, Name={matchedName}";
                            else
                                status_txt.Text = "No match found. Try again...";
                        }));

                        if (matchFound) break;
                    }

                    Thread.Sleep(200);
                }

                if (!matchFound)
                {
                    this.Invoke((Action)(() =>
                    {
                        status_txt.Text = "Verification failed. No match found within timeout period.";
                    }));
                }

                bIsTimeToDie = true;
            });

            verifyThread.IsBackground = true;
            verifyThread.Start();
        }

        private void fingerprint_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void removeFingerprint_btn_Click(object sender, EventArgs e)
        {
            List<DataGridViewRow> rowsToRemove = new List<DataGridViewRow>();

            foreach (DataGridViewRow row in fingerprintData_dgv.Rows)
            {
                bool isChecked = Convert.ToBoolean(row.Cells[0].Value);
                if (isChecked)
                {
                    rowsToRemove.Add(row);
                }
            }

            foreach (var row in rowsToRemove)
            {
                fingerprintData_dgv.Rows.Remove(row);
            }

            if (rowsToRemove.Count == 0)
            {
                MessageBox.Show("No fingerprints selected for removal.");
            }
        }

        private void loadDBFingerprints_btn_Click(object sender, EventArgs e)
        {
            try
            {
                storedFingerprints.Clear();

                Database db = new Database();
                string connStr = $"server={db.Host};port={db.Port};database={db.Name};user={db.User};password={db.Password}";

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                SELECT f.fingerprint_id, f.user_id, f.finger_type, f.data,
                       u.first_name, u.last_name
                FROM fingerprints f
                INNER JOIN users u ON f.user_id = u.user_id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StoredFingerprint sf = new StoredFingerprint
                            {
                                UserId = reader.GetInt32("user_id"),
                                FirstName = reader.GetString("first_name"),
                                LastName = reader.GetString("last_name"),
                                FingerType = reader.GetString("finger_type"),
                                Template = (byte[])reader["data"]
                            };

                            storedFingerprints.Add(sf);
                        }
                    }
                }

                status_txt.Text = $"Loaded {storedFingerprints.Count} fingerprints into memory.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading fingerprints: " + ex.Message);
            }
        }

        private void loadCacheFingerprints_btn_Click(object sender, EventArgs e)
        {
            try
            {
                fingerprintData_dgv.Rows.Clear();

                foreach (var sf in storedFingerprints)
                {
                    fingerprintData_dgv.Rows.Add(
                        false,                  
                        sf.UserId,              
                        sf.FingerType,          
                        sf.FirstName,           
                        sf.LastName,            
                        sf.Template             
                    );
                }

                status_txt.Text = $"Populated DataGridView with {storedFingerprints.Count} cached fingerprints.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading cache: " + ex.Message);
            }
        }
    }
}
