using lib_track_kiosk.configs;
using lib_track_kiosk.loading_forms;
using lib_track_kiosk.models;
using lib_track_kiosk.panel_forms;
using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_UserLogin : UserControl
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private static readonly HttpClient httpClient = new HttpClient();

        // On-screen keyboard helper
        private OnScreenKeyboard _osk;

        public UC_UserLogin()
        {
            InitializeComponent();

            // construct the keyboard helper (uses FileLocations.OnScreenKeyboardExecutable by default)
            try
            {
                _osk = new OnScreenKeyboard();
            }
            catch
            {
                // swallow construction errors — behavior will just fall back to no on-screen keyboard
                _osk = null;
            }

            // wire focus events for textboxes so keyboard opens/closes automatically
            email_txt.GotFocus += Input_GotFocus;
            password_txt.GotFocus += Input_GotFocus;

            email_txt.Leave += Input_Leave;
            password_txt.Leave += Input_Leave;

            // handle Enter key inside the textboxes to close the keyboard
            email_txt.KeyDown += TextBox_KeyDown;
            password_txt.KeyDown += TextBox_KeyDown;

            this.Load += UC_UserLogin_Load;
            login_btn.Click += Login_btn_Click;

            // ensure OSK is disposed when this control is disposed/destroyed
            this.Disposed += (s, e) =>
            {
                try { _osk?.Dispose(); } catch { }
                _osk = null;
            };
        }

        private async void UC_UserLogin_Load(object sender, EventArgs e)
        {
            // show loading form
            var loading = new Loading();
            loading.StartPosition = FormStartPosition.CenterScreen;
            loading.Show();
            loading.BringToFront();
            loading.TopMost = true;
            loading.Refresh();

            bool connected = await ConnectWebSocket(loading);

            if (!connected)
            {
                // cleanup websocket and cts
                TryDisposeWebsocket();

                // show message (optional)
                MessageBox.Show("Unable to connect to server. Please check your network or try again later.",
                    "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // go back to welcome screen
                ReturnToWelcome();
            }
        }

        private async Task<bool> ConnectWebSocket(Form loadingForm)
        {
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                var uri = new Uri(WS_Backend.ServerUrl);
                await _ws.ConnectAsync(uri, _cts.Token);

                if (_ws.State == WebSocketState.Open)
                {
                    LogMessage("Connected to WebSocket server.\r\n");
                    TryCloseLoading(loadingForm);

                    _ = ReceiveMessages();
                    return true;
                }
                else
                {
                    throw new Exception("WebSocket not open after connect attempt.");
                }
            }
            catch (Exception ex)
            {
                LogMessage("WebSocket connection failed: " + ex.Message + "\r\n");

                try { TryCloseLoading(loadingForm); } catch { }
                return false;
            }
        }

        private async void Login_btn_Click(object sender, EventArgs e)
        {
            var email = email_txt.Text.Trim();
            var password = password_txt.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter email and password");
                return;
            }

            await DoManualLogin(email, password);
        }

        private async Task DoManualLogin(string email, string password)
        {
            var loading = new Loading();
            loading.StartPosition = FormStartPosition.CenterScreen;
            loading.Show();
            loading.BringToFront();
            loading.TopMost = true;
            loading.Refresh();

            try
            {
                var loginData = new { identifier = email, password = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{API_Backend.BaseUrl}/api/users/login";
                var response = await httpClient.PostAsync(url, content);
                var respJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(respJson);
                    if (doc.RootElement.TryGetProperty("user", out var userElement))
                    {
                        var user = JsonSerializer.Deserialize<LoginResponse>(
                            userElement.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (user != null)
                        {
                            // After successful login, check fingerprint registration status
                            await ProcessPostLoginAsync(user);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Login successful but no user data returned.");
                    }
                }
                else
                {
                    // Show login failure message
                    try
                    {
                        using var doc = JsonDocument.Parse(respJson);
                        var root = doc.RootElement;
                        var msg = root.TryGetProperty("message", out var m) ? m.GetString() : null;
                        MessageBox.Show(msg ?? "Login failed");
                    }
                    catch
                    {
                        MessageBox.Show("Login failed");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error logging in: " + ex.Message);
            }
            finally
            {
                TryCloseLoading(loading);
            }
        }

        // Check if user has fingerprints registered in database
        private async Task<bool> HasFingerprintsRegistered(int userId)
        {
            return await Task.Run(() =>
            {
                Database dbConfig = new Database();
                string connStr = $"Server={dbConfig.Host};Database={dbConfig.Name};User Id={dbConfig.User};Password={dbConfig.Password};Port={dbConfig.Port};AllowPublicKeyRetrieval=True;SslMode=None;ConnectionTimeout=15;";

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connStr))
                    {
                        conn.Open();
                        string query = "SELECT COUNT(*) FROM fingerprints WHERE user_id = @userId";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", userId);
                            var result = cmd.ExecuteScalar();
                            int count = result != null ? Convert.ToInt32(result) : 0;
                            return count > 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error checking fingerprints in database: {ex.Message}\r\n");
                    // On database error, assume no fingerprints to allow registration attempt
                    return false;
                }
            });
        }

        // After a successful login, decide whether to navigate to fingerprint enrollment or show already registered message
        private async Task ProcessPostLoginAsync(LoginResponse user)
        {
            // Try to get an integer user id using common property names via reflection
            int? userId = TryExtractUserId(user);

            // If we couldn't determine a user id, show error and return to welcome
            if (!userId.HasValue)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Unable to determine user ID. Please contact support.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ReturnToWelcome();
                    }));
                }
                else
                {
                    MessageBox.Show("Unable to determine user ID. Please contact support.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ReturnToWelcome();
                }
                return;
            }

            try
            {
                // Check database directly for existing fingerprints
                bool hasFingerprints = await HasFingerprintsRegistered(userId.Value);

                if (hasFingerprints)
                {
                    // Fingerprint already registered -> inform user and return to welcome screen
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show("There is already a fingerprint registered for this account.", "Fingerprint Already Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ReturnToWelcome();
                        }));
                    }
                    else
                    {
                        MessageBox.Show("There is already a fingerprint registered for this account.", "Fingerprint Already Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ReturnToWelcome();
                    }
                }
                else
                {
                    // No fingerprint found -> navigate to registration
                    NavigateToFingerprintOnUI(user);
                }
            }
            catch (Exception ex)
            {
                // On error, log and inform the user
                LogMessage("Fingerprint check failed: " + ex.Message + "\r\n");
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Could not confirm fingerprint status due to an error. Please try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ReturnToWelcome();
                    }));
                }
                else
                {
                    MessageBox.Show("Could not confirm fingerprint status due to an error. Please try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ReturnToWelcome();
                }
            }
        }

        // Try to extract a numeric user id from the LoginResponse via reflection:
        // common property names: Id, UserId, id, user_id, StudentId, student_id
        private int? TryExtractUserId(LoginResponse user)
        {
            if (user == null) return null;

            try
            {
                var t = user.GetType();
                string[] names = new[] { "Id", "UserId", "id", "user_id", "StudentId", "student_id" };
                foreach (var n in names)
                {
                    var pi = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (pi != null)
                    {
                        var val = pi.GetValue(user);
                        if (val == null) continue;
                        if (val is int i) return i;
                        if (val is long l) return (int)l;
                        if (int.TryParse(val.ToString(), out var iv)) return iv;
                    }
                }
            }
            catch { /* ignore */ }

            return null;
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[4096];

            while (_ws != null && _ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
                        LogMessage("WebSocket closed by server.\r\n");
                    }
                    else
                    {
                        var jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // handle websocket messages that may indicate user logged in
                        HandleMessage(jsonString);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Error receiving message: " + ex.Message + "\r\n");
                    break;
                }
            }
        }

        private void HandleMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeElement) &&
                    typeElement.GetString() == "user-logged-in")
                {
                    var payload = root.GetProperty("payload");
                    var user = JsonSerializer.Deserialize<LoginResponse>(
                        payload.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (user != null)
                    {
                        // run async fingerprint check and navigation without blocking WS loop
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessPostLoginAsync(user).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                LogMessage("Error during websocket post-login processing: " + ex.Message + "\r\n");
                            }
                        });
                        return;
                    }
                }

                var pretty = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                LogMessage(pretty + "\r\n");
            }
            catch
            {
                LogMessage(json + "\r\n");
            }
        }

        // Navigate to fingerprint registration on UI thread
        private void NavigateToFingerprintOnUI(LoginResponse user)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => NavigateToFingerprintOnUI(user)));
                return;
            }

            ShowUserInfo(user);

            if (this.ParentForm is MainForm mainForm)
            {
                // pass user details to UC_RegisterFingerprint
                var fingerprintScreen = new UC_RegisterFingerprint(user);
                mainForm.addUserControl(fingerprintScreen);
            }
        }

        private void NavigateToFingerprint(LoginResponse user)
        {
            // kept for backward compatibility, routes immediately to registration (not used in new flow)
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => NavigateToFingerprint(user)));
                return;
            }

            ShowUserInfo(user);

            if (this.ParentForm is MainForm mainForm)
            {
                // pass user details to UC_RegisterFingerprint
                var fingerprintScreen = new UC_RegisterFingerprint(user);
                mainForm.addUserControl(fingerprintScreen);
            }
        }

        private void ShowUserInfo(LoginResponse user)
        {
            var info = new StringBuilder();
            info.AppendLine("User Logged In:");
            info.AppendLine($"Name: {user.FirstName} {user.LastName}");
            info.AppendLine($"Email: {user.Email}");
            info.AppendLine($"StudentId: {user.StudentId}");
            info.AppendLine($"Contact Number: {user.ContactNumber}");
            info.AppendLine($"Email Verification: {user.Email_Verification}");
            info.AppendLine($"Librarian Approval: {user.Librarian_Approval}");
            info.AppendLine();

            LogMessage(info.ToString());
        }

        // replacement for dataRecieved_txt
        private void LogMessage(string text)
        {
            // use console for debugging or your own logger
            Console.WriteLine(text);
            // or write to a file if needed
        }

        private async void cancel_btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "User canceled", CancellationToken.None);
                }
                _cts?.Cancel();
            }
            catch { /* ignore errors */ }

            ReturnToWelcome();
        }

        // Helper: Dispose websocket and cancellation token safely
        private void TryDisposeWebsocket()
        {
            try
            {
                try { _ws?.Dispose(); } catch { }
                _ws = null;
            }
            catch { }
            try
            {
                try { _cts?.Cancel(); } catch { }
                try { _cts?.Dispose(); } catch { }
                _cts = null;
            }
            catch { }
        }

        // Replace direct this.Close() semantics with returning to UC_Welcome
        private void ReturnToWelcome()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ReturnToWelcome));
                return;
            }

            // cleanup resources
            TryDisposeWebsocket();
            try { _osk?.Dispose(); } catch { }
            _osk = null;

            if (this.ParentForm is MainForm mainForm)
            {
                var welcome = new UC_Welcome();
                mainForm.addUserControl(welcome);
            }
        }

        // Safely close a Loading form from any thread
        private void TryCloseLoading(Form loading)
        {
            if (loading == null) return;
            try
            {
                if (loading.IsDisposed) return;
                if (loading.InvokeRequired)
                    loading.Invoke(new Action(() =>
                    {
                        try { if (!loading.IsDisposed) loading.Close(); } catch { }
                    }));
                else
                {
                    try { if (!loading.IsDisposed) loading.Close(); } catch { }
                }
            }
            catch { /* swallow */ }
        }

        // ------------------ On-screen keyboard related handlers ------------------

        private void Input_GotFocus(object sender, EventArgs e)
        {
            // Open the on-screen keyboard (fire-and-forget to avoid blocking UI).
            if (_osk == null) return;
            Task.Run(() =>
            {
                try { _osk.Open(); } catch { }
            });
        }

        private async void Input_Leave(object sender, EventArgs e)
        {
            // Debounce a little to avoid closing when focus briefly moves between the two textboxes.
            await Task.Delay(200).ConfigureAwait(true);

            // If neither textbox has focus, close the keyboard.
            bool emailFocused = false;
            bool passwordFocused = false;

            // Access Focused properties on UI thread
            if (this.IsHandleCreated)
            {
                try
                {
                    emailFocused = email_txt.Focused;
                    passwordFocused = password_txt.Focused;
                }
                catch { }
            }

            if (!emailFocused && !passwordFocused)
            {
                try { _osk?.Close(); } catch { }
            }
        }

        // Close the on-screen keyboard when Enter is pressed in either textbox.
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try { _osk?.Close(); } catch { }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            TryDisposeWebsocket();
            try { _osk?.Dispose(); } catch { }
            _osk = null;
        }
    }
}