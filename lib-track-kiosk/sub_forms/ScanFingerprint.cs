using lib_track_kiosk.configs;
using libzkfpcsharp;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace lib_track_kiosk.sub_forms
{
    public partial class ScanFingerprint : Form
    {
        private IntPtr mDevHandle = IntPtr.Zero;
        private IntPtr mDBHandle = IntPtr.Zero;
        private int mfpWidth = 0;
        private int mfpHeight = 0;
        private byte[] FPBuffer;
        private byte[] CapTmp;
        private int cbCapTmp = 2048;
        private List<(int userId, byte[] template)> fingerprintTemplates = new();

        public int? ScannedUserId { get; private set; }

        public ScanFingerprint()
        {
            InitializeComponent();
            this.Load += ScanFingerprint_Load;
            this.FormClosing += ScanFingerprint_FormClosing;
        }

        private void ScanFingerprint_Load(object sender, EventArgs e)
        {
            InitializeFingerprint();
        }

        private void ScanFingerprint_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupFingerprint();
        }

        private void InitializeFingerprint()
        {
            try
            {
                zkfp2.Terminate();

                int ret = zkfp2.Init();
                if (ret == zkfperrdef.ZKFP_ERR_OK)
                {
                    int nCount = zkfp2.GetDeviceCount();
                    if (nCount > 0)
                    {
                        OpenFingerprintDevice(0);
                        LoadFingerprintTemplates();
                        UpdateStatus($"✅ Device ready. {fingerprintTemplates.Count} fingerprints loaded.");
                        StartCaptureThread();
                    }
                    else
                        UpdateStatus("❌ No fingerprint device connected!");
                }
                else
                    UpdateStatus($"❌ Fingerprint SDK init failed, ret={ret}");
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ Init error: " + ex.Message);
            }
        }

        private void OpenFingerprintDevice(int deviceIndex)
        {
            mDevHandle = zkfp2.OpenDevice(deviceIndex);
            if (mDevHandle == IntPtr.Zero)
            {
                UpdateStatus("❌ Failed to open fingerprint device.");
                return;
            }

            mDBHandle = zkfp2.DBInit();
            if (mDBHandle == IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                UpdateStatus("❌ Failed to initialize fingerprint DB.");
                return;
            }

            byte[] paramValue = new byte[4];
            int size = 4;
            zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpWidth);
            size = 4;
            zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

            FPBuffer = new byte[mfpWidth * mfpHeight];
            CapTmp = new byte[2048];

            UpdateStatus($"📸 Device ready (Width={mfpWidth}, Height={mfpHeight})");
        }

        private void LoadFingerprintTemplates()
        {
            fingerprintTemplates.Clear();
            try
            {
                Database db = new Database();
                string connStr = $"server={db.Host};port={db.Port};database={db.Name};user={db.User};password={db.Password}";

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT user_id, data FROM fingerprints";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fingerprintTemplates.Add((reader.GetInt32("user_id"), (byte[])reader["data"]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ DB load error: " + ex.Message);
            }
        }

        private void StartCaptureThread()
        {
            Thread captureThread = new Thread(() =>
            {
                while (true)
                {
                    if (mDevHandle == IntPtr.Zero)
                        break;

                    cbCapTmp = 2048;
                    int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                    if (ret == zkfp.ZKFP_ERR_OK)
                    {
                        Bitmap bmp = BufferToBitmap(FPBuffer, mfpWidth, mfpHeight);
                        this.Invoke((Action)(() => finger_picturebox.Image = bmp));

                        byte[] template = new byte[cbCapTmp];
                        Array.Copy(CapTmp, template, cbCapTmp);

                        int userId = IdentifyUser(template);
                        if (userId > 0)
                        {
                            this.Invoke((Action)(() => OnUserIdentified(userId)));
                            break;
                        }
                        else
                        {
                            this.Invoke((Action)(() => UpdateStatus("❌ No match found. Try again.")));
                        }
                    }
                    Thread.Sleep(300);
                }
            })
            {
                IsBackground = true
            };
            captureThread.Start();
        }

        private int IdentifyUser(byte[] capturedTemplate)
        {
            int bestScore = 0;
            int matchedUserId = -1;

            foreach (var (userId, template) in fingerprintTemplates)
            {
                int score = zkfp2.DBMatch(mDBHandle, capturedTemplate, template);
                if (score > bestScore)
                {
                    bestScore = score;
                    matchedUserId = userId;
                }
            }

            return bestScore > 60 ? matchedUserId : -1;
        }

        private void OnUserIdentified(int userId)
        {
            // Set the scanned user id and update status, then close immediately.
            ScannedUserId = userId;
            UpdateStatus($"✅ Match found (User ID: {userId})");

            // Do not show any MessageBox or interactive dialog here.
            // Close the form and return DialogResult.OK to the caller.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CleanupFingerprint()
        {
            try
            {
                if (mDevHandle != IntPtr.Zero)
                {
                    zkfp2.CloseDevice(mDevHandle);
                    mDevHandle = IntPtr.Zero;
                }

                if (mDBHandle != IntPtr.Zero)
                {
                    zkfp2.DBFree(mDBHandle);
                    mDBHandle = IntPtr.Zero;
                }

                zkfp2.Terminate();
                UpdateStatus("👋 Device closed.");
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ Cleanup error: " + ex.Message);
            }
        }

        private void UpdateStatus(string text)
        {
            if (status_label.InvokeRequired)
                status_label.Invoke(new Action(() => status_label.Text = text));
            else
                status_label.Text = text;
        }

        private static Bitmap BufferToBitmap(byte[] buffer, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var cp = bmp.Palette;
            for (int i = 0; i < 256; i++)
                cp.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = cp;

            var data = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            int stride = data.Stride;
            IntPtr ptr = data.Scan0;

            for (int y = 0; y < height; y++)
                Marshal.Copy(buffer, y * width, ptr + y * stride, width);

            bmp.UnlockBits(data);
            return bmp;
        }

        private void cancel_btn_Click(object sender, EventArgs e)
        {
            try
            {
                ScannedUserId = null;
                UpdateStatus("❌ Scan canceled by user.");

                CleanupFingerprint();

                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while canceling scan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}