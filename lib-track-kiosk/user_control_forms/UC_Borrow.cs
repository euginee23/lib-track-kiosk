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
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using System.Globalization;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Borrow : UserControl
    {
        // STORAGE FOR SCANNED ITEMS
        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        private int? currentUserId;

        private UC_GenerateReceipt receiptUCInstance;

        private static readonly HttpClient sharedHttpClient = new HttpClient();

        public UC_Borrow()
        {
            InitializeComponent();
            this.Load += UC_Borrow_Load;
        }

        // FORM LOAD - SCAN FINGERPRINT 
        private async void UC_Borrow_Load(object sender, EventArgs e)
        {
            using (var scanFingerprint = new ScanFingerprint())
            {
                var result = scanFingerprint.ShowDialog();

                if (result == DialogResult.Cancel)
                {
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
                        return;
                    }

                    await FetchAndDisplayUserInformation(userId.Value);
                }
            }
        }

        // FETCH USER INFORMATION FROM API
        private async Task FetchAndDisplayUserInformation(int userId)
        {
            try
            {
                currentUserId = userId;

                // Note: UserFetcher.GetUserInfoAsync now returns the restriction flag as the last element
                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto, isRestricted)
                    = await UserFetcher.GetUserInfoAsync(userId);

                fullName_lbl.Text = fullName;
                email_lbl.Text = email;
                contactNumber_lbl.Text = contactNumber;
                department_lbl.Text = department;
                position_lbl.Text = position;
                yearLevel_lbl.Text = yearLevel;

                // initialize defaults while we fetch more details
                pendingPenalties_lbl.Text = "0";
                fines_lbl.Text = "₱0.00";
                booksCurrentlyBorrowed_lbl.Text = "0";

                // DISPLAY PROFILE PHOTO
                if (profilePhoto != null)
                {
                    profile_pictureBox.Image = profilePhoto;
                }
                else
                {
                    // fallback default avatar path (update if different in your environment)
                    profile_pictureBox.ImageLocation = @"E:\Library-Tracker\lib-track-admin\public\avatar-default.png";
                }
                profile_pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

                // LOAD BORROWING LIMITS
                string userType = position ?? "Student";
                var (_, _, studentMaxBooks, facultyMaxBooks) =
                    await SystemSettingsFetcher.GetBorrowingLimitsAsync();

                int maxBooks = userType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                    ? studentMaxBooks
                    : facultyMaxBooks;

                maxBooksThatCanBorrow_lbl.Text = maxBooks.ToString();

                LoadReceiptGenerationPanel(userType, userId);

                // Update penalties/fines and currently borrowed counts
                var (penaltyCount, totalFines) = await UpdatePenaltyAndFinesAsync(userId);
                int borrowedCount = await UpdateBooksCurrentlyBorrowedAsync(userId);

                // If account is restricted, handle like penalties / max-limit: show message and restrict actions
                if (isRestricted)
                {
                    string message = $"⚠️ Your account is restricted and cannot borrow items at this time.\n\n" +
                                     "Please contact library staff to resolve the restriction.";

                    var dialogResult = MessageBox.Show(message, "Account Restricted", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        // Go back to welcome screen
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
                        // User clicked OK (wants to stay). Disable actions that would allow borrowing.
                        DisableActionsForPendingFines();
                    }
                }

                // If there are pending fines, notify user and require payment before borrowing
                if (penaltyCount > 0 || totalFines > 0.0)
                {
                    string finesFormatted = $"₱{totalFines:N2}";
                    string message = $"⚠️ You have pending penalties ({penaltyCount}) with total fines {finesFormatted}.\n\n" +
                                     "Please pay your fines before borrowing additional items.";

                    // Offer the user to go back to the Welcome screen (Cancel) or remain (OK).
                    // If they choose to stay, disable adding/borrowing until fines are resolved.
                    var dialogResult = MessageBox.Show(message, "Pending Fines", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        // Go back to welcome screen
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
                        // User clicked OK (wants to stay). Disable actions that would allow borrowing.
                        DisableActionsForPendingFines();
                    }
                }

                // Check if borrowed count reached or exceeded maximum allowed
                if (borrowedCount >= maxBooks)
                {
                    string message = $"⚠️ You have reached the maximum allowed borrowed items ({borrowedCount}/{maxBooks}).\n\n" +
                                     "You cannot borrow more items until some are returned or your limit is adjusted.";

                    var dialogResult = MessageBox.Show(message, "Maximum Borrowed Reached", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (dialogResult == DialogResult.Cancel)
                    {
                        // Go back to welcome screen
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
                        // disable borrowing actions as with pending fines
                        DisableActionsForPendingFines();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error fetching user info: {ex.Message}");
            }
        }

        // EXIT BORROW PANEL
        private void exitBorrow_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Welcome welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
            }
        }

        // LOAD RECEIPT GENERATION PANEL
        private void LoadReceiptGenerationPanel(string userType, int userId)
        {
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
                // make sure receipt has correct user id if it already exists
                try
                {
                    if (receiptUCInstance.UserId != userId)
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
                }
                catch
                {
                    // swallow - guard in case UC_GenerateReceipt doesn't expose UserId in some versions
                }
            }
        }

        // helper: check if a book is already scanned by id or book number
        private bool IsBookAlreadyScanned(int bookId, string bookNumber)
        {
            foreach (var (id, number) in scannedBooks)
            {
                if (id == bookId)
                    return true;
                if (!string.IsNullOrEmpty(bookNumber) && !string.IsNullOrEmpty(number) &&
                    string.Equals(number, bookNumber, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // helper: check if research paper already scanned
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

                    scannedType_panel.Controls.Clear();

                    if (scannedType == "Book" &&
                        scanForm.ScannedBookId.HasValue &&
                        !string.IsNullOrEmpty(scanForm.ScannedBookNumber))
                    {
                        int bookId = scanForm.ScannedBookId.Value;
                        string bookNumber = scanForm.ScannedBookNumber;

                        // Prevent adding duplicate book scans
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
                    // 📄 Research Paper
                    else if (scannedType == "Research Paper" &&
                             scanForm.ScannedResearchPaperId.HasValue)
                    {
                        int researchPaperId = scanForm.ScannedResearchPaperId.Value;

                        // Prevent duplicate research paper scans
                        if (IsResearchPaperAlreadyScanned(researchPaperId))
                        {
                            MessageBox.Show("⚠️ This research paper has already been scanned and won't be added again.");
                        }
                        else
                        {
                            scannedResearchPapers.Add(researchPaperId);

                            // Use the real UC_ScannedResearchInformation from sub_user_controls
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
                // fire and forget update — the receipt UC handles the lists asynchronously
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
            scannedType_panel.Controls.Clear();

            if (type == "Book")
            {
                var bookInfoUC = new UC_ScannedBookInformation(itemId, bookNumber);
                bookInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(bookInfoUC);

            }
            else if (type == "Research Paper")
            {
                // Use the real UC_ScannedResearchInformation control to show full details
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

            // BUILD BORROW REQUEST
            var borrowRequest = new BorrowRequest
            {
                ReferenceNumber = receiptUCInstance.ReferenceNumber,
                UserId = receiptUCInstance.UserId.Value,
                DueDate = receiptUCInstance.DueDate
            };

            // ADD SCANNED BOOKS
            foreach (var (bookId, _) in scannedBooks)
                borrowRequest.BookIds.Add(bookId);

            // ADD SCANNED RESEARCH PAPERS  
            borrowRequest.ResearchPaperIds.AddRange(scannedResearchPapers);

            // ATTACH RECEIPT IMAGE (one-step: send local file with /borrow)
            string tempReceiptPath = GenerateReceiptImageFromPanel();
            if (!string.IsNullOrEmpty(tempReceiptPath))
            {
                // Assign local temp file path so BorrowFetcher will attach it as file content.
                borrowRequest.ReceiptFilePath = tempReceiptPath;
            }

            try
            {
                var response = await BorrowFetcher.BorrowAsync(borrowRequest);

                if (response.Success)
                {
                    MessageBox.Show($"✅ Borrow successful!\nItems borrowed: {borrowRequest.BookIds.Count + borrowRequest.ResearchPaperIds.Count}\nReference: {borrowRequest.ReferenceNumber}");

                    // --- Show Post-Assessment Survey BEFORE returning to the welcome screen ---
                    try
                    {
                        using (var survey = new PostAssessmentSurvey())
                        {
                            // center on parent form if available
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
                        // If survey fails to open for any reason, log or show a message but continue the flow.
                        Console.Error.WriteLine("Failed to show PostAssessmentSurvey: " + ex);
                    }

                    // CLEAR SCANNED ITEMS
                    scannedBooks.Clear();
                    scannedResearchPapers.Clear();
                    scannedType_panel.Controls.Clear();

                    // UPDATE RECEIPT PANEL
                    _ = receiptUCInstance?.UpdateScannedItemsAsync(scannedBooks, scannedResearchPapers);

                    // CONFIRMATION - RETURN TO WELCOME SCREEN
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
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Unexpected error: {ex.Message}");
            }
            finally
            {
                // Clean up local temp file AFTER the borrow request completes so the file is available to be attached.
                try
                {
                    if (!string.IsNullOrEmpty(tempReceiptPath) && File.Exists(tempReceiptPath))
                        File.Delete(tempReceiptPath);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        // HELPER TO GENERATE RECEIPT IMAGE
        private string GenerateReceiptImageFromPanel()
        {
            if (generateReceipt_panel.Controls.Count == 0)
                return null;

            Bitmap bitmap = new Bitmap(generateReceipt_panel.Width, generateReceipt_panel.Height);
            generateReceipt_panel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            string tempPath = Path.Combine(Path.GetTempPath(), $"Receipt_{DateTime.Now:yyyyMMddHHmmss}.jpg");
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            return tempPath;
        }

        // Upload the local temp receipt image to the server upload endpoint and return the server path/url.
        // This version is simplified and more tolerant of different JSON shapes the upload endpoint might return.
        // It will try common locations for the returned path/url:
        // - top-level string
        // - root.data (string/object)
        // - root.file.url / root.file.path / root.file.fname
        // - root.data.file.url
        // - root.files[0] variants
        // If nothing is found the method throws a descriptive exception including the raw response.
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

                var resp = await sharedHttpClient.PostAsync(uploadUrl, content);
                var respBody = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    // Try to extract useful message but ultimately throw
                    string serverMsg = respBody;
                    try
                    {
                        var parsed = JObject.Parse(respBody);
                        serverMsg = parsed.Value<string>("message") ?? parsed.Value<string>("error") ?? respBody;
                    }
                    catch { /* ignore */ }

                    throw new Exception($"Upload failed ({(int)resp.StatusCode}): {serverMsg}");
                }

                // Defensive and simplified parsing:
                if (string.IsNullOrWhiteSpace(respBody))
                    throw new Exception("Upload returned an empty response body");

                try
                {
                    var root = JObject.Parse(respBody);

                    // 1) If top-level has "url" or "path" or "filePath" or "filename"
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
                                // common case: root.file = { url: "...", fname: "..." }
                                var obj = token as JObject;
                                var url = obj.Value<string>("url") ?? obj.Value<string>("path") ?? obj.Value<string>("fname") ?? obj.Value<string>("filename");
                                if (!string.IsNullOrEmpty(url)) return url;
                            }
                        }
                    }

                    // 2) Check root["data"]
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

                            // data could be an array
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

                    // 3) Check root["file"] directly (some upload endpoints return { file: { ... } })
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

                    // 4) Check root["files"] array
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

                    // Nothing matched - include raw response for diagnostics
                    throw new Exception($"Failed to parse upload response: no file path/url found. Raw response: {respBody}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse upload response: {ex.Message}. Raw response: {respBody}");
                }
            }
        }

        // Fetch penalties for the current user and update labels:
        // pendingPenalties_lbl => count of unpaid penalties
        // fines_lbl => sum of fines for unpaid penalties (computed from returned penalties array to avoid backend string-concat bugs)
        // Returns a tuple (unpaidCount, computedFines) so the caller can react (e.g., force payment)
        private async Task<(int totalCount, double totalFines)> UpdatePenaltyAndFinesAsync(int userId)
        {
            try
            {
                string url = $"{API_Backend.BaseUrl}/api/penalties/user/{userId}";
                using (HttpClient client = new HttpClient())
                {
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        // fallback defaults on failure
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
                    // Defensive: compute unpaid penalties count and fine sum from penalties array,
                    // ignoring entries where status == "Paid" (case-insensitive).
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
                                // skip paid penalties
                                continue;
                            }

                            // This penalty is considered unpaid
                            unpaidCount++;

                            var fineToken = p["fine"];
                            if (fineToken != null)
                            {
                                // Try to parse using invariant culture to handle decimal separators reliably.
                                string fineStr = fineToken.ToString();
                                if (double.TryParse(fineStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedFine) ||
                                    double.TryParse(fineStr, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedFine))
                                {
                                    computedFines += parsedFine;
                                }
                                else
                                {
                                    // try parsing ints as fallback
                                    if (int.TryParse(fineStr, out int parsedIntFine))
                                        computedFines += parsedIntFine;
                                }
                            }
                        }
                    }
                    else
                    {
                        // If penalties array missing, attempt to use total_count and total_fines,
                        // but we cannot determine paid status — assume server already excludes Paid in these fields.
                        unpaidCount = data?["total_count"]?.Value<int>() ?? 0;
                        computedFines = data?["total_fines"]?.Value<double>() ?? 0.0;
                    }

                    pendingPenalties_lbl.Text = unpaidCount.ToString();
                    fines_lbl.Text = $"₱{computedFines:N2}";

                    return (unpaidCount, computedFines);
                }
            }
            catch (Exception ex)
            {
                // on any error, default to zeros and log
                Console.Error.WriteLine($"Error updating penalties/fines: {ex}");
                pendingPenalties_lbl.Text = "0";
                fines_lbl.Text = "₱0.00";
                return (0, 0.0);
            }
        }

        // Fetch transactions for the current user and update booksCurrentlyBorrowed_lbl.
        // Count individual items that are considered still "borrowed".
        // Exclude those with status == "Returned" or status == "Done".
        // Returns the computed borrowed items count so caller can make decisions.
        private async Task<int> UpdateBooksCurrentlyBorrowedAsync(int userId)
        {
            try
            {
                string url = $"{API_Backend.BaseUrl}/api/transactions/user/{userId}";
                using (HttpClient client = new HttpClient())
                {
                    var resp = await client.GetAsync(url);
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

                        // Exclude transactions that are already returned or done.
                        // Treat missing status as still borrowed (conservative).
                        if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Done", StringComparison.OrdinalIgnoreCase))
                        {
                            // do not count
                            continue;
                        }

                        // Count this transaction as currently borrowed
                        borrowedItemsCount++;
                    }

                    booksCurrentlyBorrowed_lbl.Text = borrowedItemsCount.ToString();
                    return borrowedItemsCount;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating books currently borrowed: {ex}");
                booksCurrentlyBorrowed_lbl.Text = "0";
                return 0;
            }
        }

        // Disable actions that allow borrowing while user has pending fines or reached limits.
        private void DisableActionsForPendingFines()
        {
            try
            {
                // Common controls to disable to prevent further borrowing
                add_btn.Enabled = false;
                borrow_btn.Enabled = false;
                viewScannedBooks_btn.Enabled = false;
                // Optionally disable receipt generation interactions if they expose enable properties
                if (receiptUCInstance != null)
                {
                    try
                    {
                        receiptUCInstance.Enabled = false;
                    }
                    catch { /* ignore if not supported */ }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}