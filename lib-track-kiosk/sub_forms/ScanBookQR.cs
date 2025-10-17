using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace lib_track_kiosk.sub_forms
{
    public partial class ScanBookQR : Form
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private System.Windows.Forms.Timer _qrTimer;
        private Bitmap _currentFrame;

        private readonly BarcodeReader _qrReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        private bool _decodingActive = false;

        // ✅ Results to return
        public string ScannedType { get; private set; }          // <-- Added
        public int? ScannedBookId { get; private set; }
        public string ScannedBookNumber { get; private set; }
        public int? ScannedResearchPaperId { get; private set; }

        public ScanBookQR()
        {
            InitializeComponent();
            this.Load += ScanBookQR_Load;
            this.FormClosing += ScanBookQR_FormClosing;
        }

        // 🔹 Initialize camera
        private void ScanBookQR_Load(object sender, EventArgs e)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    status_label.Text = "⚠️ No camera detected.";
                    return;
                }

                _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();

                status_label.Text = "📸 Camera started. Please scan the QR code.";
                _decodingActive = true;

                _qrTimer = new System.Windows.Forms.Timer { Interval = 400 };
                _qrTimer.Tick += QrTimer_Tick;
                _qrTimer.Start();
            }
            catch (Exception ex)
            {
                status_label.Text = $"❌ Camera error: {ex.Message}";
            }
        }

        // 🔹 Show live camera feed
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                var frame = (Bitmap)eventArgs.Frame.Clone();
                var old = System.Threading.Interlocked.Exchange(ref _currentFrame, (Bitmap)frame.Clone());
                old?.Dispose();

                if (camera_pbx.IsHandleCreated)
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
                    frame.Dispose();
                }
            }
            catch { }
        }

        // 🔹 QR Decode Loop
        private async void QrTimer_Tick(object sender, EventArgs e)
        {
            if (!_decodingActive) return;

            Bitmap snapshot = null;
            try
            {
                var source = _currentFrame;
                if (source == null) return;

                snapshot = (Bitmap)source.Clone();
                var result = _qrReader.Decode(snapshot);

                if (result != null)
                {
                    _decodingActive = false;
                    _qrTimer.Stop();

                    string qrText = result.Text.Trim();
                    status_label.Text = "✅ QR detected: " + qrText;

                    await SendQrDataToBackend(qrText);
                }
            }
            catch (Exception ex)
            {
                status_label.Text = $"⚠️ Decode error: {ex.Message}";
            }
            finally
            {
                snapshot?.Dispose();
            }
        }

        // 🔹 Send QR Data to Backend
        private async Task SendQrDataToBackend(string qrData)
        {
            try
            {
                string apiUrl = "http://localhost:5000/api/qr/scan";

                using (var client = new HttpClient())
                {
                    var payload = new { qrData = qrData };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        status_label.Text = "❌ Backend error: " + response.ReasonPhrase;
                        RestartScan();
                        return;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();

                    JsonDocument doc;
                    try
                    {
                        doc = JsonDocument.Parse(responseContent);
                    }
                    catch
                    {
                        status_label.Text = "❌ JSON parsing error: invalid backend response.";
                        RestartScan();
                        return;
                    }

                    using (doc)
                    {
                        var root = doc.RootElement;

                        // ✅ check success
                        if (!root.TryGetProperty("success", out var successEl) || successEl.ValueKind != JsonValueKind.True)
                        {
                            string msg = root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                                ? msgEl.GetString()
                                : "Unknown Error";
                            status_label.Text = $"⚠️ Error: {msg}";
                            RestartScan();
                            return;
                        }

                        // ✅ get type
                        string type = root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                            ? typeEl.GetString()
                            : null;

                        if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Object)
                        {
                            status_label.Text = "⚠️ Invalid or missing data from backend.";
                            RestartScan();
                            return;
                        }

                        int? bookId = null;
                        string bookNumber = null;
                        int? researchPaperId = null;

                        if (dataEl.TryGetProperty("qrInfo", out var qrInfoEl) && qrInfoEl.ValueKind == JsonValueKind.Object)
                        {
                            if (qrInfoEl.TryGetProperty("bookId", out var bEl))
                            {
                                if (bEl.ValueKind == JsonValueKind.Number && bEl.TryGetInt32(out var n))
                                    bookId = n;
                                else if (bEl.ValueKind == JsonValueKind.String && int.TryParse(bEl.GetString(), out var n2))
                                    bookId = n2;
                            }

                            if (qrInfoEl.TryGetProperty("bookNumber", out var bnEl))
                            {
                                bookNumber = bnEl.ValueKind == JsonValueKind.String
                                    ? bnEl.GetString()
                                    : bnEl.GetRawText();
                            }

                            if (qrInfoEl.TryGetProperty("researchPaperId", out var rpEl))
                            {
                                if (rpEl.ValueKind == JsonValueKind.Number && rpEl.TryGetInt32(out var rpn))
                                    researchPaperId = rpn;
                                else if (rpEl.ValueKind == JsonValueKind.String && int.TryParse(rpEl.GetString(), out var rpn2))
                                    researchPaperId = rpn2;
                            }
                        }

                        // ✅ Decide type and store scanned data
                        if (string.Equals(type, "book", StringComparison.OrdinalIgnoreCase))
                        {
                            ScannedType = "Book";
                            ScannedBookId = bookId;
                            ScannedBookNumber = bookNumber;
                            ScannedResearchPaperId = null;
                            status_label.Text = $"📘 Type: Book\nID: {bookId}\nNumber: {bookNumber}";
                        }
                        else if (string.Equals(type, "research_paper", StringComparison.OrdinalIgnoreCase))
                        {
                            ScannedType = "Research Paper";
                            ScannedResearchPaperId = researchPaperId;
                            ScannedBookId = null;
                            ScannedBookNumber = null;
                            status_label.Text = $"📄 Type: Research Paper\nID: {researchPaperId}";
                        }
                        else
                        {
                            ScannedType = "Unknown";
                            status_label.Text = $"⚠️ Unknown Type: {type ?? "null"}";
                            RestartScan();
                            return;
                        }

                        await Task.Delay(700);
                        StopCamera();
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                status_label.Text = $"❌ Backend communication error: {ex.Message}";
                RestartScan();
            }
        }

        // 🔹 Restart scanning if invalid
        private void RestartScan()
        {
            _decodingActive = true;
            _qrTimer?.Start();
        }

        // 🔹 Stop and clean up
        private void ScanBookQR_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _decodingActive = false;
                _qrTimer?.Stop();
                _qrTimer?.Dispose();

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
            catch { }
        }

        // 🔹 Stop camera safely
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
                catch { }
                finally
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                    _videoSource = null;
                }
            }
        }
    }
}
