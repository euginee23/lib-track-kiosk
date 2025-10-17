﻿using Guna.UI2.WinForms;
using lib_track_kiosk.configs;
using lib_track_kiosk.models;
using lib_track_kiosk.panel_forms;
using libzkfpcsharp;
using MySql.Data.MySqlClient;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_RegisterFingerprint : UserControl
    {
        private readonly LoginResponse _loggedInUser;

        // SCANNER INITIALIZATION
        private IntPtr mDevHandle = IntPtr.Zero;
        private IntPtr mDBHandle = IntPtr.Zero;
        private int mfpWidth = 0;
        private int mfpHeight = 0;
        private byte[] FPBuffer;
        private byte[] CapTmp;
        private int cbCapTmp = 2048;

        // TEMPLATES
        private byte[] finger1Template;
        private byte[] finger2Template;
        private byte[] finger3Template;

        public UC_RegisterFingerprint(LoginResponse loggedInUser)
        {
            InitializeComponent();
            _loggedInUser = loggedInUser;

            this.Load += UC_RegisterFingerprint_Load;
            this.Disposed += UC_RegisterFingerprint_Disposed;
        }

        private void UC_RegisterFingerprint_Load(object sender, EventArgs e)
        {
            DisplayUserName();
            InitializeFingerprint();
        }

        private void DisplayUserName()
        {
            userName_txt.Text = $"{_loggedInUser.FirstName} {_loggedInUser.LastName}!";
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
                        UpdateStatus($"SDK initialized successfully. {nCount} device(s) detected.");
                    }
                    else
                    {
                        zkfp2.Terminate();
                        UpdateStatus("No fingerprint device connected!");
                    }
                }
                else
                {
                    UpdateStatus($"Initialize failed, ret={ret} !");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Error initializing fingerprint: " + ex.Message);
            }
        }


        private void OpenFingerprintDevice(int deviceIndex)
        {
            if (mDevHandle != IntPtr.Zero)
                return;

            mDevHandle = zkfp2.OpenDevice(deviceIndex);
            if (mDevHandle == IntPtr.Zero)
            {
                UpdateStatus("Failed to open fingerprint device.");
                return;
            }

            mDBHandle = zkfp2.DBInit();
            if (mDBHandle == IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
                UpdateStatus("Failed to initialize fingerprint DB.");
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

            UpdateStatus($"Device opened successfully. Width={mfpWidth}, Height={mfpHeight}");
        }

        private static Bitmap BufferToBitmap(byte[] buffer, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            System.Drawing.Imaging.ColorPalette cp = bmp.Palette;
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
            {
                Marshal.Copy(buffer, y * width, ptr + y * stride, width);
            }

            bmp.UnlockBits(data);
            return bmp;
        }

        private bool IsFingerprintDuplicate(byte[] templateToCheck)
        {
            if (finger1Template != null && zkfp2.DBMatch(mDBHandle, templateToCheck, finger1Template) > 0)
                return true;
            if (finger2Template != null && zkfp2.DBMatch(mDBHandle, templateToCheck, finger2Template) > 0)
                return true;
            if (finger3Template != null && zkfp2.DBMatch(mDBHandle, templateToCheck, finger3Template) > 0)
                return true;

            return false;
        }

        private void CaptureFinger(PictureBox pictureBox, out byte[] templateStorage)
        {
            templateStorage = null;

            if (mDevHandle == IntPtr.Zero)
            {
                UpdateStatus("Fingerprint device not opened.");
                return;
            }

            UpdateStatus("Please place your finger on the scanner...");
            Thread captureThread = new Thread(() =>
            {
                while (true)
                {
                    cbCapTmp = 2048;
                    int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                    if (ret == zkfp.ZKFP_ERR_OK)
                    {
                        Bitmap bmp = BufferToBitmap(FPBuffer, mfpWidth, mfpHeight);
                        byte[] templateBytes = new byte[cbCapTmp];
                        Array.Copy(CapTmp, templateBytes, cbCapTmp);

                        if (IsFingerprintDuplicate(templateBytes))
                        {
                            this.Invoke((Action)(() =>
                            {
                                UpdateStatus("This finger is already scanned. Please scan a different finger.");
                            }));
                            break; 
                        }

                        this.Invoke((Action)(() =>
                        {
                            pictureBox.Image = bmp;
                            if (pictureBox == finger1_picturebox) finger1Template = templateBytes;
                            if (pictureBox == finger3_picturebox) finger2Template = templateBytes;
                            if (pictureBox == finger4_picturebox) finger3Template = templateBytes;

                            UpdateStatus("Fingerprint captured successfully.");
                        }));
                        break; 
                    }
                    Thread.Sleep(200);
                }
            });
            captureThread.IsBackground = true;
            captureThread.Start();
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

                UpdateStatus("Fingerprint device closed and SDK terminated.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error during fingerprint cleanup: " + ex.Message);
            }
        }

        private void UC_RegisterFingerprint_Disposed(object sender, EventArgs e)
        {
            CleanupFingerprint();
        }

        private void UpdateStatus(string text)
        {
            if (status_label.InvokeRequired)
            {
                status_label.Invoke(new Action(() => status_label.Text = text));
            }
            else
            {
                status_label.Text = text;
            }
        }

        private void StoreFingerprints()
        {
            if (finger1Template == null || finger2Template == null || finger3Template == null)
            {
                UpdateStatus("All three fingerprints must be captured to save.");
                return;
            }

            try
            {
                Database db = new Database();
                string connStr = $"server={db.Host};port={db.Port};database={db.Name};user={db.User};password={db.Password}";

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    long userId = 0;
                    string getUserId = "SELECT user_id FROM users WHERE email=@em LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(getUserId, conn))
                    {
                        cmd.Parameters.AddWithValue("@em", _loggedInUser.Email);
                        object result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("User not found in database!");
                            return;
                        }
                        userId = Convert.ToInt64(result);
                    }

                    string insertFP = "INSERT INTO fingerprints (user_id, finger_type, data, created_at) VALUES (@uid, @type, @data, NOW())";

                    if (ValidateFingerType(finger1Type_label.Text))
                    {
                        using (MySqlCommand cmdFP = new MySqlCommand(insertFP, conn))
                        {
                            cmdFP.Parameters.AddWithValue("@uid", userId);
                            cmdFP.Parameters.AddWithValue("@type", finger1Type_label.Text.Trim());
                            cmdFP.Parameters.Add("@data", MySqlDbType.Blob).Value = finger1Template;
                            cmdFP.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        UpdateStatus("Invalid type for Finger 1. Please select and scan a valid finger.");
                        return;
                    }

                    if (ValidateFingerType(finger2Type_label.Text))
                    {
                        using (MySqlCommand cmdFP = new MySqlCommand(insertFP, conn))
                        {
                            cmdFP.Parameters.AddWithValue("@uid", userId);
                            cmdFP.Parameters.AddWithValue("@type", finger2Type_label.Text.Trim());
                            cmdFP.Parameters.Add("@data", MySqlDbType.Blob).Value = finger2Template;
                            cmdFP.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        UpdateStatus("Invalid type for Finger 2. Please select and scan a valid finger.");
                        return;
                    }

                    if (ValidateFingerType(finger3Type_label.Text))
                    {
                        using (MySqlCommand cmdFP = new MySqlCommand(insertFP, conn))
                        {
                            cmdFP.Parameters.AddWithValue("@uid", userId);
                            cmdFP.Parameters.AddWithValue("@type", finger3Type_label.Text.Trim());
                            cmdFP.Parameters.Add("@data", MySqlDbType.Blob).Value = finger3Template;
                            cmdFP.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        UpdateStatus("Invalid type for Finger 3. Please select and scan a valid finger.");
                        return;
                    }

                    MessageBox.Show("Fingerprints saved successfully!");
                    UpdateStatus("Fingerprints saved successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message);
                UpdateStatus("Error saving fingerprints: " + ex.Message);
            }
        }

        private bool ValidateFingerType(string fingerType)
        {
            if (string.IsNullOrEmpty(fingerType))
                return false;

            string[] validFingerTypes = { "Thumb", "Index", "Middle", "Ring", "Little" };
            return validFingerTypes.Contains(fingerType.Trim());
        }

        private void exitRegisterFingerprint_btn_Click(object sender, EventArgs e)
        {
            CleanupFingerprint();

            if (this.ParentForm is MainForm mainForm)
            {
                var welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
            }
        }

        private bool IsFingerTypeAlreadySelected(string fingerType, Guna2HtmlLabel currentLabel)
        {
            if (!string.IsNullOrEmpty(finger1Type_label.Text) && finger1Type_label != currentLabel && finger1Type_label.Text == fingerType)
                return true;
            if (!string.IsNullOrEmpty(finger2Type_label.Text) && finger2Type_label != currentLabel && finger2Type_label.Text == fingerType)
                return true;
            if (!string.IsNullOrEmpty(finger3Type_label.Text) && finger3Type_label != currentLabel && finger3Type_label.Text == fingerType)
                return true;

            return false;
        }

        private void finger1Ready_btn_Click(object sender, EventArgs e)
        {
            using (var selectFingerForm = new lib_track_kiosk.sub_forms.SelectFingerType())
            {
                if (selectFingerForm.ShowDialog() == DialogResult.OK)
                {
                    string fingerType = selectFingerForm.SelectedFinger;

                    if (IsFingerTypeAlreadySelected(fingerType, finger1Type_label))
                    {
                        MessageBox.Show($"{fingerType} already selected. Please choose another finger.");
                        return;
                    }

                    finger1Type_label.Text = fingerType;
                    UpdateStatus($"Please place your {fingerType} finger on the scanner...");
                    CaptureFinger(finger1_picturebox, out _);
                }
                else
                {
                    UpdateStatus("Fingerprint selection cancelled.");
                }
            }
        }

        private void finger2Ready_btn_Click(object sender, EventArgs e)
        {
            using (var selectFingerForm = new lib_track_kiosk.sub_forms.SelectFingerType())
            {
                if (selectFingerForm.ShowDialog() == DialogResult.OK)
                {
                    string fingerType = selectFingerForm.SelectedFinger;

                    if (IsFingerTypeAlreadySelected(fingerType, finger2Type_label))
                    {
                        MessageBox.Show($"{fingerType} already selected. Please choose another finger.");
                        return;
                    }

                    finger2Type_label.Text = fingerType;
                    UpdateStatus($"Please place your {fingerType} finger on the scanner...");
                    CaptureFinger(finger3_picturebox, out _);
                }
                else
                {
                    UpdateStatus("Fingerprint selection cancelled.");
                }
            }
        }

        private void finger3Ready_btn_Click(object sender, EventArgs e)
        {
            using (var selectFingerForm = new lib_track_kiosk.sub_forms.SelectFingerType())
            {
                if (selectFingerForm.ShowDialog() == DialogResult.OK)
                {
                    string fingerType = selectFingerForm.SelectedFinger;

                    if (IsFingerTypeAlreadySelected(fingerType, finger3Type_label))
                    {
                        MessageBox.Show($"{fingerType} already selected. Please choose another finger.");
                        return;
                    }

                    finger3Type_label.Text = fingerType;
                    UpdateStatus($"Please place your {fingerType} finger on the scanner...");
                    CaptureFinger(finger4_picturebox, out _);
                }
                else
                {
                    UpdateStatus("Fingerprint selection cancelled.");
                }
            }
        }

        private void removeFinger1_btn_Click(object sender, EventArgs e)
        {
            finger1_picturebox.Image = null;
            finger1Template = null;
            UpdateStatus("Finger 1 removed.");
        }

        private void removeFinger2_btn_Click(object sender, EventArgs e)
        {
            finger3_picturebox.Image = null;
            finger2Template = null;
            UpdateStatus("Finger 2 removed.");
        }

        private void removeFinger3_btn_Click(object sender, EventArgs e)
        {
            finger4_picturebox.Image = null;
            finger3Template = null;
            UpdateStatus("Finger 3 removed.");
        }

        private void registerPrints_btn_Click(object sender, EventArgs e)
        {
            StoreFingerprints();
        }
    }
}