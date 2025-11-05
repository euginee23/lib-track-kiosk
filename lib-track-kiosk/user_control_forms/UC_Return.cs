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
using System.Linq;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Return : UserControl
    {
        private int? currentUserId;

        // Turn this to true while debugging to show raw API JSON after fetch
        private bool _debugShowRawApiResponse = false;

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

        // Fixed sized images (X and Check) used in the grid — single shared cache
        private Image _checkImageFixed;
        private Image _xImageFixed;
        private const int IndicatorImageSize = 20; // fixed size in px

        public UC_Return()
        {
            InitializeComponent();

            // Configure DataGridView so each column/cell supports multiline text wrapping
            SetupTransactionsGridAppearance();

            this.Load += UC_Return_Load;
        }

        // Configure transactions_DGV appearance and basic sizing
        private void SetupTransactionsGridAppearance()
        {
            if (transactions_DGV == null)
                return;

            transactions_DGV.Dock = DockStyle.Fill;
            transactions_DGV.Margin = new Padding(8);

            // Wrapping and row sizing
            transactions_DGV.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            transactions_DGV.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            transactions_DGV.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            transactions_DGV.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            transactions_DGV.AllowUserToResizeRows = true;

            // Use Fill for text columns but we will control fill weights; the indicator column will be a fixed width
            transactions_DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            transactions_DGV.AllowUserToResizeColumns = true;

            // Avoid "highlight everywhere" by keeping selection colors same as background
            transactions_DGV.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            transactions_DGV.MultiSelect = false;
            transactions_DGV.DefaultCellStyle.SelectionBackColor = transactions_DGV.DefaultCellStyle.BackColor;
            transactions_DGV.DefaultCellStyle.SelectionForeColor = transactions_DGV.DefaultCellStyle.ForeColor;
            transactions_DGV.RowTemplate.DefaultCellStyle.SelectionBackColor = transactions_DGV.RowTemplate.DefaultCellStyle.BackColor;
            transactions_DGV.RowTemplate.DefaultCellStyle.SelectionForeColor = transactions_DGV.RowTemplate.DefaultCellStyle.ForeColor;

            // header styles
            transactions_DGV.EnableHeadersVisualStyles = false;
            transactions_DGV.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            transactions_DGV.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
            transactions_DGV.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // hide row headers and vertical scroll only
            transactions_DGV.RowHeadersVisible = false;
            transactions_DGV.ScrollBars = ScrollBars.Vertical;

            // Small visual tweaks
            transactions_DGV.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            transactions_DGV.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            // Prepare fixed indicator images now (fixed size, centered later via cell alignment)
            _checkImageFixed = CreateCheckImage(IndicatorImageSize);
            _xImageFixed = CreateXImage(IndicatorImageSize);
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
                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto, isRestricted)
                    = await UserFetcher.GetUserInfoAsync(userId);

                fullName_lbl.Text = fullName;
                email_lbl.Text = email;
                contactNumber_lbl.Text = contactNumber;
                department_lbl.Text = department;
                position_lbl.Text = position;
                yearLevel_lbl.Text = yearLevel;

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
                if (id.HasValue && !IsBookAlreadyScanned(id.Value, LastScannedBookNumber))
                {
                    scannedBooks.Add((id.Value, LastScannedBookNumber));
                    // Mark matching transaction row as scanned (check the right-side column)
                    MarkTransactionRowAsScanned(id.Value, LastScannedBookNumber);
                }
            }
            else if (type == "Research Paper")
            {
                if (id.HasValue && !IsResearchPaperAlreadyScanned(id.Value))
                {
                    scannedResearchPapers.Add(id.Value);
                    // Mark research row if present
                    MarkTransactionRowAsScanned(null, null, researchPaperId: id.Value);
                }
            }
            else
            {
                MessageBox.Show($"Unknown scan type: {type}");
            }

            await Task.CompletedTask;
        }

        // ---------- Load transactions for the current user and populate transactions_DGV ----------
        // This uses the TransactionsFetcher helper to get the JArray and then maps to rows.
        // It also uses PenaltiesFetcher to show penalty status verbatim and uses the API 'status' field as authoritative when present.
        private async Task LoadTransactionsForCurrentUserAsync()
        {
            if (!currentUserId.HasValue)
                return;

            try
            {
                // Use TransactionsFetcher helper to get the JArray of transactions
                var data = await TransactionsFetcher.GetTransactionsForUserAsync(currentUserId.Value);

                // Optional: show raw response for debugging
                if (_debugShowRawApiResponse)
                {
                    try { ShowRawJsonDialog(data.ToString(Newtonsoft.Json.Formatting.Indented)); } catch { }
                }

                // Fetch penalties once and map ALL penalty statuses per transaction_id (if any)
                // Also build penaltyFinesByTransaction so we can prefer penalty amounts when transaction doesn't include total_fine.
                var penalties = await PenaltiesFetcher.GetPenaltiesForUserAsync(currentUserId.Value);
                var penaltiesByTransaction = new Dictionary<int, List<string>>();
                var penaltyFinesByTransaction = new Dictionary<int, double>();
                var latestPenaltyByTransaction = new Dictionary<int, JToken>(); // preserve for any "latest" status checks if needed

                if (penalties != null)
                {
                    foreach (var p in penalties.OfType<JObject>())
                    {
                        var txId = p.Value<int?>("transaction_id");
                        if (!txId.HasValue) continue;
                        var status = p.Value<string>("status") ?? string.Empty;

                        if (!penaltiesByTransaction.TryGetValue(txId.Value, out var list))
                        {
                            list = new List<string>();
                            penaltiesByTransaction[txId.Value] = list;
                        }
                        list.Add(status);

                        // accumulate fine value (best-effort parsing)
                        double fineVal = 0.0;
                        var fineToken = p["fine"];
                        if (fineToken != null && fineToken.Type != JTokenType.Null)
                        {
                            var fStr = fineToken.ToString();
                            if (!string.IsNullOrWhiteSpace(fStr))
                            {
                                if (double.TryParse(fStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var fv) ||
                                    double.TryParse(fStr, NumberStyles.Any, CultureInfo.CurrentCulture, out fv))
                                {
                                    fineVal = fv;
                                }
                                else if (int.TryParse(fStr, out var iv))
                                {
                                    fineVal = iv;
                                }
                            }
                        }

                        if (!penaltyFinesByTransaction.TryGetValue(txId.Value, out var acc))
                            penaltyFinesByTransaction[txId.Value] = fineVal;
                        else
                            penaltyFinesByTransaction[txId.Value] = acc + fineVal;

                        // maintain latestPenaltyByTransaction similar to original logic (by updated_at or id)
                        if (!latestPenaltyByTransaction.TryGetValue(txId.Value, out var existing))
                        {
                            latestPenaltyByTransaction[txId.Value] = p;
                        }
                        else
                        {
                            DateTime existingDt = DateTime.MinValue, newDt = DateTime.MinValue;
                            var exStr = existing["updated_at"]?.ToString();
                            var newStr = p["updated_at"]?.ToString();
                            DateTime.TryParse(exStr, out existingDt);
                            DateTime.TryParse(newStr, out newDt);

                            if (newDt > existingDt)
                                latestPenaltyByTransaction[txId.Value] = p;
                            else if (existingDt == DateTime.MinValue && p["penalty_id"] != null && existing["penalty_id"] != null)
                            {
                                var exId = existing["penalty_id"]?.Value<int?>() ?? 0;
                                var newId = p["penalty_id"]?.Value<int?>() ?? 0;
                                if (newId > exId) latestPenaltyByTransaction[txId.Value] = p;
                            }
                        }
                    }
                }

                if (data == null || data.Count == 0)
                {
                    MessageBox.Show("⚠️ No transactions found for this user.");
                    return;
                }

                // Build rows to add.
                var rows = new List<(object[] Cells, int? bookId, int? researchId, string referenceNumber)>();

                foreach (var t in data)
                {
                    bool hasBook = t["book_id"] != null && t["book_id"].Type != JTokenType.Null;
                    bool hasResearch = t["research_paper_id"] != null && t["research_paper_id"].Type != JTokenType.Null;

                    string type = hasBook ? "Book" : hasResearch ? "Research Paper" : "Unknown";

                    string title = t.Value<string>("book_title")
                                   ?? t.Value<string>("research_title")
                                   ?? t.Value<string>("title")
                                   ?? "";

                    string authors = t.Value<string>("book_authors")
                                     ?? t.Value<string>("research_authors")
                                     ?? t.Value<string>("authors")
                                     ?? "";

                    if (string.IsNullOrWhiteSpace(authors))
                    {
                        var fn = t.Value<string>("first_name") ?? "";
                        var ln = t.Value<string>("last_name") ?? "";
                        var combined = (fn + " " + ln).Trim();
                        if (!string.IsNullOrEmpty(combined))
                            authors = combined;
                    }

                    string referenceNumber = t["reference_number"]?.ToString()
                                             ?? t["referenceNumber"]?.ToString()
                                             ?? t["book_number"]?.ToString()
                                             ?? t["bookNumber"]?.ToString()
                                             ?? "";

                    DateTime? parsedDueDate = null;
                    DateTime? parsedTransactionDate = null;
                    string transactionDate = "";
                    string dueDate = "";

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

                    // Read API-provided status fields - prefer normalized "status" (TransactionsFetcher already normalizes) but accept alternatives
                    string apiStatusCandidate1 = t.Value<string>("status") ?? "";
                    string apiStatusCandidate2 = t.Value<string>("transaction_status") ?? "";
                    string apiType = t.Value<string>("transaction_type") ?? "";
                    string apiStatusRaw = (!string.IsNullOrWhiteSpace(apiStatusCandidate1) ? apiStatusCandidate1 : (!string.IsNullOrWhiteSpace(apiStatusCandidate2) ? apiStatusCandidate2 : apiType ?? "")).Trim();

                    // ---------- TOTAL FINE: prefer backend value, otherwise sum penalties for this transaction, otherwise compute overdue fine ----------
                    double txFineValue = 0.0;
                    bool backendFineFound = false;

                    // prefer server-provided fields if present
                    var totalFineToken = t["total_fine"] ?? t["fine_amount"] ?? t["totalFine"];
                    if (totalFineToken != null && totalFineToken.Type != JTokenType.Null)
                    {
                        var tfStr = totalFineToken.ToString();
                        if (!string.IsNullOrWhiteSpace(tfStr))
                        {
                            if (double.TryParse(tfStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedTf) ||
                                double.TryParse(tfStr, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedTf))
                            {
                                txFineValue = parsedTf;
                                backendFineFound = true;
                            }
                        }
                    }

                    if (!backendFineFound)
                    {
                        // try penalty sum for this transaction
                        int txIdForFine = t.Value<int?>("transaction_id") ?? t.Value<int?>("id") ?? 0;
                        if (txIdForFine != 0 && penaltyFinesByTransaction.TryGetValue(txIdForFine, out var penaltySum) && penaltySum > 0)
                        {
                            txFineValue = penaltySum;
                        }
                        else
                        {
                            // fallback: compute overdue fine from due_date if overdue
                            if (parsedDueDate.HasValue)
                            {
                                var today = DateTime.Now.Date;
                                var due = parsedDueDate.Value.Date;
                                if (today > due)
                                {
                                    int overdueDays = (today - due).Days;
                                    double dailyFine = currentUserType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                                        ? _studentDailyFine
                                        : _facultyDailyFine;
                                    txFineValue = overdueDays * dailyFine;
                                }
                                else
                                {
                                    txFineValue = 0.0;
                                }
                            }
                            else
                            {
                                txFineValue = 0.0;
                            }
                        }
                    }

                    string totalFine = txFineValue.ToString("N2"); // format like "0.00" or "35.00"

                    // --- SIMPLE COMBINED DISPLAY (transaction status verbatim + penalty statuses verbatim) ---
                    // Format: "{transaction_status}  ·  {penalty_statuses|No Penalty}"
                    string displayStatus = GetTransactionAndPenaltyDisplayStatus((JObject)t, penaltiesByTransaction);

                    // Visible cells in the grid (8 columns expected by the existing UI)
                    var visibleCells = new object[]
                    {
                        type,
                        title,
                        authors,
                        referenceNumber,
                        transactionDate,
                        dueDate,
                        displayStatus,
                        totalFine
                    };

                    int? bookId = hasBook ? (int?)t.Value<int?>("book_id") : null;
                    int? researchId = hasResearch ? (int?)t.Value<int?>("research_paper_id") : null;

                    rows.Add((visibleCells, bookId, researchId, referenceNumber));
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
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error loading transactions: {ex.Message}");
            }
        }

        /// <summary>
        /// Populates the transactions DataGridView.
        /// Creates columns with proper FillWeight and a small image column for scanned indicator that fits the grid.
        /// </summary>
        private void PopulateTransactionsGrid(List<(object[] Cells, int? bookId, int? researchId, string referenceNumber)> rows)
        {
            try
            {
                transactions_DGV.SuspendLayout();
                transactions_DGV.Rows.Clear();

                // Recreate columns to ensure consistent column order and sizing.
                SetupColumnsForTransactionsGrid();

                // Add rows and populate hidden id columns
                foreach (var r in rows)
                {
                    int newIndex = transactions_DGV.Rows.Add();
                    var dgvRow = transactions_DGV.Rows[newIndex];

                    // assign visible values by column name to avoid position issues
                    SetCellValueSafe(dgvRow, "Type", r.Cells.ElementAtOrDefault(0));
                    SetCellValueSafe(dgvRow, "Title", r.Cells.ElementAtOrDefault(1));
                    SetCellValueSafe(dgvRow, "Authors", r.Cells.ElementAtOrDefault(2));
                    SetCellValueSafe(dgvRow, "ReferenceNumber", r.Cells.ElementAtOrDefault(3));
                    SetCellValueSafe(dgvRow, "TransactionDate", r.Cells.ElementAtOrDefault(4));
                    SetCellValueSafe(dgvRow, "DueDate", r.Cells.ElementAtOrDefault(5));
                    SetCellValueSafe(dgvRow, "Status", r.Cells.ElementAtOrDefault(6));
                    SetCellValueSafe(dgvRow, "TotalFine", r.Cells.ElementAtOrDefault(7));

                    // default unscanned indicator = X (fixed size)
                    dgvRow.Cells["Scanned"].Value = _xImageFixed;

                    // Put ids into hidden columns
                    dgvRow.Cells["book_id_hidden"].Value = r.bookId.HasValue ? r.bookId.Value.ToString() : "";
                    dgvRow.Cells["research_id_hidden"].Value = r.researchId.HasValue ? r.researchId.Value.ToString() : "";

                    // If this row corresponds to any already scanned book, mark it checked — but only if the transaction row is active.
                    // We avoid marking returned/done/reserved transactions.
                    string rowStatusString = r.Cells.ElementAtOrDefault(6)?.ToString() ?? "";

                    if (r.bookId.HasValue && scannedBooks.Any(s => s.bookId == r.bookId.Value))
                    {
                        if (IsStatusActiveString(rowStatusString))
                        {
                            dgvRow.Cells["Scanned"].Value = _checkImageFixed;
                        }
                    }
                    else if (!string.IsNullOrEmpty(r.referenceNumber) && scannedBooks.Any(s => !string.IsNullOrEmpty(s.bookNumber) && string.Equals(s.bookNumber.Trim(), r.referenceNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (IsStatusActiveString(rowStatusString))
                        {
                            dgvRow.Cells["Scanned"].Value = _checkImageFixed;
                        }
                    }
                    else if (r.researchId.HasValue && scannedResearchPapers.Contains(r.researchId.Value))
                    {
                        if (IsStatusActiveString(rowStatusString))
                        {
                            dgvRow.Cells["Scanned"].Value = _checkImageFixed;
                        }
                    }
                }

                // After adding rows, auto-resize row heights to fit wrapped content
                transactions_DGV.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

                // Ensure the indicator column retains fixed width and the image stays centered
                if (transactions_DGV.Columns.Contains("Scanned"))
                {
                    var col = transactions_DGV.Columns["Scanned"];
                    col.Width = IndicatorImageSize + 12; // padding
                    col.MinimumWidth = IndicatorImageSize + 8;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                transactions_DGV.ResumeLayout();

                // Optional: set a reasonable minimum row height
                int minHeight = 28;
                foreach (DataGridViewRow rr in transactions_DGV.Rows)
                {
                    if (rr.Height < minHeight)
                        rr.Height = minHeight;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error populating transactions grid: {ex.Message}");
            }
        }

        // Ensure grid has the desired columns and fill weights so Scanned icon remains visible.
        private void SetupColumnsForTransactionsGrid()
        {
            // Recreate columns to ensure consistent layout
            transactions_DGV.Columns.Clear();

            // Type
            var typeCol = new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 8 };
            transactions_DGV.Columns.Add(typeCol);

            // Title - give most space
            var titleCol = new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 };
            transactions_DGV.Columns.Add(titleCol);

            // Authors
            var authorsCol = new DataGridViewTextBoxColumn { Name = "Authors", HeaderText = "Author(s)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 18 };
            transactions_DGV.Columns.Add(authorsCol);

            // Reference Number
            var refCol = new DataGridViewTextBoxColumn { Name = "ReferenceNumber", HeaderText = "Reference Number", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12 };
            transactions_DGV.Columns.Add(refCol);

            // Transaction Date
            var txDateCol = new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Transaction Date", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10 };
            transactions_DGV.Columns.Add(txDateCol);

            // Due Date
            var dueDateCol = new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "Due Date", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10 };
            transactions_DGV.Columns.Add(dueDateCol);

            // Status
            var statusCol = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10 };
            transactions_DGV.Columns.Add(statusCol);

            // Total Fine
            var fineCol = new DataGridViewTextBoxColumn { Name = "TotalFine", HeaderText = "Total Fine", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 6 };
            transactions_DGV.Columns.Add(fineCol);

            // Scanned image column (small fixed width)
            var imgCol = new DataGridViewImageColumn
            {
                Name = "Scanned",
                HeaderText = "",
                ImageLayout = DataGridViewImageCellLayout.Normal,
                ReadOnly = true
            };
            transactions_DGV.Columns.Add(imgCol);

            // Hidden id columns for mapping (not visible)
            var idCol = new DataGridViewTextBoxColumn { Name = "book_id_hidden", HeaderText = "book_id_hidden", Visible = false };
            transactions_DGV.Columns.Add(idCol);
            var rCol = new DataGridViewTextBoxColumn { Name = "research_id_hidden", HeaderText = "research_id_hidden", Visible = false };
            transactions_DGV.Columns.Add(rCol);

            // Column styles
            foreach (DataGridViewColumn col in transactions_DGV.Columns)
            {
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            }

            // Make sure the Scanned column alignment is center so our fixed image is centered
            try
            {
                transactions_DGV.Columns["Scanned"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                transactions_DGV.Columns["Scanned"].Width = IndicatorImageSize + 12;
                transactions_DGV.Columns["Scanned"].MinimumWidth = IndicatorImageSize + 8;
                transactions_DGV.Columns["Scanned"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
            catch { }
        }

        // Safe helper to set a cell value by column name if present
        private void SetCellValueSafe(DataGridViewRow row, string columnName, object value)
        {
            if (row == null || row.DataGridView == null) return;
            if (!row.DataGridView.Columns.Contains(columnName)) return;
            row.Cells[columnName].Value = value;
            // Ensure wrap for text columns
            row.Cells[columnName].Style.WrapMode = DataGridViewTriState.True;
            row.Cells[columnName].Style.Alignment = DataGridViewContentAlignment.TopLeft;
        }

        // Mark a transaction row as scanned by matching bookId, bookNumber or researchPaperId
        // This only sets the image in the Scanned cell (no row background changes)
        private void MarkTransactionRowAsScanned(int? bookId, string bookNumber = null, int? researchPaperId = null)
        {
            try
            {
                if (transactions_DGV.InvokeRequired)
                {
                    transactions_DGV.Invoke(new Action(() => MarkTransactionRowAsScanned(bookId, bookNumber, researchPaperId)));
                    return;
                }

                if (!transactions_DGV.Columns.Contains("Scanned"))
                    return;

                var candidates = new List<DataGridViewRow>();

                foreach (DataGridViewRow row in transactions_DGV.Rows)
                {
                    // match by hidden book id
                    if (bookId.HasValue && transactions_DGV.Columns.Contains("book_id_hidden"))
                    {
                        var cellVal = row.Cells["book_id_hidden"].Value?.ToString();
                        if (!string.IsNullOrEmpty(cellVal) && int.TryParse(cellVal, out int bid) && bid == bookId.Value)
                        {
                            candidates.Add(row);
                            continue;
                        }
                    }

                    // match by hidden research id
                    if (researchPaperId.HasValue && transactions_DGV.Columns.Contains("research_id_hidden"))
                    {
                        var cellVal = row.Cells["research_id_hidden"].Value?.ToString();
                        if (!string.IsNullOrEmpty(cellVal) && int.TryParse(cellVal, out int rid) && rid == researchPaperId.Value)
                        {
                            candidates.Add(row);
                            continue;
                        }
                    }

                    // match by reference/book number cell (visible column ReferenceNumber)
                    if (!string.IsNullOrEmpty(bookNumber) && transactions_DGV.Columns.Contains("ReferenceNumber"))
                    {
                        var rv = row.Cells["ReferenceNumber"].Value?.ToString();
                        if (!string.IsNullOrEmpty(rv) && string.Equals(rv.Trim(), bookNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            candidates.Add(row);
                            continue;
                        }
                    }
                }

                if (candidates.Count == 0)
                    return;

                // Prefer an active transaction row (not Returned / Done / Reserved). If none active, fall back to first candidate.
                var chosen = candidates.FirstOrDefault(r => IsRowActiveTransaction(r)) ?? candidates.First();

                chosen.Cells["Scanned"].Value = _checkImageFixed;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error marking transaction row as scanned: {ex}");
            }
        }

        // PopulateTransactionsGrid helper (keeps previous behaviour)
        private void PopulateTransactionsGrid(List<object[]> rows)
        {
            // Keep this overload to avoid breaking existing calls if any external code uses it.
            // It converts to the new expected tuple format with empty ids.
            var converted = rows.Select(r => (Cells: r, bookId: (int?)null, researchId: (int?)null, referenceNumber: r.Length > 3 ? r[3]?.ToString() ?? "" : "")).ToList();
            PopulateTransactionsGrid(converted);
        }

        // -------- image creation utilities (fixed-size assets) --------

        // Create fixed-size check icon (green circle + white check)
        private Image CreateCheckImage(int px)
        {
            try
            {
                int s = Math.Max(IndicatorImageSize, px);
                var bmp = new Bitmap(IndicatorImageSize, IndicatorImageSize);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (var brush = new SolidBrush(Color.FromArgb(40, 167, 69)))
                    {
                        g.FillEllipse(brush, 0, 0, IndicatorImageSize - 1, IndicatorImageSize - 1);
                    }

                    using (var pen = new Pen(Color.White, 2f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        var p1 = new PointF(IndicatorImageSize * 0.22f, IndicatorImageSize * 0.55f);
                        var p2 = new PointF(IndicatorImageSize * 0.45f, IndicatorImageSize * 0.78f);
                        var p3 = new PointF(IndicatorImageSize * 0.82f, IndicatorImageSize * 0.28f);
                        g.DrawLines(pen, new[] { p1, p2, p3 });
                    }
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        // Create fixed-size X icon (gray box with X)
        private Image CreateXImage(int px)
        {
            try
            {
                var bmp = new Bitmap(IndicatorImageSize, IndicatorImageSize);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // small light gray square background with border
                    using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    {
                        g.FillRectangle(brush, 0, 0, IndicatorImageSize - 1, IndicatorImageSize - 1);
                    }
                    using (var pen = new Pen(Color.FromArgb(180, 180, 180), 1))
                    {
                        g.DrawRectangle(pen, 0, 0, IndicatorImageSize - 1, IndicatorImageSize - 1);
                    }

                    // draw X
                    using (var pen = new Pen(Color.FromArgb(120, 120, 120), 2f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, 4, 4, IndicatorImageSize - 5, IndicatorImageSize - 5);
                        g.DrawLine(pen, IndicatorImageSize - 5, 4, 4, IndicatorImageSize - 5);
                    }
                }
                return bmp;
            }
            catch
            {
                return null;
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

        private async void return_btn_Click(object sender, EventArgs e)
        {
            // Basic validations
            if (!currentUserId.HasValue)
            {
                MessageBox.Show("No user loaded. Please scan user fingerprint first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure there is at least one scanned item
            if (!scannedBooks.Any() && !scannedResearchPapers.Any())
            {
                MessageBox.Show("No scanned items to return. Please scan book(s) or research paper(s) before returning.", "Nothing scanned", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Attempt to determine reference number from the grid.
            string referenceNumber = GetReferenceNumberFromGrid();
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                MessageBox.Show("Reference number not found in transactions. Please select or load the correct transactions reference.", "Missing reference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm action with the user
            string confirmMsg = $"You are about to return {scannedBooks.Count} book(s) and {scannedResearchPapers.Count} research paper(s) under reference '{referenceNumber}'.\n\nProceed?";
            var confirm = MessageBox.Show(confirmMsg, "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // Disable return button while processing
            try
            {
                return_btn.Enabled = false;

                // Prepare ids
                var bookIds = scannedBooks.Select(s => s.bookId).ToList();
                var researchIds = scannedResearchPapers.ToList();

                // Call helper to post to server
                var result = await ReturnBookResearch.ReturnAsync(referenceNumber, currentUserId.Value, bookIds, researchIds);

                if (result == null)
                {
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (result.Success)
                {
                    // Show success summary
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine(result.Message ?? "Return completed.");
                    sb.AppendLine($"Reference: {result.ReferenceNumber}");
                    sb.AppendLine($"Returned items: {result.TotalReturned}");
                    if (result.ReturnedItems?.Any() == true)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Returned items:");
                        foreach (var it in result.ReturnedItems)
                        {
                            try
                            {
                                sb.AppendLine($" - [{GetObjectStringProp(it, "ItemType", "item_type", "type", "Item_Type")}] {GetObjectStringProp(it, "ItemTitle", "item_title", "title") ?? ""} (ID: {GetObjectIntProp(it, "ItemId", "item_id", "id")})");
                            }
                            catch
                            {
                                sb.AppendLine($" - {it.ToString()}");
                            }
                        }
                    }

                    MessageBox.Show(sb.ToString(), "Return Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // --- Open Rate dialog for each returned item (if any) BEFORE PostAssessmentSurvey ---
                    try
                    {
                        // If server returned explicit ReturnedItems array, iterate that and rate each element.
                        if (result.ReturnedItems != null && result.ReturnedItems.Any())
                        {
                            foreach (var it in result.ReturnedItems)
                            {
                                try
                                {
                                    var itemType = GetObjectStringProp(it, "ItemType", "item_type", "type", "itemType");
                                    var itemTitle = GetObjectStringProp(it, "ItemTitle", "item_title", "title", "itemTitle");
                                    var itemId = GetObjectIntProp(it, "ItemId", "item_id", "id");

                                    if (itemId.HasValue && !string.IsNullOrWhiteSpace(itemType) && currentUserId.HasValue)
                                    {
                                        // Show Rate dialog for this item (modal, sequential)
                                        using (var rate = new Rate(currentUserId.Value, itemType, itemId, itemTitle))
                                        {
                                            var owner = this.FindForm();
                                            rate.StartPosition = FormStartPosition.CenterParent;
                                            if (owner != null)
                                                rate.ShowDialog(owner);
                                            else
                                                rate.ShowDialog();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // If one item fails, log and continue with other items
                                    Console.Error.WriteLine("Failed to show Rate dialog for one returned item: " + ex);
                                }
                            }
                        }
                        else
                        {
                            // Fallback: if no ReturnedItems in server response, rate scanned items that were returned.
                            // Rate every scanned research paper
                            foreach (var rid in scannedResearchPapers.ToList())
                            {
                                try
                                {
                                    if (currentUserId.HasValue)
                                    {
                                        using (var rate = new Rate(currentUserId.Value, "Research Paper", rid, null))
                                        {
                                            var owner = this.FindForm();
                                            rate.StartPosition = FormStartPosition.CenterParent;
                                            if (owner != null)
                                                rate.ShowDialog(owner);
                                            else
                                                rate.ShowDialog();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine("Failed to show Rate dialog for scanned research paper: " + ex);
                                }
                            }

                            // Rate every scanned book
                            foreach (var b in scannedBooks.ToList())
                            {
                                try
                                {
                                    if (currentUserId.HasValue)
                                    {
                                        using (var rate = new Rate(currentUserId.Value, "Book", b.bookId, b.bookNumber))
                                        {
                                            var owner = this.FindForm();
                                            rate.StartPosition = FormStartPosition.CenterParent;
                                            if (owner != null)
                                                rate.ShowDialog(owner);
                                            else
                                                rate.ShowDialog();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine("Failed to show Rate dialog for scanned book: " + ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // If rating dialogs fail for any reason, log but continue to survey and navigation.
                        Console.Error.WriteLine("Failed to show rating dialogs: " + ex);
                    }

                    // --- Show PostAssessmentSurvey last ---
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
                        // If survey fails to open for any reason, log but continue the flow.
                        Console.Error.WriteLine("Failed to show PostAssessmentSurvey: " + ex);
                    }

                    // Clear scanned lists
                    scannedBooks.Clear();
                    scannedResearchPapers.Clear();

                    // Refresh transactions and penalty/fine counts (best-effort — safe to call)
                    await RefreshTransactionsAsync();
                    await UpdatePenaltyAndFinesAsync(currentUserId.Value);
                    await UpdateBooksCurrentlyBorrowedAsync(currentUserId.Value);

                    // AFTER SUCCESS: go back to Welcome screen
                    try
                    {
                        MainForm mainForm = (MainForm)this.ParentForm;
                        if (mainForm != null)
                        {
                            UC_Welcome welcomeScreen = new UC_Welcome();
                            mainForm.addUserControl(welcomeScreen);
                            return; // stop further processing in this handler
                        }
                    }
                    catch
                    {
                        // ignore navigation errors — we've already completed the return
                    }
                }
                else
                {
                    // Non-success: try to show helpful info
                    if (result.ExpectedItems != null || result.ProvidedItems != null)
                    {
                        var expected = result.ExpectedItems;
                        var provided = result.ProvidedItems;

                        string msg = result.Message ?? "Server rejected the return request.";
                        msg += Environment.NewLine + Environment.NewLine;
                        msg += "Expected items (must be returned together):" + Environment.NewLine;
                        if (expected != null)
                        {
                            try
                            {
                                msg += FormatItemsFromToken(expected);
                            }
                            catch { msg += expected.ToString(); }
                        }

                        msg += Environment.NewLine + "Provided items:" + Environment.NewLine;
                        if (provided != null)
                        {
                            try
                            {
                                msg += FormatItemsFromToken(provided);
                            }
                            catch { msg += provided.ToString(); }
                        }

                        MessageBox.Show(msg, "Incomplete / Incorrect Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (result.HttpStatusCode == 402 && result.PenaltyChecks != null)
                    {
                        // Unpaid penalties
                        string msg = "Cannot return items due to unpaid penalties. Details:" + Environment.NewLine;
                        try
                        {
                            var unpaid = result.PenaltyChecks;
                            if (unpaid is JArray arr)
                            {
                                foreach (var p in arr)
                                {
                                    msg += $"- Transaction {p["transaction_id"]?.ToString()}: fine {p["fine"]?.ToString()} (status: {p["status"]?.ToString()})" + Environment.NewLine;
                                }
                            }
                            else
                            {
                                msg += unpaid.ToString();
                            }
                        }
                        catch
                        {
                            msg += "See server response for details.";
                        }
                        MessageBox.Show(msg, "Unpaid Penalties", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        // Generic error
                        string err = result.Message ?? "Return failed.";
                        MessageBox.Show($"{err}\n\nServer response code: {result.HttpStatusCode}", "Return Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        // If ExpectedItems exist but were not parsed into result fields, try to show raw JSON
                        if (!string.IsNullOrEmpty(result.RawJson))
                        {
                            // Offer to show more details
                            var viewDetails = MessageBox.Show("Show server response for debugging?", "Details", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (viewDetails == DialogResult.Yes)
                            {
                                ShowRawJsonDialog(result.RawJson);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while returning items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                return_btn.Enabled = true;
            }
        }

        // Helper: pick a reference number for the current set of loaded transactions
        private string GetReferenceNumberFromGrid()
        {
            try
            {
                if (transactions_DGV == null || transactions_DGV.Rows.Count == 0) return null;

                // Prefer first non-empty ReferenceNumber cell
                foreach (DataGridViewRow r in transactions_DGV.Rows)
                {
                    var v = r.Cells["ReferenceNumber"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(v)) return v.Trim();
                }

                // fallback to cell at index 3 (old layout)
                if (transactions_DGV.Columns.Count > 3)
                {
                    var v = transactions_DGV.Rows[0].Cells[3]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(v)) return v.Trim();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Helper: Format expected/provided items token into readable string
        private string FormatItemsFromToken(JToken token)
        {
            if (token == null) return "";
            var sb = new System.Text.StringBuilder();

            if (token is JArray arr)
            {
                foreach (var it in arr)
                {
                    var type = it["type"]?.ToString() ?? it["item_type"]?.ToString() ?? "item";
                    var id = it["id"]?.ToString() ?? it["item_id"]?.ToString() ?? "";
                    sb.AppendLine($" - {type}: {id}");
                }
            }
            else if (token is JObject obj)
            {
                foreach (var prop in obj)
                {
                    sb.AppendLine($"{prop.Key}: {prop.Value}");
                }
            }
            else
            {
                sb.AppendLine(token.ToString());
            }

            return sb.ToString();
        }

        // Helper: show raw JSON response in a simple dialog for debugging
        private void ShowRawJsonDialog(string json)
        {
            try
            {
                var dlg = new Form()
                {
                    Text = "Server response",
                    Width = 900,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterParent
                };
                var tb = new TextBox()
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10f),
                    Text = json
                };
                dlg.Controls.Add(tb);
                dlg.ShowDialog();
            }
            catch { /* ignore errors showing dialog */ }
        }

        // Helper: decide if a status string indicates an active (borrowed) transaction.
        // Treat statuses containing "return", "returned", "done", "reserve" as not-active.
        private bool IsStatusActiveString(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return true; // assume active if unknown (safe default)

            string s = status.ToLowerInvariant();
            if (s.Contains("return") || s.Contains("returned") || s.Contains("done") || s.Contains("reserve") || s.Contains("reserved"))
                return false;

            // For statuses like "overdue" or "due today" or "active" or "pending payment" -> treat as active
            return true;
        }

        // Helper: decide if a DataGridViewRow represents an active transaction row
        private bool IsRowActiveTransaction(DataGridViewRow row)
        {
            if (row == null) return true;
            try
            {
                if (row.DataGridView.Columns.Contains("Status"))
                {
                    var statusObj = row.Cells["Status"].Value;
                    var statusStr = statusObj?.ToString() ?? "";
                    return IsStatusActiveString(statusStr);
                }
            }
            catch { /* ignore */ }
            return true;
        }

        private string GetTransactionAndPenaltyDisplayStatus(JObject tx, Dictionary<int, List<string>> penaltiesByTransaction)
        {
            if (tx == null) return "Unknown  ·  No Penalty";

            // preserve backend-provided transaction status (try several possible keys)
            string transactionStatus = tx.Value<string>("status")
                                       ?? tx.Value<string>("transaction_status")
                                       ?? tx.Value<string>("transaction_type")
                                       ?? string.Empty;

            transactionStatus = transactionStatus?.Trim();
            if (string.IsNullOrEmpty(transactionStatus))
            {
                // If backend left it empty, show explicit "Unknown" instead of inventing semantics.
                transactionStatus = "Unknown";
            }

            // determine transaction id (try common names)
            int txId = tx.Value<int?>("transaction_id")
                       ?? tx.Value<int?>("id")
                       ?? tx.Value<int?>("transactionId")
                       ?? 0;

            string penaltyPart = "No Penalty";
            if (txId != 0 && penaltiesByTransaction != null && penaltiesByTransaction.TryGetValue(txId, out var pStatuses) && pStatuses.Count > 0)
            {
                // preserve exactly what the backend provided, deduplicate identical strings
                var distinct = pStatuses.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.Ordinal).ToList();
                if (distinct.Count == 0)
                {
                    // fallback: if statuses are empty but penalties exist, show "Unknown"
                    penaltyPart = "Unknown";
                }
                else
                {
                    penaltyPart = string.Join(", ", distinct);
                }
            }

            return $"{transactionStatus}  ·  {penaltyPart}";
        }

        // -----------------------------------------------------------------------
        // Helpers moved to class-level so they are available anywhere in the method
        // -----------------------------------------------------------------------

        // Extract string property from either a JObject or a POCO/dynamic object
        private string GetObjectStringProp(object obj, params string[] names)
        {
            if (obj == null) return null;

            if (obj is JObject jo)
            {
                foreach (var n in names)
                {
                    // try SelectToken first (handles nested or different casing)
                    var v = jo.SelectToken(n, false) ?? jo.SelectToken(n.Replace("_", ""), false);
                    if (v != null && v.Type != JTokenType.Null)
                    {
                        var s = v.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                }

                // also try properties with different casings
                foreach (var prop in jo.Properties())
                {
                    foreach (var n in names)
                    {
                        if (string.Equals(prop.Name, n, StringComparison.OrdinalIgnoreCase))
                        {
                            var s = prop.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                        }
                    }
                }
            }
            else
            {
                var t = obj.GetType();
                foreach (var n in names)
                {
                    var pi = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (pi != null)
                    {
                        var val = pi.GetValue(obj);
                        if (val != null)
                        {
                            var s = val.ToString();
                            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                        }
                    }
                }
            }
            return null;
        }

        // Extract integer property from either a JObject or a POCO/dynamic object
        private int? GetObjectIntProp(object obj, params string[] names)
        {
            if (obj == null) return null;

            if (obj is JObject jo)
            {
                foreach (var n in names)
                {
                    var tok = jo.SelectToken(n, false) ?? jo.SelectToken(n.Replace("_", ""), false);
                    if (tok != null && tok.Type != JTokenType.Null)
                    {
                        if (int.TryParse(tok.ToString(), out var iv)) return iv;
                        if (long.TryParse(tok.ToString(), out var lv)) return (int)lv;
                    }
                }

                foreach (var prop in jo.Properties())
                {
                    foreach (var n in names)
                    {
                        if (string.Equals(prop.Name, n, StringComparison.OrdinalIgnoreCase))
                        {
                            var s = prop.Value?.ToString();
                            if (int.TryParse(s, out var iv)) return iv;
                        }
                    }
                }
            }
            else
            {
                var t = obj.GetType();
                foreach (var n in names)
                {
                    var pi = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (pi != null)
                    {
                        var val = pi.GetValue(obj);
                        if (val != null)
                        {
                            if (val is int i) return i;
                            if (val is long l) return (int)l;
                            if (int.TryParse(val.ToString(), out var iv)) return iv;
                        }
                    }
                }
            }

            return null;
        }
    }
}