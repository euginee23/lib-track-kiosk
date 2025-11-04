using lib_track_kiosk.configs;
using libzkfpcsharp;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        // now stores (userId, templateBytes) where templateBytes come from files (or fallback to DB blob)
        private List<(int userId, byte[] template)> fingerprintTemplates = new();
        public int? ScannedUserId { get; private set; }

        private bool isCapturing = false;
        private Thread captureThread;

        public ScanFingerprint()
        {
            InitializeComponent();
            this.Load += ScanFingerprint_Load;
            this.FormClosing += ScanFingerprint_FormClosing;
        }

        // -----------------------------------------------
        // FORM LOAD
        // -----------------------------------------------
        private async void ScanFingerprint_Load(object sender, EventArgs e)
        {
            try
            {
                // Fixes crash caused by libzkfpcsharp looking in wrong folder
                Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                UpdateStatus("🔄 Initializing fingerprint system...");
                await Task.Delay(500); // allow initial startup

                await InitializeFingerprintAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ Load error: " + ex.Message);
            }
        }

        private void ScanFingerprint_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupFingerprint();
        }

        // -----------------------------------------------
        // INITIALIZATION SEQUENCE (with delays)
        // -----------------------------------------------
        private async Task InitializeFingerprintAsync()
        {
            try
            {
                zkfp2.Terminate();
                await Task.Delay(300); // ensure previous context closed

                int ret = zkfp2.Init();
                if (ret != zkfperrdef.ZKFP_ERR_OK)
                {
                    UpdateStatus($"❌ Fingerprint SDK init failed (ret={ret})");
                    return;
                }

                await Task.Delay(400); // allow SDK to load devices

                int nCount = zkfp2.GetDeviceCount();
                if (nCount <= 0)
                {
                    UpdateStatus("❌ No fingerprint device connected!");
                    zkfp2.Terminate();
                    return;
                }

                UpdateStatus("🔌 Opening fingerprint device...");
                await Task.Delay(300);

                OpenFingerprintDevice(0);

                UpdateStatus("📦 Loading fingerprint templates...");
                await Task.Delay(500);

                LoadFingerprintTemplates();

                UpdateStatus($"✅ Device ready. {fingerprintTemplates.Count} fingerprints loaded.");
                StartCaptureThread();
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ Initialization error: " + ex.Message);
            }
        }

        // -----------------------------------------------
        // DEVICE OPENING
        // -----------------------------------------------
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

            UpdateStatus($"📸 Device opened successfully. Width={mfpWidth}, Height={mfpHeight}");
        }

        // -----------------------------------------------
        // LOAD TEMPLATES
        // -----------------------------------------------
        private void LoadFingerprintTemplates()
        {
            fingerprintTemplates.Clear();

            try
            {
                // Ensure templates directory exists (no-op if already present)
                try
                {
                    FileLocations.EnsureTemplatesDirectoryExists();
                }
                catch (Exception exDir)
                {
                    UpdateStatus("⚠️ Template directory error: " + exDir.Message);
                    // continue — we might still be able to read absolute paths stored in DB
                }

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
                            int userId = reader.GetInt32("user_id");
                            object dataObj = reader["data"];

                            if (dataObj == DBNull.Value)
                                continue;

                            // If the DB row still contains a binary blob (old data), handle it:
                            if (dataObj is byte[] blob)
                            {
                                // direct blob -> use as template bytes
                                fingerprintTemplates.Add((userId, blob));
                                continue;
                            }

                            // Otherwise we expect a filename or path stored as string
                            string dataStr = dataObj.ToString().Trim();
                            if (string.IsNullOrEmpty(dataStr))
                                continue;

                            string candidatePath;
                            // if dataStr is a rooted path, use it; otherwise combine with central templates folder
                            if (Path.IsPathRooted(dataStr))
                                candidatePath = dataStr;
                            else
                                candidatePath = Path.Combine(FileLocations.TemplatesDirectory, dataStr);

                            if (File.Exists(candidatePath))
                            {
                                try
                                {
                                    byte[] templateBytes = File.ReadAllBytes(candidatePath);
                                    fingerprintTemplates.Add((userId, templateBytes));
                                }
                                catch (Exception exFile)
                                {
                                    // failed to read file, log via status (non-fatal)
                                    UpdateStatus($"⚠️ Could not read template file for user {userId}: {Path.GetFileName(candidatePath)}");
                                }
                            }
                            else
                            {
                                // file missing — try to handle gracefully and continue
                                UpdateStatus($"⚠️ Template file not found: {Path.GetFileName(candidatePath)} (user {userId})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("⚠️ DB load error: " + ex.Message);
            }
        }

        // -----------------------------------------------
        // CAPTURE THREAD
        // -----------------------------------------------
        private void StartCaptureThread()
        {
            if (isCapturing) return;
            isCapturing = true;

            captureThread = new Thread(() =>
            {
                try
                {
                    // allow device warm-up
                    Thread.Sleep(1000);

                    while (isCapturing)
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
                        Thread.Sleep(250);
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() => UpdateStatus("⚠️ Capture error: " + ex.Message)));
                }
            })
            {
                IsBackground = true
            };
            captureThread.Start();
        }

        // -----------------------------------------------
        // MATCH FINGERPRINT
        // -----------------------------------------------
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
            ScannedUserId = userId;
            UpdateStatus($"✅ Match found (User ID: {userId})");
            isCapturing = false;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // -----------------------------------------------
        // CLEANUP
        // -----------------------------------------------
        private void CleanupFingerprint()
        {
            try
            {
                isCapturing = false;

                if (captureThread != null && captureThread.IsAlive)
                    captureThread.Join(500);

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

        // -----------------------------------------------
        // HELPERS
        // -----------------------------------------------
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
                isCapturing = false;
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