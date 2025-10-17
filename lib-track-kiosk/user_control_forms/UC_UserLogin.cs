﻿using lib_track_kiosk.configs;
using lib_track_kiosk.loading_forms;
using lib_track_kiosk.models;
using lib_track_kiosk.panel_forms;
using System;
using System.Net.Http;
using System.Net.WebSockets;
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

        public UC_UserLogin()
        {
            InitializeComponent();
            this.Load += UC_UserLogin_Load;
            login_btn.Click += Login_btn_Click;
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
                // prevent further websocket use
                _ws?.Dispose();
                _ws = null;
                _cts?.Dispose();
                _cts = null;

                // show message (optional)
                MessageBox.Show("Unable to connect to server. Please check your network or try again later.",
                    "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // go back to welcome screen
                if (this.ParentForm is MainForm mainForm)
                {
                    var welcome = new UC_Welcome();
                    mainForm.addUserControl(welcome);
                }
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
                    loadingForm.Close();

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

                try { loadingForm.Close(); } catch { }
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
                            NavigateToFingerprint(user);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Login successful but no user data returned.");
                    }
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(respJson);
                        var msg = doc.RootElement.GetProperty("message").GetString();
                        MessageBox.Show(msg);
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
                loading.Close();
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[4096];

            while (_ws.State == WebSocketState.Open)
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
                        NavigateToFingerprint(user);
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

        private void NavigateToFingerprint(LoginResponse user)
        {
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

            if (this.ParentForm is MainForm mainForm)
            {
                var welcome = new UC_Welcome();
                mainForm.addUserControl(welcome);
            }
        }
    }
}
