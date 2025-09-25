using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace lib_track_kiosk.test_forms
{
    public partial class ScanQRTest : Form
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;

        private System.Windows.Forms.Timer _qrTimer;
        private Bitmap _currentFrame;

        // Reusable reader (QR only for speed)
        private readonly BarcodeReader _qrReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        // If you want to automatically resume scanning after a successful decode
        private bool _autoRestartAfterDecode = false;
        private bool _decodingActive = false;

        public ScanQRTest()
        {
            InitializeComponent();
            // Ensure event handlers are wired (if not done via Designer)
            this.Load += ScanQRTest_Load;
            this.FormClosing += ScanQRTest_FormClosing;
            enable_btn.Click += enable_btn_Click;
        }

        // LOAD CAMERAS
        private void ScanQRTest_Load(object sender, EventArgs e)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                camera_cmbx.Items.Clear();
                if (_videoDevices.Count == 0)
                {
                    camera_cmbx.Items.Add("No camera found");
                    enable_btn.Enabled = false;
                }
                else
                {
                    foreach (FilterInfo dev in _videoDevices)
                        camera_cmbx.Items.Add(dev.Name);

                    camera_cmbx.SelectedIndex = 0;
                    enable_btn.Enabled = true;
                }

                _qrTimer = new System.Windows.Forms.Timer
                {
                    Interval = 400 // a bit faster; tweak as needed
                };
                _qrTimer.Tick += QrTimer_Tick;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading cameras: " + ex.Message);
            }
        }

        // ENABLE CAMERA
        private void enable_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (_videoDevices == null || _videoDevices.Count == 0) return;

                StopCamera();

                _videoSource = new VideoCaptureDevice(_videoDevices[camera_cmbx.SelectedIndex].MonikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();

                _decodingActive = true;
                _qrTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting camera: " + ex.Message);
            }
        }

        // SHOW CAMERA IN PICTUREBOX + keep a copy for decoding
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                var frame = (Bitmap)eventArgs.Frame.Clone();

                var old = System.Threading.Interlocked.Exchange(ref _currentFrame, (Bitmap)frame.Clone());
                old?.Dispose();

                if (camera_pbx.IsHandleCreated)
                {
                    if (camera_pbx.InvokeRequired)
                    {
                        camera_pbx.BeginInvoke(new Action(() =>
                        {
                            var prev = camera_pbx.Image;
                            camera_pbx.Image = frame;
                            prev?.Dispose();
                        }));
                    }
                    else
                    {
                        var prev = camera_pbx.Image;
                        camera_pbx.Image = frame;
                        prev?.Dispose();
                    }
                }
                else
                {
                    frame.Dispose();
                }
            }
            catch
            {
                // Ignore transient frame errors
            }
        }

        // Timer tick: decode QR
        private async void QrTimer_Tick(object sender, EventArgs e)
        {
            if (!_decodingActive) return;

            Bitmap snapshot = null;
            try
            {
                // Clone current frame to avoid threading issues while decoding
                var source = _currentFrame;
                if (source == null) return;
                snapshot = (Bitmap)source.Clone();

                // Primary (simple) path using compatibility Bitmap overload
                var result = _qrReader.Decode(snapshot);

                if (result != null)
                {
                    _decodingActive = false;
                    _qrTimer.Stop();

                    string qrText = result.Text;
                    MessageBox.Show("QR Detected: " + qrText);

                    await SendQrToBackend(qrText);

                    if (_autoRestartAfterDecode)
                    {
                        _decodingActive = true;
                        _qrTimer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("QR decode error: " + ex.Message);
            }
            finally
            {
                snapshot?.Dispose();
            }
        }

        // POST to backend
        private async Task SendQrToBackend(string qrText)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                var body = new
                {
                    qrData = qrText,
                    scannedAtUtc = DateTime.UtcNow
                };

                string json = JsonConvert.SerializeObject(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                const string apiUrl = "http://localhost:5000/scan";
                HttpResponseMessage resp = await client.PostAsync(apiUrl, content);
                string respStr = await resp.Content.ReadAsStringAsync();

                MessageBox.Show($"Server response ({(int)resp.StatusCode} {resp.StatusCode}):\n{respStr}");
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Error sending QR to server: Request timed out.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending QR to server: " + ex.Message);
            }
        }

        // Cleanup on close
        private void ScanQRTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            _decodingActive = false;
            _qrTimer?.Stop();

            var frame = System.Threading.Interlocked.Exchange(ref _currentFrame, null);
            frame?.Dispose();

            if (camera_pbx.Image != null)
            {
                var img = camera_pbx.Image;
                camera_pbx.Image = null;
                img.Dispose();
            }

            StopCamera();
        }

        private void StopCamera()
        {
            if (_videoSource != null)
            {
                try
                {
                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }
                }
                catch { /* ignore */ }
                finally
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                    _videoSource = null;
                }
            }
        }
    }
}