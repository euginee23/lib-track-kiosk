using lib_track_kiosk.helpers;
using lib_track_kiosk.panel_forms;
using lib_track_kiosk.sub_forms;
using lib_track_kiosk.sub_user_controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using System.Globalization;
using System.Linq;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Borrow : UserControl
    {
        // STORAGE FOR SCANNED ITEMS
        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        private int? currentUserId;

        private UC_GenerateReceipt receiptUCInstance;

        // Single shared HttpClient for this control (shared across instances via static)
        private static readonly HttpClient sharedHttpClient = new HttpClient();

        // Cancellation token to cancel ongoing network ops when control is disposed/handle destroyed
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Track if control is active
        private bool _isActive = false;

        public UC_Borrow()
        {
            InitializeComponent();

            // Wire up visibility changed events for cleanup
            this.VisibleChanged += UC_Borrow_VisibleChanged;
            this.Load += UC_Borrow_Load;
            this.Disposed += UC_Borrow_Disposed;
        }

        /// <summary>
        /// Called when control visibility changes. Cleanup when hidden.
        /// </summary>
        private void UC_Borrow_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.Visible && _isActive)
                {
                    Console.WriteLine("🧹 UC_Borrow hidden - cleaning up memory...");
                    CleanupMemory();
                    _isActive = false;
                }
                else if (this.Visible && !_isActive)
                {
                    _isActive = true;
                    Console.WriteLine("✓ UC_Borrow shown");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_Borrow_VisibleChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method to cleanup memory when leaving this control.
        /// Call this from parent form before switching to another control.
        /// </summary>
        public void CleanupMemory()
        {
            try
            {
                Console.WriteLine("🧹 Starting memory cleanup for UC_Borrow...");

                // Cancel any pending operations
                try { _cts?.Cancel(); } catch { }

                // Clear scanned items lists
                scannedBooks?.Clear();
                scannedResearchPapers?.Clear();

                // Dispose receipt UC
                try
                {
                    if (receiptUCInstance != null)
                    {
                        generateReceipt_panel.Controls.Remove(receiptUCInstance);
                        receiptUCInstance.Dispose();
                        receiptUCInstance = null;
                    }
                }
                catch { }

                // Dispose and clear panels
                try { DisposeAndClearPanelChildren(scannedType_panel); } catch { }
                try { DisposeAndClearPanelChildren(generateReceipt_panel); } catch { }

                // Dispose profile picture
                try
                {
                    if (profile_pictureBox != null && profile_pictureBox.Image != null)
                    {
                        var img = profile_pictureBox.Image;
                        profile_pictureBox.Image = null;
                        img.Dispose();
                    }
                }
                catch { }

                // Clear user data
                currentUserId = null;

                // Clear labels
                try
                {
                    if (fullName_lbl != null) fullName_lbl.Text = "";
                    if (email_lbl != null) email_lbl.Text = "";
                    if (contactNumber_lbl != null) contactNumber_lbl.Text = "";
                    if (department_lbl != null) department_lbl.Text = "";
                    if (position_lbl != null) position_lbl.Text = "";
                    if (yearLevel_lbl != null) yearLevel_lbl.Text = "";
                    if (pendingPenalties_lbl != null) pendingPenalties_lbl.Text = "0";
                    if (fines_lbl != null) fines_lbl.Text = "₱0.00";
                    if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = "0";
                    if (maxBooksThatCanBorrow_lbl != null) maxBooksThatCanBorrow_lbl.Text = "0";
                }
                catch { }

                // Re-enable buttons that may have been disabled
                try
                {
                    if (add_btn != null) add_btn.Enabled = true;
                    if (borrow_btn != null) borrow_btn.Enabled = true;
                    if (viewScannedBooks_btn != null) viewScannedBooks_btn.Enabled = true;
                }
                catch { }

                // Force aggressive garbage collection
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

                long memoryAfter = GC.GetTotalMemory(false) / (1024 * 1024);
                Console.WriteLine($"✓ Memory cleanup completed. Current memory: {memoryAfter}MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ CleanupMemory error: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method to reload data when returning to this control.
        /// </summary>
        public async Task ReloadDataAsync()
        {
            try
            {
                Console.WriteLine("🔄 Reloading UC_Borrow data...");

                if (currentUserId.HasValue)
                {
                    await FetchAndDisplayUserInformation(currentUserId.Value);
                }

                Console.WriteLine("✓ UC_Borrow data reloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ReloadDataAsync error: {ex.Message}");
            }
        }

        private void UC_Borrow_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine("🗑️ UC_Borrow disposing...");

            try { this.VisibleChanged -= UC_Borrow_VisibleChanged; } catch { }
            try { this.Load -= UC_Borrow_Load; } catch { }

            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;

            // Dispose receipt UC
            try
            {
                if (receiptUCInstance != null)
                {
                    generateReceipt_panel.Controls.Remove(receiptUCInstance);
                    receiptUCInstance.Dispose();
                    receiptUCInstance = null;
                }
            }
            catch { }

            // Clear all data
            try
            {
                scannedBooks?.Clear();
                scannedResearchPapers?.Clear();
            }
            catch { }

            // Dispose panels
            try { DisposeAndClearPanelChildren(scannedType_panel); } catch { }
            try { DisposeAndClearPanelChildren(generateReceipt_panel); } catch { }

            // Dispose profile picture
            try
            {
                if (profile_pictureBox != null && profile_pictureBox.Image != null)
                {
                    var img = profile_pictureBox.Image;
                    profile_pictureBox.Image = null;
                    img.Dispose();
                }
            }
            catch { }

            // Force final cleanup
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch { }

            Console.WriteLine("✓ UC_Borrow disposed");
        }

        // FORM LOAD - SCAN FINGERPRINT 
        private async void UC_Borrow_Load(object sender, EventArgs e)
        {
            try
            {
                if (this.IsDisposed || _cts.IsCancellationRequested) return;

                using (var scanFingerprint = new ScanFingerprint())
                {
                    var result = scanFingerprint.ShowDialog();

                    if (result == DialogResult.Cancel)
                    {
                        CleanupMemory(); // Cleanup before leaving

                        MainForm mainForm = (MainForm)this.ParentForm;
                        if (mainForm != null)
                        {
                            UC_Welcome welcomeScreen = new UC_Welcome();
                            mainForm.addUserControl(welcomeScreen);
                        }
                        return;
                    }

                    if (result == DialogResult.OK)
                    {
                        int? userId = scanFingerprint.ScannedUserId;
                        if (!userId.HasValue)
                        {
                            MessageBox.Show("⚠️ No valid user detected.");
                            CleanupMemory();
                            return;
                        }

                        _isActive = true;
                        await FetchAndDisplayUserInformation(userId.Value).ConfigureAwait(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_Borrow_Load error: {ex.Message}");
                MessageBox.Show($"Error loading borrow screen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // FETCH USER INFORMATION FROM API
        private async Task FetchAndDisplayUserInformation(int userId)
        {
            try
            {
                currentUserId = userId;

                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto, isRestricted)
                    = await UserFetcher.GetUserInfoAsync(userId);

                if (this.IsHandleCreated)
                {
                    this.Invoke((Action)(() =>
                    {
                        fullName_lbl.Text = fullName;
                        email_lbl.Text = email;
                        contactNumber_lbl.Text = contactNumber;
                        department_lbl.Text = department;
                        position_lbl.Text = position;
                        yearLevel_lbl.Text = yearLevel;
                    }));
                }

                pendingPenalties_lbl.Text = "0";
                fines_lbl.Text = "₱0.00";
                booksCurrentlyBorrowed_lbl.Text = "0";

                // DISPLAY PROFILE PHOTO
                try
                {
                    if (this.profile_pictureBox.Image != null)
                    {
                        try { this.profile_pictureBox.Image.Dispose(); } catch { }
                        this.profile_pictureBox.Image = null;
                    }

                    if (profilePhoto != null)
                    {
                        this.profile_pictureBox.Image = new Bitmap(profilePhoto);
                    }
                    else
                    {
                        string defaultPath = FileLocations.DefaultAvatarPath ?? @"E:\Library-Tracker\lib-track-admin\public\avatar-default.png";
                        if (File.Exists(defaultPath))
                        {
                            using (var fs = File.OpenRead(defaultPath))
                            using (var img = Image.FromStream(fs))
                            {
                                this.profile_pictureBox.Image = new Bitmap(img);
                            }
                        }
                        else
                        {
                            this.profile_pictureBox.Image = null;
                        }
                    }

                    this.profile_pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                }
                catch { }

                // LOAD BORROWING LIMITS
                string userType = position ?? "Student";
                var (_, _, studentMaxBooks, facultyMaxBooks) =
                    await SystemSettingsFetcher.GetBorrowingLimitsAsync();

                int maxBooks = userType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                    ? studentMaxBooks
                    : facultyMaxBooks;

                maxBooksThatCanBorrow_lbl.Text = maxBooks.ToString();

                LoadReceiptGenerationPanel(userType, userId);

                var (penaltyCount, totalFines) = await UpdatePenaltyAndFinesAsync(userId);
                int borrowedCount = await UpdateBooksCurrentlyBorrowedAsync(userId);

                if (isRestricted)
                {
                    string message = $"⚠️ Your account is restricted and cannot borrow items at this time.\n\n" +
                                     "Please contact library staff to resolve the restriction.";

                    var dialogResult = MessageBox.Show(message, "Account Restricted", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        CleanupMemory();

                        MainForm mainForm = this.ParentForm as MainForm;
                        if (mainForm != null)
                        {
                            UC_Welcome welcomeScreen = new UC_Welcome();
                            mainForm.addUserControl(welcomeScreen);
                            return;
                        }
                    }
                    else
                    {
                        DisableActionsForPendingFines();
                    }
                }

                if (penaltyCount > 0 || totalFines > 0.0)
                {
                    string finesFormatted = $"₱{totalFines:N2}";
                    string message = $"⚠️ You have pending penalties ({penaltyCount}) with total fines {finesFormatted}.\n\n" +
                                     "Please pay your fines before borrowing additional items.";

                    var dialogResult = MessageBox.Show(message, "Pending Fines", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        CleanupMemory();

                        MainForm mainForm = this.ParentForm as MainForm;
                        if (mainForm != null)
                        {
                            UC_Welcome welcomeScreen = new UC_Welcome();
                            mainForm.addUserControl(welcomeScreen);
                            return;
                        }
                    }
                    else
                    {
                        DisableActionsForPendingFines();
                    }
                }

                if (borrowedCount >= maxBooks)
                {
                    string message = $"⚠️ You have reached the maximum allowed borrowed items ({borrowedCount}/{maxBooks}).\n\n" +
                                     "You cannot borrow more items until some are returned or your limit is adjusted.";

                    var dialogResult = MessageBox.Show(message, "Maximum Borrowed Reached", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        CleanupMemory();

                        MainForm mainForm = this.ParentForm as MainForm;
                        if (mainForm != null)
                        {
                            UC_Welcome welcomeScreen = new UC_Welcome();
                            mainForm.addUserControl(welcomeScreen);
                            return;
                        }
                    }
                    else
                    {
                        DisableActionsForPendingFines();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error fetching user info: {ex.Message}");
            }
        }

        // EXIT BORROW PANEL
        private void exitBorrow_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("🚪 Exiting Borrow screen...");

                CleanupMemory(); // Cleanup before leaving

                MainForm mainForm = (MainForm)this.ParentForm;
                if (mainForm != null)
                {
                    UC_Welcome welcomeScreen = new UC_Welcome();
                    mainForm.addUserControl(welcomeScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ exitBorrow_btn_Click error: {ex.Message}");
            }
        }

        // LOAD RECEIPT GENERATION PANEL
        private void LoadReceiptGenerationPanel(string userType, int userId)
        {
            try
            {
                if (receiptUCInstance != null)
                {
                    if (receiptUCInstance.UserId.HasValue && receiptUCInstance.UserId.Value != userId)
                    {
                        try
                        {
                            generateReceipt_panel.Controls.Remove(receiptUCInstance);
                            receiptUCInstance.Dispose();
                        }
                        catch { }
                        receiptUCInstance = null;
                    }
                }
            }
            catch { }

            if (receiptUCInstance == null)
            {
                receiptUCInstance = new UC_GenerateReceipt(
                    userType,
                    userId,
                    scannedBooks,
                    scannedResearchPapers
                );
                receiptUCInstance.Dock = DockStyle.Fill;
                generateReceipt_panel.Controls.Clear();
                generateReceipt_panel.Controls.Add(receiptUCInstance);
            }
            else
            {
                try
                {
                    if (!receiptUCInstance.UserId.HasValue || receiptUCInstance.UserId.Value != userId)
                    {
                        generateReceipt_panel.Controls.Remove(receiptUCInstance);
                        try { receiptUCInstance.Dispose(); } catch { }
                        receiptUCInstance = new UC_GenerateReceipt(
                            userType,
                            userId,
                            scannedBooks,
                            scannedResearchPapers
                        );
                        receiptUCInstance.Dock = DockStyle.Fill;
                        generateReceipt_panel.Controls.Clear();
                        generateReceipt_panel.Controls.Add(receiptUCInstance);
                    }
                }
                catch { }
            }
        }

        private bool IsBookAlreadyScanned(int bookId, string bookNumber)
        {
            foreach (var (id, number) in scannedBooks)
            {
                // A book is considered already scanned only if BOTH book_id AND book_number match
                if (id == bookId && 
                    !string.IsNullOrEmpty(bookNumber) && 
                    !string.IsNullOrEmpty(number) &&
                    string.Equals(number, bookNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsResearchPaperAlreadyScanned(int researchPaperId)
        {
            return scannedResearchPapers.Contains(researchPaperId);
        }

        // ADD BOOKS OR RESEARCH PAPERS
        private void add_btn_Click(object sender, EventArgs e)
        {
            using (var scanForm = new ScanBookQR())
            {
                if (scanForm.ShowDialog() == DialogResult.OK)
                {
                    string scannedType = scanForm.ScannedType;

                    DisposeAndClearPanelChildren(scannedType_panel);

                    if (scannedType == "Book" &&
                        scanForm.ScannedBookId.HasValue &&
                        !string.IsNullOrEmpty(scanForm.ScannedBookNumber))
                    {
                        int bookId = scanForm.ScannedBookId.Value;
                        string bookNumber = scanForm.ScannedBookNumber;

                        if (IsBookAlreadyScanned(bookId, bookNumber))
                        {
                            MessageBox.Show("⚠️ This book has already been scanned and won't be added again.");
                        }
                        else
                        {
                            scannedBooks.Add((bookId, bookNumber));

                            var bookInfoUC = new UC_ScannedBookInformation(bookId, bookNumber);
                            bookInfoUC.Dock = DockStyle.Fill;
                            scannedType_panel.Controls.Add(bookInfoUC);
                        }
                    }
                    else if (scannedType == "Research Paper" &&
                             scanForm.ScannedResearchPaperId.HasValue)
                    {
                        int researchPaperId = scanForm.ScannedResearchPaperId.Value;

                        if (IsResearchPaperAlreadyScanned(researchPaperId))
                        {
                            MessageBox.Show("⚠️ This research paper has already been scanned and won't be added again.");
                        }
                        else
                        {
                            scannedResearchPapers.Add(researchPaperId);

                            var researchInfoUC = new UC_ScannedResearchInformation(researchPaperId);
                            researchInfoUC.Dock = DockStyle.Fill;
                            scannedType_panel.Controls.Add(researchInfoUC);
                        }
                    }
                    else
                    {
                        MessageBox.Show("⚠️ Invalid scan data. Please try again.");
                    }
                }
            }

            if (receiptUCInstance != null)
            {
                _ = receiptUCInstance.UpdateScannedItemsAsync(scannedBooks, scannedResearchPapers);
            }
        }

        // VIEW SCANNED BOOKS OR RESEARCH PAPERS
        private void viewScannedBooks_btn_Click(object sender, EventArgs e)
        {
            using (var viewForm = new ViewScannedBooks(scannedBooks, scannedResearchPapers, this))
            {
                viewForm.ShowDialog();
            }
        }

        // DYNAMIC DISPLAY OF SCANNED ITEM
        public void ShowScannedItem(string type, int itemId, string bookNumber = null)
        {
            DisposeAndClearPanelChildren(scannedType_panel);

            if (type == "Book")
            {
                var bookInfoUC = new UC_ScannedBookInformation(itemId, bookNumber);
                bookInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(bookInfoUC);
            }
            else if (type == "Research Paper")
            {
                var researchInfoUC = new UC_ScannedResearchInformation(researchPaperId: itemId);
                researchInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(researchInfoUC);
            }
        }

        // BORROW BUTTON CLICK
        private async void borrow_btn_Click(object sender, EventArgs e)
        {
            if (scannedBooks.Count == 0 && scannedResearchPapers.Count == 0)
            {
                MessageBox.Show("⚠️ No books or research papers scanned. Please add items first.");
                return;
            }

            if (receiptUCInstance == null || !receiptUCInstance.UserId.HasValue)
            {
                MessageBox.Show("⚠️ No valid user detected.");
                return;
            }

            var borrowRequest = new BorrowRequest
            {
                ReferenceNumber = receiptUCInstance.ReferenceNumber,
                UserId = receiptUCInstance.UserId.Value,
                DueDate = receiptUCInstance.DueDate
            };

            foreach (var (bookId, _) in scannedBooks)
                borrowRequest.BookIds.Add(bookId);

            borrowRequest.ResearchPaperIds.AddRange(scannedResearchPapers);

            string tempReceiptPath = null;
            try
            {
                tempReceiptPath = GenerateReceiptImageFromPanel();
                if (!string.IsNullOrEmpty(tempReceiptPath))
                {
                    borrowRequest.ReceiptFilePath = tempReceiptPath;
                }

                var response = await BorrowFetcher.BorrowAsync(borrowRequest);

                if (response.Success)
                {
                    MessageBox.Show($"✅ Borrow successful!\nItems borrowed: {borrowRequest.BookIds.Count + borrowRequest.ResearchPaperIds.Count}\nReference: {borrowRequest.ReferenceNumber}");

                    try
                    {
                        using (var survey = new PostAssessmentSurvey())
                        {
                            var owner = this.FindForm();
                            survey.StartPosition = FormStartPosition.CenterParent;
                            if (owner != null)
                                survey.ShowDialog(owner);
                            else
                                survey.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to show PostAssessmentSurvey: " + ex);
                    }

                    scannedBooks.Clear();
                    scannedResearchPapers.Clear();
                    DisposeAndClearPanelChildren(scannedType_panel);

                    _ = receiptUCInstance?.UpdateScannedItemsAsync(scannedBooks, scannedResearchPapers);

                    // Cleanup before navigating
                    CleanupMemory();

                    MainForm mainForm = this.ParentForm as MainForm;
                    if (mainForm != null)
                    {
                        UC_Welcome welcomeScreen = new UC_Welcome();
                        mainForm.addUserControl(welcomeScreen);
                    }
                }
                else
                {
                    MessageBox.Show($"❌ Borrow failed: {response.Message}\nError: {response.Error}");
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Unexpected error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempReceiptPath) && File.Exists(tempReceiptPath))
                        File.Delete(tempReceiptPath);
                }
                catch { }
            }
        }

        private string GenerateReceiptImageFromPanel()
        {
            if (generateReceipt_panel.Controls.Count == 0)
                return null;

            Bitmap bitmap = null;
            string tempPath = null;
            try
            {
                bitmap = new Bitmap(generateReceipt_panel.Width, generateReceipt_panel.Height);
                generateReceipt_panel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                tempPath = Path.Combine(Path.GetTempPath(), $"Receipt_{DateTime.Now:yyyyMMddHHmmssfff}.jpg");
                bitmap.Save(tempPath, ImageFormat.Jpeg);
            }
            catch
            {
                tempPath = null;
            }
            finally
            {
                try { bitmap?.Dispose(); } catch { }
            }

            return tempPath;
        }

        private async Task<string> UploadReceiptImageToServerAsync(string localFilePath)
        {
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                return null;

            string baseUrl = API_Backend.BaseUrl?.TrimEnd('/') ?? "";
            string uploadUrl = $"{baseUrl}/api/uploads/receipt";

            using (var content = new MultipartFormDataContent())
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(localFilePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "file", Path.GetFileName(localFilePath));

                var resp = await sharedHttpClient.PostAsync(uploadUrl, content, _cts.Token);
                var respBody = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    string serverMsg = respBody;
                    try
                    {
                        var parsed = JObject.Parse(respBody);
                        serverMsg = parsed.Value<string>("message") ?? parsed.Value<string>("error") ?? respBody;
                    }
                    catch { }

                    throw new Exception($"Upload failed ({(int)resp.StatusCode}): {serverMsg}");
                }

                if (string.IsNullOrWhiteSpace(respBody))
                    throw new Exception("Upload returned an empty response body");

                try
                {
                    var root = JObject.Parse(respBody);

                    string[] topLevelCandidates = { "url", "path", "filePath", "filename", "file", "location" };
                    foreach (var key in topLevelCandidates)
                    {
                        var token = root[key];
                        if (token != null)
                        {
                            if (token.Type == JTokenType.String)
                            {
                                var s = token.Value<string>();
                                if (!string.IsNullOrWhiteSpace(s)) return s;
                            }
                            else if (token.Type == JTokenType.Object)
                            {
                                var obj = token as JObject;
                                var url = obj.Value<string>("url") ?? obj.Value<string>("path") ?? obj.Value<string>("fname") ?? obj.Value<string>("filename");
                                if (!string.IsNullOrEmpty(url)) return url;
                            }
                        }
                    }

                    var data = root["data"];
                    if (data != null)
                    {
                        if (data.Type == JTokenType.String)
                        {
                            var s = data.Value<string>();
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                        }
                        else if (data.Type == JTokenType.Object)
                        {
                            var dataObj = data as JObject;
                            var url = dataObj.Value<string>("url")
                                      ?? dataObj.Value<string>("path")
                                      ?? dataObj.Value<string>("filePath")
                                      ?? dataObj.Value<string>("filename")
                                      ?? dataObj.Value<string>("location");
                            if (!string.IsNullOrEmpty(url)) return url;

                            var nestedFile = dataObj["file"] as JObject;
                            if (nestedFile != null)
                            {
                                var nf = nestedFile.Value<string>("url") ?? nestedFile.Value<string>("path") ?? nestedFile.Value<string>("fname") ?? nestedFile.Value<string>("filename");
                                if (!string.IsNullOrEmpty(nf)) return nf;
                            }

                            if (data is JArray dataArr && dataArr.Count > 0)
                            {
                                var first = dataArr[0];
                                if (first.Type == JTokenType.String)
                                {
                                    var s = first.Value<string>();
                                    if (!string.IsNullOrWhiteSpace(s)) return s;
                                }
                                else if (first is JObject fo)
                                {
                                    var nf = fo.Value<string>("url") ?? fo.Value<string>("path") ?? fo.Value<string>("filename");
                                    if (!string.IsNullOrEmpty(nf)) return nf;
                                }
                            }
                        }
                    }

                    var fileToken = root["file"];
                    if (fileToken != null)
                    {
                        if (fileToken.Type == JTokenType.String)
                        {
                            var s = fileToken.Value<string>();
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                        }
                        else if (fileToken is JObject fileObj)
                        {
                            var url = fileObj.Value<string>("url") ?? fileObj.Value<string>("path") ?? fileObj.Value<string>("fname") ?? fileObj.Value<string>("filename");
                            if (!string.IsNullOrEmpty(url)) return url;
                        }
                    }

                    var filesArr = root["files"] as JArray;
                    if (filesArr != null && filesArr.Count > 0)
                    {
                        var first = filesArr[0];
                        if (first.Type == JTokenType.String)
                        {
                            var s = first.Value<string>();
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                        }
                        else if (first is JObject fo)
                        {
                            var nf = fo.Value<string>("url") ?? fo.Value<string>("path") ?? fo.Value<string>("filename");
                            if (!string.IsNullOrEmpty(nf)) return nf;
                        }
                    }

                    throw new Exception($"Failed to parse upload response: no file path/url found. Raw response: {respBody}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse upload response: {ex.Message}. Raw response: {respBody}");
                }
            }
        }

        private async Task<(int totalCount, double totalFines)> UpdatePenaltyAndFinesAsync(int userId)
        {
            try
            {
                string url = $"{API_Backend.BaseUrl}/api/penalties/user/{userId}";
                using (var resp = await sharedHttpClient.GetAsync(url, _cts.Token))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        pendingPenalties_lbl.Text = "0";
                        fines_lbl.Text = "₱0.00";
                        return (0, 0.0);
                    }

                    string json = await resp.Content.ReadAsStringAsync();
                    var root = JObject.Parse(json);

                    bool success = root["success"]?.Value<bool>() ?? false;
                    if (!success)
                    {
                        pendingPenalties_lbl.Text = "0";
                        fines_lbl.Text = "₱0.00";
                        return (0, 0.0);
                    }

                    var data = root["data"];
                    double computedFines = 0.0;
                    int unpaidCount = 0;

                    var penaltiesArray = data?["penalties"] as JArray;
                    if (penaltiesArray != null)
                    {
                        foreach (var p in penaltiesArray)
                        {
                            string status = p["status"]?.Value<string>() ?? string.Empty;
                            if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            unpaidCount++;

                            var fineToken = p["fine"];
                            if (fineToken != null)
                            {
                                string fineStr = fineToken.ToString();
                                if (double.TryParse(fineStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedFine) ||
                                    double.TryParse(fineStr, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedFine))
                                {
                                    computedFines += parsedFine;
                                }
                                else
                                {
                                    if (int.TryParse(fineStr, out int parsedIntFine))
                                        computedFines += parsedIntFine;
                                }
                            }
                        }
                    }
                    else
                    {
                        unpaidCount = data?["total_count"]?.Value<int>() ?? 0;
                        computedFines = data?["total_fines"]?.Value<double>() ?? 0.0;
                    }

                    pendingPenalties_lbl.Text = unpaidCount.ToString();
                    fines_lbl.Text = $"₱{computedFines:N2}";

                    return (unpaidCount, computedFines);
                }
            }
            catch (OperationCanceledException)
            {
                return (0, 0.0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating penalties/fines: {ex}");
                pendingPenalties_lbl.Text = "0";
                fines_lbl.Text = "₱0.00";
                return (0, 0.0);
            }
        }

        private async Task<int> UpdateBooksCurrentlyBorrowedAsync(int userId)
        {
            try
            {
                string url = $"{API_Backend.BaseUrl}/api/transactions/user/{userId}";
                using (var resp = await sharedHttpClient.GetAsync(url, _cts.Token))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    string json = await resp.Content.ReadAsStringAsync();
                    var root = JObject.Parse(json);

                    bool success = root["success"]?.Value<bool>() ?? false;
                    if (!success)
                    {
                        booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    var data = root["data"] as JArray;
                    if (data == null)
                    {
                        booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    int borrowedItemsCount = 0;
                    foreach (var tx in data)
                    {
                        string status = tx["status"]?.Value<string>() ?? string.Empty;
                        if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Done", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        borrowedItemsCount++;
                    }

                    booksCurrentlyBorrowed_lbl.Text = borrowedItemsCount.ToString();
                    return borrowedItemsCount;
                }
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating books currently borrowed: {ex}");
                booksCurrentlyBorrowed_lbl.Text = "0";
                return 0;
            }
        }

        private void DisableActionsForPendingFines()
        {
            try
            {
                add_btn.Enabled = false;
                borrow_btn.Enabled = false;
                viewScannedBooks_btn.Enabled = false;
                if (receiptUCInstance != null)
                {
                    try { receiptUCInstance.Enabled = false; } catch { }
                }
            }
            catch { }
        }

        private void DisposeImagesInContainer(Control container)
        {
            if (container == null) return;

            var children = container.Controls.Cast<Control>().ToArray();
            foreach (var c in children)
            {
                try
                {
                    if (c is PictureBox pb)
                    {
                        var img = pb.Image;
                        pb.Image = null;
                        try { img?.Dispose(); } catch { }
                    }

                    if (c.HasChildren)
                        DisposeImagesInContainer(c);
                }
                catch { }
            }

            if (container is PictureBox rootPb)
            {
                var img = rootPb.Image;
                rootPb.Image = null;
                try { img?.Dispose(); } catch { }
            }
        }

        private void DisposeAndClearPanelChildren(Control container)
        {
            if (container == null) return;

            var children = container.Controls.Cast<Control>().ToArray();
            foreach (var ctrl in children)
            {
                try
                {
                    DisposeImagesInContainer(ctrl);
                    try { container.Controls.Remove(ctrl); } catch { }
                    try { ctrl.Dispose(); } catch { }
                }
                catch { }
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                try { _cts.Cancel(); } catch { }

                if (receiptUCInstance != null)
                {
                    try
                    {
                        generateReceipt_panel.Controls.Remove(receiptUCInstance);
                        receiptUCInstance.Dispose();
                    }
                    catch { }
                    finally { receiptUCInstance = null; }
                }

                try { DisposeAndClearPanelChildren(scannedType_panel); } catch { }
                try { DisposeAndClearPanelChildren(generateReceipt_panel); } catch { }

                try
                {
                    var img = profile_pictureBox.Image;
                    profile_pictureBox.Image = null;
                    try { img?.Dispose(); } catch { }
                }
                catch { }
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }
    }
}