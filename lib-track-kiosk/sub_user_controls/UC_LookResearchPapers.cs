using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Models = lib_track_kiosk.models;    // alias to avoid ambiguity with helper types
using lib_track_kiosk.configs;
using lib_track_kiosk.helpers;       // for GetAllResearchPapers / AllResearchPaperInfo
using lib_track_kiosk.loading_forms; // for Loading
using Newtonsoft.Json.Linq;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookResearchPapers : UserControl
    {
        // Full dataset from backend (never mutated by filtering)
        private List<Models.ResearchPaper> _allResearchPapers = new List<Models.ResearchPaper>();

        // Current (possibly filtered) view
        private List<Models.ResearchPaper> _researchPapers = new List<Models.ResearchPaper>();

        private readonly List<Color> _accentColors = new List<Color>
        {
            Color.FromArgb(20, 54, 100),
            Color.FromArgb(15, 86, 63),
            Color.FromArgb(115, 28, 36),
            Color.FromArgb(72, 42, 95),
            Color.FromArgb(85, 93, 80),
            Color.FromArgb(140, 60, 12),
            Color.FromArgb(12, 93, 93),
            Color.FromArgb(100, 20, 60),
            Color.FromArgb(60, 60, 60),
            Color.FromArgb(44, 62, 80)
        };

        private readonly Random _rand = new Random();

        // On-screen keyboard helper (reusable)
        private OnScreenKeyboard _osk;

        // track currently selected card so we can toggle selection visuals
        private Panel _selectedCard;

        // Debounce timer for search box — explicitly use System.Windows.Forms.Timer to avoid ambiguity
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;

        // Departments for the combo box
        private List<DeptItem> _departments = new List<DeptItem>();

        // Currently selected department filter (null or 0 = All)
        private int? _selectedDepartmentId = null;

        // DeptItem helper (for ComboBox display + value)
        private class DeptItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name ?? "N/A";
        }

        public UC_LookResearchPapers()
        {
            InitializeComponent();

            // instantiate keyboard helper (lazy alternative is possible)
            _osk = new OnScreenKeyboard();

            // Wire keyboard open/close to the search textbox if it exists
            if (search_txtBox != null)
            {
                search_txtBox.GotFocus += SearchTxtBox_GotFocus;
                search_txtBox.Enter += SearchTxtBox_GotFocus;
                search_txtBox.Click += SearchTxtBox_Click;
                search_txtBox.LostFocus += SearchTxtBox_LostFocus;

                // Set up text-changed handler for searching
                search_txtBox.TextChanged += SearchTxtBox_TextChanged;
            }

            // wire department combo box change (if present)
            if (departments_cmbx != null)
            {
                departments_cmbx.SelectedIndexChanged += Departments_cmbx_SelectedIndexChanged;
            }

            // search debounce timer (300ms) — instantiate explicitly as Forms Timer
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            // clean up on disposal (avoid designer Dispose conflict)
            this.Disposed += UC_LookResearchPapers_Disposed;

            // reduce flicker by enabling double buffering on the flow panel (non-public property)
            TryEnableDoubleBuffer(research_FlowLayoutPanel);

            // start async load from backend (shows Loading form while fetching)
            _ = LoadDepartmentsAsync();       // populate department filter first
            _ = LoadResearchPapersAsync();    // then load papers

            abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";

            // 🖱️ Scroll button handlers
            scrollUp_btn.Click += ScrollUp_btn_Click;
            scrollDown_btn.Click += ScrollDown_btn_Click;
        }

        private void UC_LookResearchPapers_Disposed(object sender, EventArgs e)
        {
            try
            {
                if (search_txtBox != null)
                {
                    search_txtBox.GotFocus -= SearchTxtBox_GotFocus;
                    search_txtBox.Enter -= SearchTxtBox_GotFocus;
                    search_txtBox.Click -= SearchTxtBox_Click;
                    search_txtBox.LostFocus -= SearchTxtBox_LostFocus;
                    search_txtBox.TextChanged -= SearchTxtBox_TextChanged;
                }
            }
            catch { /* ignore */ }

            try
            {
                if (departments_cmbx != null)
                    departments_cmbx.SelectedIndexChanged -= Departments_cmbx_SelectedIndexChanged;
            }
            catch { /* ignore */ }

            try
            {
                _osk?.Dispose();
            }
            catch { /* ignore */ }

            try
            {
                // Stop and Dispose the forms timer safely
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Dispose();
            }
            catch { /* ignore */ }
        }

        private void SearchTxtBox_Click(object sender, EventArgs e) => TryOpenKeyboard();
        private void SearchTxtBox_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void SearchTxtBox_LostFocus(object sender, EventArgs e)
        {
            // Debounce so quick focus moves within this control don't close the keyboard immediately
            await Task.Delay(150);
            try
            {
                // If focus has left this UserControl entirely, close the keyboard
                if (!this.ContainsFocus)
                    TryCloseKeyboard();
            }
            catch { /* best-effort */ }
        }

        private void TryOpenKeyboard()
        {
            try { _osk?.Open(); } catch { /* ignore */ }
        }

        private void TryCloseKeyboard()
        {
            try { _osk?.Close(); } catch { /* ignore */ }
        }

        /// <summary>
        /// Loads departments from API and populates departments_cmbx (if present).
        /// Endpoint: {API_Backend.BaseUrl}/api/settings/departments
        /// </summary>
        private async Task LoadDepartmentsAsync()
        {
            if (departments_cmbx == null) return;

            try
            {
                using var http = new HttpClient();
                string url = $"{API_Backend.BaseUrl}/api/settings/departments";
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return;

                string jsonString = await res.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonString);
                if (!(bool?)json["success"] == true) return;

                JArray data = json["data"] as JArray;
                if (data == null) return;

                _departments.Clear();
                // Add "All Departments" entry (Id = 0)
                _departments.Add(new DeptItem { Id = 0, Name = "All Departments" });

                foreach (var t in data)
                {
                    try
                    {
                        var obj = (JObject)t;
                        int id = (int?)(obj["department_id"]) ?? 0;
                        string name = obj["department_name"]?.ToString() ?? $"Dept {id}";
                        _departments.Add(new DeptItem { Id = id, Name = name });
                    }
                    catch { /* ignore item */ }
                }

                // Populate combo box on UI thread
                if (departments_cmbx.InvokeRequired)
                {
                    departments_cmbx.Invoke((Action)(() =>
                    {
                        departments_cmbx.Items.Clear();
                        foreach (var d in _departments) departments_cmbx.Items.Add(d);
                        departments_cmbx.SelectedIndex = 0;
                    }));
                }
                else
                {
                    departments_cmbx.Items.Clear();
                    foreach (var d in _departments) departments_cmbx.Items.Add(d);
                    departments_cmbx.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load departments: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the department combobox selection changes.
        /// Sets the selected department id and reapplies the current search filter.
        /// </summary>
        private void Departments_cmbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (departments_cmbx.SelectedItem is DeptItem di)
                {
                    _selectedDepartmentId = di.Id == 0 ? (int?)null : di.Id;
                }
                else
                {
                    _selectedDepartmentId = null;
                }

                // Reapply search with current query and new department filter
                string query = search_txtBox?.Text ?? "";
                ApplySearchFilter(query);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Load research papers from the backend using GetAllResearchPapers.
        /// Shows the Loading form while fetching and closes it when finished.
        /// Maps helper DTO (AllResearchPaperInfo) into the UI model (Models.ResearchPaper).
        /// </summary>
        private async Task LoadResearchPapersAsync()
        {
            Loading loadingForm = null;

            // Show loading form (non-blocking)
            try
            {
                loadingForm = new Loading();
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    loadingForm.StartPosition = FormStartPosition.CenterParent;
                    loadingForm.Show(parentForm);
                }
                else
                {
                    loadingForm.StartPosition = FormStartPosition.CenterScreen;
                    loadingForm.Show();
                }

                loadingForm.Refresh();
                Application.DoEvents();
            }
            catch
            {
                try { loadingForm?.Dispose(); } catch { }
                loadingForm = null;
            }

            try
            {
                // GetAllResearchPapers returns List<AllResearchPaperInfo>
                var fetched = await GetAllResearchPapers.GetAllAsync();

                if (fetched != null && fetched.Count > 0)
                {
                    // Map AllResearchPaperInfo -> Models.ResearchPaper
                    _allResearchPapers = fetched.Select(r => new Models.ResearchPaper
                    {
                        Id = r.ResearchPaperId,
                        Title = string.IsNullOrWhiteSpace(r.Title) ? "N/A" : r.Title,
                        Authors = string.IsNullOrWhiteSpace(r.Authors) ? "N/A" : r.Authors,
                        Year = int.TryParse(r.YearPublication, out var y) ? y : 0,
                        Abstract = string.IsNullOrWhiteSpace(r.Abstract) ? "" : r.Abstract,
                        DepartmentId = r.DepartmentId,
                        DepartmentName = string.IsNullOrWhiteSpace(r.DepartmentName) ? "N/A" : r.DepartmentName,
                        ShelfLocation = string.IsNullOrWhiteSpace(r.ShelfLocation) ? "N/A" : r.ShelfLocation
                    }).ToList();

                    // initial view = full dataset
                    _researchPapers = new List<Models.ResearchPaper>(_allResearchPapers);
                }
                else
                {
                    // no results — clear UI list
                    _allResearchPapers = new List<Models.ResearchPaper>();
                    _researchPapers = new List<Models.ResearchPaper>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading research papers: {ex.Message}");
                _allResearchPapers = new List<Models.ResearchPaper>();
                _researchPapers = new List<Models.ResearchPaper>();
            }
            finally
            {
                // Close and dispose the loading form on UI thread
                if (loadingForm != null)
                {
                    try
                    {
                        if (!loadingForm.IsDisposed)
                        {
                            if (loadingForm.InvokeRequired)
                                loadingForm.Invoke((Action)(() => { loadingForm.Close(); loadingForm.Dispose(); }));
                            else
                            {
                                loadingForm.Close();
                                loadingForm.Dispose();
                            }
                        }
                    }
                    catch { /* ignore */ }
                }
            }

            // Update UI
            DisplayResearchPapers();
        }

        private void DisplayResearchPapers()
        {
            research_FlowLayoutPanel.Controls.Clear();
            research_FlowLayoutPanel.AutoScroll = true;
            research_FlowLayoutPanel.WrapContents = true;
            research_FlowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;

            // If there are no items to display, show an informative message in the flow panel
            if (_researchPapers == null || _researchPapers.Count == 0)
            {
                // Clear any selection / abstract
                _selectedCard = null;
                if (abstract_rtbx != null)
                    abstract_rtbx.Text = "📄 No research papers found. Try a different search or select another department.";

                AddNoResultsMessageToFlow();
                return;
            }

            foreach (var paper in _researchPapers)
                AddResearchCard(paper);
        }

        // Adds a centered "no results" message into the FlowLayoutPanel
        private void AddNoResultsMessageToFlow()
        {
            try
            {
                // Create a panel to host the message so margins/padding look nice
                var msgPanel = new Panel
                {
                    Width = Math.Max(200, research_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 40),
                    Height = 120,
                    Margin = new Padding(20),
                    BackColor = Color.Transparent
                };

                var lbl = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12f, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Text = "No research papers found.\nTry a different search or choose another department."
                };

                msgPanel.Controls.Add(lbl);

                // If invoked from a non-UI thread, marshal to UI thread
                if (research_FlowLayoutPanel.InvokeRequired)
                {
                    research_FlowLayoutPanel.Invoke((Action)(() => research_FlowLayoutPanel.Controls.Add(msgPanel)));
                }
                else
                {
                    research_FlowLayoutPanel.Controls.Add(msgPanel);
                }
            }
            catch (Exception ex)
            {
                // Swallow errors for UI fallback; still leave flow empty
                Console.WriteLine($"⚠️ Failed to add 'no results' message: {ex.Message}");
            }
        }

        // Use Models.ResearchPaper in method signatures to avoid ambiguity
        private void AddResearchCard(Models.ResearchPaper paper)
        {
            int cardWidth = 410;
            int cardHeight = 150;
            int padding = 16;
            int badgeWidth = 56;
            int badgeHeight = 28;
            int gap = 12;

            Color accent = _accentColors[_rand.Next(_accentColors.Count)];

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                BackColor = Color.White,
                Margin = new Padding(12, 10, 12, 10),
                Tag = paper.Id,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = accent
            };

            int contentWidth = cardWidth - (padding * 2) - badgeWidth - gap;
            if (contentWidth < 180) contentWidth = 180;

            var authorsList = (paper.Authors ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
            string authorsDisplay = string.Join(", ", authorsList.Take(3));
            if (authorsList.Count > 3) authorsDisplay += " + others";

            Label title = new Label
            {
                AutoSize = false,
                Left = padding,
                Top = 12,
                Width = contentWidth,
                Height = 64,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.TopLeft,
                Text = paper.Title,
                AutoEllipsis = true
            };

            Label authors = new Label
            {
                AutoSize = false,
                Left = padding,
                Top = title.Top + title.Height + 6,
                Width = contentWidth,
                Height = 22,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = authorsDisplay,
                AutoEllipsis = true
            };

            // Department label (displayed under authors)
            Label department = new Label
            {
                AutoSize = false,
                Left = padding,
                Top = authors.Top + authors.Height + 6,
                Width = contentWidth,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.IsNullOrWhiteSpace(paper.DepartmentName) ? "Department: N/A" : $"🏫 {paper.DepartmentName}",
                AutoEllipsis = true
            };

            int badgeLeft = padding + contentWidth + gap;
            int badgeTop = title.Top + (title.Height / 2) - (badgeHeight / 2);
            if (badgeTop < 10) badgeTop = 10;

            Label year = new Label
            {
                AutoSize = false,
                Width = badgeWidth,
                Height = badgeHeight,
                Left = badgeLeft,
                Top = badgeTop,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = accent,
                Text = paper.Year > 0 ? paper.Year.ToString() : "N/A"
            };

            year.Region = System.Drawing.Region.FromHrgn(
                NativeMethods.CreateRoundRectRgn(0, 0, year.Width, year.Height, 8, 8)
            );

            // Selection visual setter (no hover visuals)
            void SetCardSelectedVisual(Panel p, bool selected)
            {
                if (p == null) return;
                if (selected)
                {
                    p.BackColor = Color.FromArgb(235, 245, 255);
                    p.BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    p.BackColor = Color.White;
                    p.BorderStyle = BorderStyle.FixedSingle;
                }
            }

            // click handler selects this card and shows the abstract
            void Card_Click(object sender, EventArgs e)
            {
                // deselect all cards first
                foreach (Control ctrl in research_FlowLayoutPanel.Controls)
                {
                    if (ctrl is Panel pnl)
                        SetCardSelectedVisual(pnl, false);
                }

                // select clicked card
                _selectedCard = card;
                SetCardSelectedVisual(card, true);

                ShowAbstract(paper);
            }

            // Add controls then wire clicks — forward child clicks to the card click handler
            card.Controls.Add(topBar);
            card.Controls.Add(title);
            card.Controls.Add(authors);
            card.Controls.Add(department);
            card.Controls.Add(year);

            // Attach click handlers to the card and all children so clicks anywhere select it.
            Action<Control> attachClickRecursively = null;
            attachClickRecursively = (ctrl) =>
            {
                ctrl.Click += Card_Click;
                foreach (Control child in ctrl.Controls)
                    attachClickRecursively(child);
            };

            attachClickRecursively(card);

            research_FlowLayoutPanel.Controls.Add(card);
        }

        private void ShowAbstract(Models.ResearchPaper paper)
        {
            if (abstract_rtbx == null)
            {
                MessageBox.Show("abstract_rtbx control not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            abstract_rtbx.Clear();

            var authorsList = (paper.Authors ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
            string formattedAuthors = "👥 Authors:\n👤 " + string.Join("\n👤 ", authorsList);

            string departmentLine = string.IsNullOrWhiteSpace(paper.DepartmentName) ? "🏫 Department: N/A" : $"🏫 Department: {paper.DepartmentName}";
            string shelfLine = string.IsNullOrWhiteSpace(paper.ShelfLocation) ? "📚 Shelf: N/A" : $"📚 Shelf: {paper.ShelfLocation}";

            abstract_rtbx.Text =
                $"📘 {paper.Title}\n\n{formattedAuthors}\n\n{departmentLine}\n{shelfLine}\n\n📅 {paper.Year}\n\n{paper.Abstract}";
            abstract_rtbx.SelectionStart = 0;
            abstract_rtbx.ScrollToCaret();
        }

        // Search textbox changed -> debounce and apply filter
        private void SearchTxtBox_TextChanged(object sender, EventArgs e)
        {
            // restart debounce timer
            try
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
            catch { /* ignore */ }
        }

        // Debounce timer tick -> perform search
        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer.Stop();
                string query = search_txtBox?.Text ?? "";
                ApplySearchFilter(query);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Filters the full dataset (_allResearchPapers) by the query string.
        /// Searches in Title, Authors and Abstract, DepartmentName and Year.
        /// Also applies the selected department filter (if any).
        /// </summary>
        private void ApplySearchFilter(string query)
        {
            if (string.IsNullOrWhiteSpace(query) && !_selectedDepartmentId.HasValue)
            {
                // Reset to full list
                _researchPapers = new List<Models.ResearchPaper>(_allResearchPapers);
            }
            else
            {
                string q = (query ?? "").Trim();
                // case-insensitive contains checks and optional department filter
                _researchPapers = _allResearchPapers.Where(p =>
                    (!_selectedDepartmentId.HasValue || (p.DepartmentId.HasValue && p.DepartmentId.Value == _selectedDepartmentId.Value)) &&
                    (
                        string.IsNullOrEmpty(q) ||
                        ((!string.IsNullOrEmpty(p.Title) && p.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (!string.IsNullOrEmpty(p.Authors) && p.Authors.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (!string.IsNullOrEmpty(p.Abstract) && p.Abstract.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (!string.IsNullOrEmpty(p.DepartmentName) && p.DepartmentName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         p.Year.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    )
                ).ToList();
            }

            // clear selection and abstract when filtering
            _selectedCard = null;
            abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";

            // refresh UI
            DisplayResearchPapers();
        }

        // 🖱 Scroll up/down by one card
        private void ScrollUp_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 160; // card height + spacing
            research_FlowLayoutPanel.AutoScrollPosition = new Point(
                0,
                Math.Max(0, research_FlowLayoutPanel.VerticalScroll.Value - scrollAmount)
            );
        }

        private void ScrollDown_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 160;
            int newValue = research_FlowLayoutPanel.VerticalScroll.Value + scrollAmount;
            int maxValue = research_FlowLayoutPanel.VerticalScroll.Maximum;

            if (newValue > maxValue)
                newValue = maxValue;

            research_FlowLayoutPanel.AutoScrollPosition = new Point(0, newValue);
        }

        private void TryEnableDoubleBuffer(Control c)
        {
            if (c == null) return;
            try
            {
                // set protected DoubleBuffered property using reflection
                var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch
            {
                // best-effort
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
            public static extern IntPtr CreateRoundRectRgn(
                int nLeftRect, int nTopRect,
                int nRightRect, int nBottomRect,
                int nWidthEllipse, int nHeightEllipse
            );
        }
    }
}