using lib_track_kiosk.helpers;
using lib_track_kiosk.panel_forms;
using lib_track_kiosk.sub_forms;
using lib_track_kiosk.sub_user_controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Return : UserControl
    {
        private int? currentUserId;

        // STORAGE FOR SCANNED ITEMS (for return)
        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        // Last scan result properties (exposed so other code can read them)
        public string LastScannedType { get; private set; }
        public int? LastScannedBookId { get; private set; }
        public string LastScannedBookNumber { get; private set; }
        public int? LastScannedResearchPaperId { get; private set; }

        // System settings (fetched from server)
        private double _studentDailyFine = 5.00;
        private double _facultyDailyFine = 10.00;
        private int _studentBorrowDays = 3;
        private int _facultyBorrowDays = 5;

        // Current user type for applying fines/limits
        private string currentUserType = "Student";

        public UC_Return()
        {
            InitializeComponent();

            // Configure DataGridView so each column/cell supports multiline text wrapping
            SetupTransactionsGridAppearance();

            this.Load += UC_Return_Load;
        }

        // Configure transactions_DGV so cells wrap and rows auto-size for multiline content
        private void SetupTransactionsGridAppearance()
        {
            if (transactions_DGV == null)
                return;

            // Let the grid fill its parent container (avoid tiny grid with big empty space)
            transactions_DGV.Dock = DockStyle.Fill;
            transactions_DGV.Margin = new Padding(8);

            // Wrap long text in cells
            transactions_DGV.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            transactions_DGV.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            transactions_DGV.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Auto-size rows to fit wrapped content
            transactions_DGV.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            transactions_DGV.AllowUserToResizeRows = true;

            // Make columns fill available width to avoid a wide empty area to the right
            transactions_DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            transactions_DGV.AllowUserToResizeColumns = true;

            // Align wrapped text to top-left for nicer layout
            transactions_DGV.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            transactions_DGV.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            transactions_DGV.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Presentation tweaks
            transactions_DGV.EnableHeadersVisualStyles = false;
            transactions_DGV.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            transactions_DGV.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;

            // Hide row header to save horizontal space
            transactions_DGV.RowHeadersVisible = false;

            // Vertical scrollbar only (avoid large horizontal scrollbar)
            transactions_DGV.ScrollBars = ScrollBars.Vertical;

            // If columns already exist (designer) ensure wrap applied per column
            foreach (DataGridViewColumn col in transactions_DGV.Columns)
            {
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                // Keep AutoSizeMode = Fill to distribute space
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        // 🔹 Load event — open ScanFingerprint
        private async void UC_Return_Load(object sender, EventArgs e)
        {
            using (var scanFingerprint = new ScanFingerprint())
            {
                var result = scanFingerprint.ShowDialog();

                // ❌ Cancel → return to Welcome screen
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

                // ✅ Success → get user ID and load info
                if (result == DialogResult.OK)
                {
                    int? userId = scanFingerprint.ScannedUserId;
                    if (!userId.HasValue)
                    {
                        MessageBox.Show("⚠️ No valid user detected.");
                        return;
                    }

                    currentUserId = userId;
                    await FetchAndDisplayUserInformation(userId.Value);

                    // Load system settings (fines / borrow days)
                    await LoadSystemSettingsAsync();

                    // After loading user info, update penalties/fines and currently borrowed counts and transactions
                    // (Display-only for return UI — no disabling here)
                    var (penaltyCount, totalFines) = await UpdatePenaltyAndFinesAsync(userId.Value);
                    int borrowedCount = await UpdateBooksCurrentlyBorrowedAsync(userId.Value);

                    // Show borrowing limit (from settings)
                    try
                    {
                        string userType = currentUserType ?? "Student";
                        var (_, _, studentMaxBooks, facultyMaxBooks) = await lib_track_kiosk.configs.SystemSettingsFetcher.GetBorrowingLimitsAsync();
                        int maxBooks = userType.Equals("Student", StringComparison.OrdinalIgnoreCase) ? studentMaxBooks : facultyMaxBooks;
                        maxBooksThatCanBorrow_lbl.Text = maxBooks.ToString();
                    }
                    catch
                    {
                        maxBooksThatCanBorrow_lbl.Text = "N/A";
                    }

                    // After the counts are visible, load transaction rows
                    await LoadTransactionsForCurrentUserAsync();
                }
            }
        }

        // 🔹 Fetch user info from API
        private async Task FetchAndDisplayUserInformation(int userId)
        {
            try
            {
                // NOTE: UserFetcher.GetUserInfoAsync returns an 8-tuple including restriction flag.
                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto, isRestricted)
                    = await UserFetcher.GetUserInfoAsync(userId);

                // Example UI label updates (adjust based on your actual labels)
                fullName_lbl.Text = fullName;
                email_lbl.Text = email;
                contactNumber_lbl.Text = contactNumber;
                department_lbl.Text = department;
                position_lbl.Text = position;
                yearLevel_lbl.Text = yearLevel;

                // Save user type for fine calculations / borrow day logic
                currentUserType = string.IsNullOrWhiteSpace(position) ? "Student" : position;

                if (profilePhoto != null)
                {
                    profile_pictureBox.Image = profilePhoto;
                }
                else
                {
                    profile_pictureBox.ImageLocation = @"E:\Library-Tracker\lib-track-admin\public\avatar-default.png";
                }
                profile_pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error fetching user info: {ex.Message}");
            }
        }

        // Load system settings (borrow days and fine structure) from backend
        private async Task LoadSystemSettingsAsync()
        {
            try
            {
                var (studentBorrowDays, facultyBorrowDays, studentMaxBooks, facultyMaxBooks)
                    = await lib_track_kiosk.configs.SystemSettingsFetcher.GetBorrowingLimitsAsync();

                var (studentDailyFine, facultyDailyFine) =
                    await lib_track_kiosk.configs.SystemSettingsFetcher.GetFineStructureAsync();

                _studentBorrowDays = studentBorrowDays;
                _facultyBorrowDays = facultyBorrowDays;
                _studentDailyFine = studentDailyFine;
                _facultyDailyFine = facultyDailyFine;
            }
            catch (Exception ex)
            {
                // If fetching settings fails, fall back to defaults already set in fields
                Console.WriteLine("Warning: failed to load system settings: " + ex.Message);
            }
        }

        // 🔹 Exit button — go back to Welcome screen
        private void exitReturn_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Welcome welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
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
                    string.Equals(number.Trim(), bookNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // helper: check if research paper already scanned
        private bool IsResearchPaperAlreadyScanned(int researchPaperId)
        {
            return scannedResearchPapers.Contains(researchPaperId);
        }

        // PUBLIC METHOD: open scanner and return the scanned id/type (no panel required)
        public async Task<(string Type, int? Id)> ScanBookAndGetIdAsync()
        {
            using (var scanForm = new ScanBookQR())
            {
                var dialogResult = scanForm.ShowDialog();

                if (dialogResult != DialogResult.OK)
                {
                    LastScannedType = null;
                    LastScannedBookId = null;
                    LastScannedBookNumber = null;
                    LastScannedResearchPaperId = null;
                    return (null, null);
                }

                var scannedType = scanForm.ScannedType;
                if (string.Equals(scannedType, "Book", StringComparison.OrdinalIgnoreCase)
                    && scanForm.ScannedBookId.HasValue)
                {
                    LastScannedType = "Book";
                    LastScannedBookId = scanForm.ScannedBookId;
                    LastScannedBookNumber = scanForm.ScannedBookNumber;
                    LastScannedResearchPaperId = null;
                    return ("Book", LastScannedBookId);
                }
                else if (string.Equals(scannedType, "Research Paper", StringComparison.OrdinalIgnoreCase)
                         && scanForm.ScannedResearchPaperId.HasValue)
                {
                    LastScannedType = "Research Paper";
                    LastScannedResearchPaperId = scanForm.ScannedResearchPaperId;
                    LastScannedBookId = null;
                    LastScannedBookNumber = null;
                    return ("Research Paper", LastScannedResearchPaperId);
                }
                else
                {
                    LastScannedType = scannedType;
                    LastScannedBookId = null;
                    LastScannedBookNumber = null;
                    LastScannedResearchPaperId = null;
                    return (scannedType, null);
                }
            }
        }

        // SCAN BOOK BUTTON CLICK EVENT
        private async void scanBook_btn_Click(object sender, EventArgs e)
        {
            var (type, id) = await ScanBookAndGetIdAsync();

            if (string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Scan canceled or no valid item scanned.");
                return;
            }

            if (type == "Book")
            {
                MessageBox.Show($"Scanned Book ID: {id}");
                if (id.HasValue && !IsBookAlreadyScanned(id.Value, LastScannedBookNumber))
                {
                    scannedBooks.Add((id.Value, LastScannedBookNumber));
                }
            }
            else if (type == "Research Paper")
            {
                MessageBox.Show($"Scanned Research Paper ID: {id}");
                if (id.HasValue && !IsResearchPaperAlreadyScanned(id.Value))
                {
                    scannedResearchPapers.Add(id.Value);
                }
            }
            else
            {
                MessageBox.Show($"Unknown scan type: {type}");
            }

            await Task.CompletedTask;
        }

        // ---------- Load transactions for the current user and populate transactions_DGV ----------
        // This uses the endpoint GET /api/transactions/user/:user_id
        private async Task LoadTransactionsForCurrentUserAsync()
        {
            if (!currentUserId.HasValue)
                return;

            string url = $"{lib_track_kiosk.configs.API_Backend.BaseUrl}/api/transactions/user/{currentUserId.Value}";

            try
            {
                using (var client = new HttpClient())
                {
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"❌ Failed to fetch transactions: {resp.ReasonPhrase}");
                        return;
                    }

                    var content = await resp.Content.ReadAsStringAsync();
                    JObject root;
                    try
                    {
                        root = JObject.Parse(content);
                    }
                    catch
                    {
                        MessageBox.Show("❌ Invalid JSON received from transactions endpoint.");
                        return;
                    }

                    if (!root.TryGetValue("success", out var successToken) || successToken.Type != JTokenType.Boolean || !successToken.Value<bool>())
                    {
                        string msg = root.TryGetValue("message", out var m) ? m.ToString() : "Unknown error fetching transactions";
                        MessageBox.Show($"⚠️ Transactions API returned error: {msg}");
                        return;
                    }

                    var data = root["data"] as JArray;
                    if (data == null)
                    {
                        MessageBox.Show("⚠️ Transactions API returned no data.");
                        return;
                    }

                    // Build rows to add
                    var rows = new List<object[]>();

                    foreach (var t in data)
                    {
                        // Determine type
                        bool hasBook = t["book_id"] != null && t["book_id"].Type != JTokenType.Null;
                        bool hasResearch = t["research_paper_id"] != null && t["research_paper_id"].Type != JTokenType.Null;

                        string type = hasBook ? "Book" : hasResearch ? "Research Paper" : "Unknown";

                        // Title: prefer book_title then research_title then fallback
                        string title = t.Value<string>("book_title")
                                       ?? t.Value<string>("research_title")
                                       ?? t.Value<string>("title")
                                       ?? "";

                        // Authors: use the computed aliases from your updated route (book_authors, research_authors)
                        string authors = t.Value<string>("book_authors")
                                         ?? t.Value<string>("research_authors")
                                         ?? t.Value<string>("authors")
                                         ?? "";

                        // If authors still empty, attempt small fallbacks (e.g., user first + last)
                        if (string.IsNullOrWhiteSpace(authors))
                        {
                            var fn = t.Value<string>("first_name") ?? "";
                            var ln = t.Value<string>("last_name") ?? "";
                            var combined = (fn + " " + ln).Trim();
                            if (!string.IsNullOrEmpty(combined))
                                authors = combined;
                        }

                        // Reference number -> prefer transaction/reference_number first, then book_number
                        string referenceNumber = t["reference_number"]?.ToString()
                                                 ?? t["referenceNumber"]?.ToString()
                                                 ?? t["book_number"]?.ToString()
                                                 ?? t["bookNumber"]?.ToString()
                                                 ?? "";

                        // Transaction date & Due date
                        string transactionDate = "";
                        string dueDate = "";
                        DateTime? parsedDueDate = null;
                        DateTime? parsedTransactionDate = null;
                        var td = t["transaction_date"];
                        if (td != null && td.Type != JTokenType.Null)
                        {
                            if (DateTime.TryParse(td.ToString(), out var parsedTd))
                            {
                                parsedTransactionDate = parsedTd;
                                transactionDate = parsedTd.ToString("yyyy-MM-dd HH:mm");
                            }
                            else
                                transactionDate = td.ToString();
                        }
                        var dd = t["due_date"];
                        if (dd != null && dd.Type != JTokenType.Null)
                        {
                            if (DateTime.TryParse(dd.ToString(), out var parsedDd))
                            {
                                dueDate = parsedDd.ToString("yyyy-MM-dd");
                                parsedDueDate = parsedDd;
                            }
                            else
                                dueDate = dd.ToString();
                        }

                        // Base status from API (SQL returns a 'status' column already)
                        string apiStatus = t.Value<string>("status") ?? t.Value<string>("transaction_type") ?? "";

                        // Total fine — try several names; we'll override if we compute overdue fine
                        string totalFine = null;
                        if (t["total_fine"] != null && t["total_fine"].Type != JTokenType.Null)
                            totalFine = $"{t.Value<decimal?>("total_fine")?.ToString("N2") ?? "0.00"}";
                        else if (t["fine_amount"] != null && t["fine_amount"].Type != JTokenType.Null)
                            totalFine = $"{t.Value<decimal?>("fine_amount")?.ToString("N2") ?? "0.00"}";
                        else if (t["totalFine"] != null && t["totalFine"].Type != JTokenType.Null)
                            totalFine = $"{t.Value<decimal?>("totalFine")?.ToString("N2") ?? "0.00"}";

                        // Determine status based on dates and compute fine if overdue
                        string displayStatus = apiStatus;
                        try
                        {
                            var today = DateTime.Now.Date;

                            if (parsedDueDate.HasValue)
                            {
                                var due = parsedDueDate.Value.Date;

                                if (today == due)
                                {
                                    displayStatus = "Due Today!";
                                    // If already provided fine is empty, set 0.00
                                    if (string.IsNullOrEmpty(totalFine))
                                        totalFine = "0.00";
                                }
                                else if (today > due)
                                {
                                    int overdueDays = (today - due).Days;
                                    displayStatus = $"Overdue ({overdueDays} days)";

                                    // compute fine
                                    double dailyFine = currentUserType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                                        ? _studentDailyFine
                                        : _facultyDailyFine;

                                    double computed = overdueDays * dailyFine;
                                    totalFine = computed.ToString("N2");
                                }
                                else
                                {
                                    // today < due
                                    // if we have a transaction date, and today is within [transaction_date, due_date),
                                    // use API status (borrowed/reserved/etc.)
                                    if (parsedTransactionDate.HasValue)
                                    {
                                        var start = parsedTransactionDate.Value.Date;
                                        if (today >= start && today < due)
                                        {
                                            displayStatus = apiStatus;
                                            if (string.IsNullOrEmpty(totalFine))
                                                totalFine = "0.00";
                                        }
                                        else
                                        {
                                            // not within range -> still prefer API status
                                            displayStatus = apiStatus;
                                            if (string.IsNullOrEmpty(totalFine))
                                                totalFine = "0.00";
                                        }
                                    }
                                    else
                                    {
                                        // no transaction date — fall back to API status
                                        displayStatus = apiStatus;
                                        if (string.IsNullOrEmpty(totalFine))
                                            totalFine = "0.00";
                                    }
                                }
                            }
                            else
                            {
                                // no due date — just use API status
                                displayStatus = apiStatus;
                                if (string.IsNullOrEmpty(totalFine))
                                    totalFine = "0.00";
                            }
                        }
                        catch
                        {
                            // If anything fails while computing, fallback to API values
                            displayStatus = apiStatus;
                            if (string.IsNullOrEmpty(totalFine))
                                totalFine = "0.00";
                        }

                        // Ensure totalFine is not null
                        if (string.IsNullOrEmpty(totalFine))
                            totalFine = "0.00";

                        // Build row in the expected column order.
                        // Column1 == Type
                        // Column2 == Title
                        // Column3 == Authors
                        // Column4 == Reference Number
                        // Column5 == Transaction Date
                        // Column6 == Due Date
                        // Column7 == Status
                        // Column8 == Total Fine
                        rows.Add(new object[]
                        {
                            type,
                            title,
                            authors,
                            referenceNumber,
                            transactionDate,
                            dueDate,
                            displayStatus,
                            totalFine
                        });
                    }

                    if (transactions_DGV.InvokeRequired)
                    {
                        transactions_DGV.Invoke(new Action(() =>
                        {
                            PopulateTransactionsGrid(rows);
                        }));
                    }
                    else
                    {
                        PopulateTransactionsGrid(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error loading transactions: {ex.Message}");
            }
        }

        private void PopulateTransactionsGrid(List<object[]> rows)
        {
            try
            {
                transactions_DGV.SuspendLayout();
                transactions_DGV.Rows.Clear();

                // Ensure columns will wrap; if columns are added programmatically, set wrap here too
                foreach (DataGridViewColumn col in transactions_DGV.Columns)
                {
                    col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                foreach (var row in rows)
                {
                    int newIndex = transactions_DGV.Rows.Add();
                    var dgvRow = transactions_DGV.Rows[newIndex];

                    for (int i = 0; i < row.Length && i < transactions_DGV.Columns.Count; i++)
                    {
                        dgvRow.Cells[i].Value = row[i];
                        // ensure the cell style supports wrap
                        dgvRow.Cells[i].Style.WrapMode = DataGridViewTriState.True;
                        dgvRow.Cells[i].Style.Alignment = DataGridViewContentAlignment.TopLeft;
                    }
                }

                // After adding rows, auto-resize row heights to fit wrapped content
                transactions_DGV.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
                transactions_DGV.ResumeLayout();

                // Optional: set a reasonable minimum row height
                int minHeight = 22;
                foreach (DataGridViewRow r in transactions_DGV.Rows)
                {
                    if (r.Height < minHeight)
                        r.Height = minHeight;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error populating transactions grid: {ex.Message}");
            }
        }

        // Fetch penalties for the current user and update labels:
        // pendingPenalties_lbl => count of unpaid penalties
        // fines_lbl => sum of fines for unpaid penalties (computed from returned penalties array to avoid backend string-concat bugs)
        // Returns a tuple (unpaidCount, computedFines)
        private async Task<(int totalCount, double totalFines)> UpdatePenaltyAndFinesAsync(int userId)
        {
            try
            {
                string url = $"{lib_track_kiosk.configs.API_Backend.BaseUrl}/api/penalties/user/{userId}";
                using (HttpClient client = new HttpClient())
                {
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        // fallback defaults on failure
                        if (pendingPenalties_lbl != null) pendingPenalties_lbl.Text = "0";
                        if (fines_lbl != null) fines_lbl.Text = "₱0.00";
                        return (0, 0.0);
                    }

                    string json = await resp.Content.ReadAsStringAsync();
                    var root = JObject.Parse(json);

                    bool success = root["success"]?.Value<bool>() ?? false;
                    if (!success)
                    {
                        if (pendingPenalties_lbl != null) pendingPenalties_lbl.Text = "0";
                        if (fines_lbl != null) fines_lbl.Text = "₱0.00";
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
                                continue;

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
                                else if (int.TryParse(fineStr, out int parsedIntFine))
                                {
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

                    if (pendingPenalties_lbl != null) pendingPenalties_lbl.Text = unpaidCount.ToString();
                    if (fines_lbl != null) fines_lbl.Text = $"₱{computedFines:N2}";

                    return (unpaidCount, computedFines);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating penalties/fines: {ex}");
                if (pendingPenalties_lbl != null) pendingPenalties_lbl.Text = "0";
                if (fines_lbl != null) fines_lbl.Text = "₱0.00";
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
                string url = $"{lib_track_kiosk.configs.API_Backend.BaseUrl}/api/transactions/user/{userId}";
                using (HttpClient client = new HttpClient())
                {
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    string json = await resp.Content.ReadAsStringAsync();
                    var root = JObject.Parse(json);

                    bool success = root["success"]?.Value<bool>() ?? false;
                    if (!success)
                    {
                        if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    var data = root["data"] as JArray;
                    if (data == null)
                    {
                        if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = "0";
                        return 0;
                    }

                    int borrowedItemsCount = 0;
                    foreach (var tx in data)
                    {
                        string status = tx["status"]?.Value<string>() ?? string.Empty;

                        // Exclude transactions that are already returned or done.
                        if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Done", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        borrowedItemsCount++;
                    }

                    if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = borrowedItemsCount.ToString();
                    return borrowedItemsCount;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating books currently borrowed: {ex}");
                if (booksCurrentlyBorrowed_lbl != null) booksCurrentlyBorrowed_lbl.Text = "0";
                return 0;
            }
        }

        // Public refresh method so other code can force a reload after returns processed
        public async Task RefreshTransactionsAsync()
        {
            await LoadTransactionsForCurrentUserAsync();
        }
    }
}