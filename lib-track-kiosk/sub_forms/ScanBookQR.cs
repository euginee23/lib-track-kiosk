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
using lib_track_kiosk.configs;

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

        // RESULT PROPERTIES
        public string ScannedType { get; private set; }
        public int? ScannedBookId { get; private set; }
        public string ScannedBookNumber { get; private set; }
        public int? ScannedResearchPaperId { get; private set; }

        // Use centralized backend config
        private readonly string ApiUrl = $"{API_Backend.BaseUrl.TrimEnd('/')}/api/qr/scan";

        public ScanBookQR()
        {
            InitializeComponent();
            this.Load += ScanBookQR_Load;
            this.FormClosing += ScanBookQR_FormClosing;
        }

        // INIT CAMERA ON LOAD
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

        // SET NEW FRAME FROM CAMERA
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

        // QR CODE DETECTION TIMER
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

        // SEND QR DATA TO BACKEND
        private async Task SendQrDataToBackend(string qrData)
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    var payload = new { qrData = qrData };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    HttpResponseMessage response;
                    try
                    {
                        response = await client.PostAsync(ApiUrl, content);
                    }
                    catch (TaskCanceledException)
                    {
                        status_label.Text = "❌ Backend error: request timed out.";
                        RestartScan();
                        return;
                    }
                    catch (Exception ex)
                    {
                        status_label.Text = $"❌ Backend error: {ex.Message}";
                        RestartScan();
                        return;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Try to parse JSON even for non-success status codes to show server message
                    JsonDocument doc;
                    try
                    {
                        doc = JsonDocument.Parse(responseContent);
                    }
                    catch
                    {
                        // If parsing fails and status not success, show reason phrase
                        if (!response.IsSuccessStatusCode)
                        {
                            status_label.Text = $"❌ Backend error: {response.ReasonPhrase}";
                            RestartScan();
                            return;
                        }

                        status_label.Text = "❌ JSON parsing error: invalid backend response.";
                        RestartScan();
                        return;
                    }

                    using (doc)
                    {
                        var root = doc.RootElement;

                        // CHECK SUCCESS (allow success as boolean or string "true")
                        bool success = false;
                        if (root.TryGetProperty("success", out var successEl))
                        {
                            if (successEl.ValueKind == JsonValueKind.True)
                                success = true;
                            else if (successEl.ValueKind == JsonValueKind.String && string.Equals(successEl.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                                success = true;
                        }

                        if (!success)
                        {
                            string msg = root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                                ? msgEl.GetString()
                                : (response.IsSuccessStatusCode ? "Unknown Error" : response.ReasonPhrase);
                            status_label.Text = $"⚠️ Error: {msg}";
                            RestartScan();
                            return;
                        }

                        // GET TYPE AND DATA
                        string type = root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                            ? typeEl.GetString()
                            : null;

                        if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Object)
                        {
                            status_label.Text = "⚠️ Invalid or missing data from backend.";
                            RestartScan();
                            return;
                        }

                        // Safely extract qrInfo (which may contain bookId/bookNumber or researchPaperId)
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
                                // bookNumber may be number or string; convert to string
                                if (bnEl.ValueKind == JsonValueKind.Number && bnEl.TryGetInt32(out var bnNum))
                                    bookNumber = bnNum.ToString();
                                else if (bnEl.ValueKind == JsonValueKind.String)
                                    bookNumber = bnEl.GetString();
                                else
                                    bookNumber = bnEl.GetRawText();
                            }

                            if (qrInfoEl.TryGetProperty("researchPaperId", out var rpEl))
                            {
                                if (rpEl.ValueKind == JsonValueKind.Number && rpEl.TryGetInt32(out var rpn))
                                    researchPaperId = rpn;
                                else if (rpEl.ValueKind == JsonValueKind.String && int.TryParse(rpEl.GetString(), out var rpn2))
                                    researchPaperId = rpn2;
                            }
                        }

                        // DETERMINE TYPE AND SET RESULTS
                        if (string.Equals(type, "book", StringComparison.OrdinalIgnoreCase))
                        {
                            ScannedType = "Book";
                            ScannedBookId = bookId;
                            ScannedBookNumber = bookNumber;
                            ScannedResearchPaperId = null;

                            // Try to extract book info for nicer status message (book title and cover URL may be available)
                            string bookTitle = null;
                            string bookCoverUrl = null;
                            if (dataEl.TryGetProperty("book", out var bookEl) && bookEl.ValueKind == JsonValueKind.Object)
                            {
                                if (bookEl.TryGetProperty("book_title", out var bt) && bt.ValueKind == JsonValueKind.String)
                                    bookTitle = bt.GetString();
                                if (bookEl.TryGetProperty("book_cover", out var bc) && bc.ValueKind == JsonValueKind.String)
                                    bookCoverUrl = bc.GetString();
                            }

                            var msg = $"📘 Book scanned";
                            if (bookTitle != null) msg += $": {bookTitle}";
                            if (bookId.HasValue) msg += $"\nID: {bookId}";
                            if (!string.IsNullOrEmpty(bookNumber)) msg += $"\nNumber: {bookNumber}";
                            if (!string.IsNullOrEmpty(bookCoverUrl)) msg += $"\nCover: {bookCoverUrl}";
                            status_label.Text = msg;
                        }
                        else if (string.Equals(type, "research_paper", StringComparison.OrdinalIgnoreCase))
                        {
                            ScannedType = "Research Paper";
                            ScannedResearchPaperId = researchPaperId;
                            ScannedBookId = null;
                            ScannedBookNumber = null;

                            string paperTitle = null;
                            if (dataEl.TryGetProperty("researchPaper", out var rpEl) && rpEl.ValueKind == JsonValueKind.Object)
                            {
                                if (rpEl.TryGetProperty("research_title", out var rt) && rt.ValueKind == JsonValueKind.String)
                                    paperTitle = rt.GetString();
                            }

                            var msg = $"📄 Research paper scanned";
                            if (paperTitle != null) msg += $": {paperTitle}";
                            if (researchPaperId.HasValue) msg += $"\nID: {researchPaperId}";
                            status_label.Text = msg;
                        }
                        else
                        {
                            ScannedType = "Unknown";
                            status_label.Text = $"⚠️ Unknown Type: {type ?? "null"}";
                            RestartScan();
                            return;
                        }

                        // small delay so user can read the status_label
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

        // RESTART SCAN METHOD
        private void RestartScan()
        {
            _decodingActive = true;
            _qrTimer?.Start();
        }

        // STOP CAMERA ON FORM CLOSING
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

        // STOP CAMERA METHOD
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

        private void cancel_btn_Click(object sender, EventArgs e)
        {
            try
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

                status_label.Text = "❌ Scan canceled.";

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