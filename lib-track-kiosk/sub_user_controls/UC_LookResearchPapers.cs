using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Models = lib_track_kiosk.models;
using lib_track_kiosk.configs;
using lib_track_kiosk.helpers;
using lib_track_kiosk.loading_forms;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using lib_track_kiosk.caching;
using System.Threading;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookResearchPapers : UserControl
    {
        // PAGINATION SETTINGS
        private const int PAPERS_PER_PAGE = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // Full dataset from backend (never mutated by filtering)
        private List<Models.ResearchPaper> _allResearchPapers = new List<Models.ResearchPaper>();

        // Current (possibly filtered) view
        private List<Models.ResearchPaper> _filteredResearchPapers = new List<Models.ResearchPaper>();

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

        // Debounce timer for search box
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;

        // Departments for the combo box
        private List<DeptItem> _departments = new List<DeptItem>();

        // Currently selected department filter (null or 0 = All)
        private int? _selectedDepartmentId = null;

        // Cancellation token for async operations
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // Track if control is active
        private bool _isActive = false;

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

            // instantiate keyboard helper
            _osk = new OnScreenKeyboard();

            // Wire keyboard open/close to the search textbox if it exists
            if (search_txtBox != null)
            {
                search_txtBox.GotFocus += SearchTxtBox_GotFocus;
                search_txtBox.Enter += SearchTxtBox_GotFocus;
                search_txtBox.Click += SearchTxtBox_Click;
                search_txtBox.LostFocus += SearchTxtBox_LostFocus;
                search_txtBox.TextChanged += SearchTxtBox_TextChanged;
                search_txtBox.KeyDown += SearchTxtBox_KeyDown;
            }

            // wire department combo box change (if present)
            if (departments_cmbx != null)
            {
                departments_cmbx.SelectedIndexChanged += Departments_cmbx_SelectedIndexChanged;
            }

            // search debounce timer (300ms)
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            // Wire up visibility changed events for cleanup
            this.VisibleChanged += UC_LookResearchPapers_VisibleChanged;
            this.Disposed += UC_LookResearchPapers_Disposed;

            // reduce flicker by enabling double buffering
            TryEnableDoubleBuffer(research_FlowLayoutPanel);

            // start async load
            _ = LoadDepartmentsAsync();
            _ = LoadResearchPapersAsync();

            abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";

            // Scroll button handlers
            scrollUp_btn.Click += ScrollUp_btn_Click;
            scrollDown_btn.Click += ScrollDown_btn_Click;
        }

        /// <summary>
        /// Called when control visibility changes. Cleanup when hidden.
        /// </summary>
        private void UC_LookResearchPapers_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.Visible && _isActive)
                {
                    Console.WriteLine("🧹 UC_LookResearchPapers hidden - cleaning up memory...");
                    CleanupMemory();
                    _isActive = false;
                }
                else if (this.Visible && !_isActive)
                {
                    _isActive = true;
                    Console.WriteLine("✓ UC_LookResearchPapers shown");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_LookResearchPapers_VisibleChanged error: {ex.Message}");
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
                Console.WriteLine("🧹 Starting memory cleanup for UC_LookResearchPapers...");

                // Cancel any pending operations
                try { _cts?.Cancel(); } catch { }

                // Close keyboard
                TryCloseKeyboard();

                // Clear all paper cards
                ClearResearchFlowPanel();

                // Clear abstract
                if (abstract_rtbx != null)
                {
                    abstract_rtbx.Clear();
                    abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";
                }

                // Clear selection
                _selectedCard = null;

                // Clear filtered list (but keep _allResearchPapers for quick reload)
                _filteredResearchPapers?.Clear();

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
        public void ReloadData()
        {
            try
            {
                Console.WriteLine("🔄 Reloading UC_LookResearchPapers data...");

                // Reset to first page
                _currentPage = 1;

                // Restore filtered papers from cache
                _filteredResearchPapers = _allResearchPapers.Select(p => p).ToList();

                // Re-display
                DisplayResearchPapers();

                Console.WriteLine("✓ UC_LookResearchPapers data reloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ReloadData error: {ex.Message}");
            }
        }

        private void UC_LookResearchPapers_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine("🗑️ UC_LookResearchPapers disposing...");

            try { this.VisibleChanged -= UC_LookResearchPapers_VisibleChanged; } catch { }

            try
            {
                if (search_txtBox != null)
                {
                    search_txtBox.GotFocus -= SearchTxtBox_GotFocus;
                    search_txtBox.Enter -= SearchTxtBox_GotFocus;
                    search_txtBox.Click -= SearchTxtBox_Click;
                    search_txtBox.LostFocus -= SearchTxtBox_LostFocus;
                    search_txtBox.TextChanged -= SearchTxtBox_TextChanged;
                    search_txtBox.KeyDown -= SearchTxtBox_KeyDown;
                }
            }
            catch { }

            try
            {
                if (departments_cmbx != null)
                    departments_cmbx.SelectedIndexChanged -= Departments_cmbx_SelectedIndexChanged;
            }
            catch { }

            try { _osk?.Dispose(); } catch { }

            try
            {
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Dispose();
            }
            catch { }

            try { _cts.Cancel(); } catch { }
            try { _cts.Dispose(); } catch { }

            try { ClearResearchFlowPanel(); } catch { }

            try
            {
                _allResearchPapers?.Clear();
                _filteredResearchPapers?.Clear();
                _departments?.Clear();
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

            Console.WriteLine("✓ UC_LookResearchPapers disposed");
        }

        private void SearchTxtBox_Click(object sender, EventArgs e) => TryOpenKeyboard();
        private void SearchTxtBox_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void SearchTxtBox_LostFocus(object sender, EventArgs e)
        {
            await Task.Delay(150);
            try { if (!this.ContainsFocus) TryCloseKeyboard(); } catch { }
        }

        private void SearchTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e == null) return;
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    _searchDebounceTimer?.Stop();
                    ApplySearchFilter();
                    try { search_txtBox.Focus(); } catch { }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _searchDebounceTimer?.Stop();
                    if (search_txtBox != null && !string.IsNullOrEmpty(search_txtBox.Text))
                    {
                        search_txtBox.Text = string.Empty;
                        ApplySearchFilter();
                    }
                }
            }
            catch { }
        }

        private void TryOpenKeyboard()
        {
            try { _osk?.Open(); } catch { }
        }

        private void TryCloseKeyboard()
        {
            try { _osk?.Close(); } catch { }
        }

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
                    catch { }
                }

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

                ApplySearchFilter();
            }
            catch { }
        }

        private async Task LoadResearchPapersAsync()
        {
            Loading loadingForm = null;

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
                var fetched = await SharedResearchPaperCache.GetOrFetchAsync<AllResearchPaperInfo>(
                    () => GetAllResearchPapers.GetAllAsync()).ConfigureAwait(false);

                if (fetched != null && fetched.Count > 0)
                {
                    var mapped = fetched.Select(r => new Models.ResearchPaper
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

                    if (this.IsHandleCreated && this.InvokeRequired)
                    {
                        this.Invoke((Action)(() =>
                        {
                            _allResearchPapers = mapped;
                            _filteredResearchPapers = _allResearchPapers.Select(p => p).ToList();
                            _currentPage = 1;
                            DisplayResearchPapers();
                        }));
                    }
                    else
                    {
                        _allResearchPapers = mapped;
                        _filteredResearchPapers = _allResearchPapers.Select(p => p).ToList();
                        _currentPage = 1;
                        DisplayResearchPapers();
                    }
                }
                else
                {
                    _allResearchPapers = new List<Models.ResearchPaper>();
                    _filteredResearchPapers = new List<Models.ResearchPaper>();
                    DisplayResearchPapers();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading research papers: {ex.Message}");
                _allResearchPapers = new List<Models.ResearchPaper>();
                _filteredResearchPapers = new List<Models.ResearchPaper>();
                DisplayResearchPapers();
            }
            finally
            {
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
                    catch { }
                }
            }
        }

        public async Task RefreshResearchPapersAsync(bool force = false)
        {
            if (force)
            {
                SharedResearchPaperCache.Clear();
            }

            await LoadResearchPapersAsync().ConfigureAwait(false);
        }

        private void DisplayResearchPapers()
        {
            ClearResearchFlowPanel();
            research_FlowLayoutPanel.AutoScroll = true;
            research_FlowLayoutPanel.WrapContents = true;
            research_FlowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;

            // Calculate pagination
            _totalPages = (_filteredResearchPapers.Count + PAPERS_PER_PAGE - 1) / PAPERS_PER_PAGE;
            if (_totalPages == 0) _totalPages = 1;
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            research_FlowLayoutPanel.SuspendLayout();
            try
            {
                // Add pagination info at top
                AddPaginationInfo();

                if (_filteredResearchPapers == null || _filteredResearchPapers.Count == 0)
                {
                    _selectedCard = null;
                    if (abstract_rtbx != null)
                        abstract_rtbx.Text = "📄 No research papers found. Try a different search or select another department.";

                    AddNoResultsMessageToFlow();
                }
                else
                {
                    // Get papers for current page
                    var papersToDisplay = _filteredResearchPapers
                        .Skip((_currentPage - 1) * PAPERS_PER_PAGE)
                        .Take(PAPERS_PER_PAGE)
                        .ToList();

                    foreach (var paper in papersToDisplay)
                        AddResearchCard(paper);
                }

                // Add pagination controls at bottom
                AddPaginationControls();
            }
            finally
            {
                research_FlowLayoutPanel.ResumeLayout(false);
                research_FlowLayoutPanel.PerformLayout();
            }
        }

        private void AddPaginationInfo()
        {
            try
            {
                var panel = new Panel
                {
                    Width = research_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 20,
                    Height = 50,
                    Margin = new Padding(10, 10, 10, 5),
                    BackColor = Color.FromArgb(240, 248, 255)
                };

                var lblInfo = new Label
                {
                    AutoSize = false,
                    Width = panel.Width,
                    Height = 50,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 102, 204),
                    Text = $"Page {_currentPage} of {_totalPages} • Showing {Math.Min(_filteredResearchPapers.Count, PAPERS_PER_PAGE)} of {_filteredResearchPapers.Count} papers",
                    Dock = DockStyle.Fill
                };

                panel.Controls.Add(lblInfo);
                research_FlowLayoutPanel.Controls.Add(panel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddPaginationInfo error: {ex.Message}");
            }
        }

        private void AddPaginationControls()
        {
            try
            {
                var panel = new Panel
                {
                    Width = research_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 20,
                    Height = 70,
                    Margin = new Padding(10),
                    BackColor = Color.Transparent
                };

                int buttonWidth = 120;
                int buttonHeight = 50;
                int spacing = 20;
                int centerX = panel.Width / 2;

                var btnPrev = new Button
                {
                    Width = buttonWidth,
                    Height = buttonHeight,
                    Left = centerX - buttonWidth - spacing,
                    Top = 10,
                    Text = "◄ Previous",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 123, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Enabled = _currentPage > 1,
                    Cursor = Cursors.Hand
                };
                btnPrev.FlatAppearance.BorderSize = 0;
                btnPrev.Click += (s, e) =>
                {
                    if (_currentPage > 1)
                    {
                        _currentPage--;
                        DisplayResearchPapers();
                        research_FlowLayoutPanel.ScrollControlIntoView(research_FlowLayoutPanel.Controls[0]);
                    }
                };

                var btnNext = new Button
                {
                    Width = buttonWidth,
                    Height = buttonHeight,
                    Left = centerX + spacing,
                    Top = 10,
                    Text = "Next ►",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 123, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Enabled = _currentPage < _totalPages,
                    Cursor = Cursors.Hand
                };
                btnNext.FlatAppearance.BorderSize = 0;
                btnNext.Click += (s, e) =>
                {
                    if (_currentPage < _totalPages)
                    {
                        _currentPage++;
                        DisplayResearchPapers();
                        research_FlowLayoutPanel.ScrollControlIntoView(research_FlowLayoutPanel.Controls[0]);
                    }
                };

                if (!btnPrev.Enabled)
                {
                    btnPrev.BackColor = Color.Gray;
                    btnPrev.Cursor = Cursors.Default;
                }

                if (!btnNext.Enabled)
                {
                    btnNext.BackColor = Color.Gray;
                    btnNext.Cursor = Cursors.Default;
                }

                panel.Controls.Add(btnPrev);
                panel.Controls.Add(btnNext);

                research_FlowLayoutPanel.Controls.Add(panel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddPaginationControls error: {ex.Message}");
            }
        }

        private void AddNoResultsMessageToFlow()
        {
            try
            {
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
                Console.WriteLine($"⚠️ Failed to add 'no results' message: {ex.Message}");
            }
        }

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

            try
            {
                IntPtr hrgn = NativeMethods.CreateRoundRectRgn(0, 0, Math.Max(1, year.Width), Math.Max(1, year.Height), 8, 8);
                if (hrgn != IntPtr.Zero)
                {
                    try
                    {
                        year.Region = System.Drawing.Region.FromHrgn(hrgn);
                    }
                    catch { }
                    finally
                    {
                        try { NativeMethods.DeleteObject(hrgn); } catch { }
                    }
                }
            }
            catch { }

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

            void Card_Click(object sender, EventArgs e)
            {
                foreach (Control ctrl in research_FlowLayoutPanel.Controls)
                {
                    if (ctrl is Panel pnl)
                        SetCardSelectedVisual(pnl, false);
                }

                _selectedCard = card;
                SetCardSelectedVisual(card, true);

                ShowAbstract(paper);
            }

            card.Controls.Add(topBar);
            card.Controls.Add(title);
            card.Controls.Add(authors);
            card.Controls.Add(department);
            card.Controls.Add(year);

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

        private void SearchTxtBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
            catch { }
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer.Stop();
                ApplySearchFilter();
            }
            catch { }
        }

        private void ApplySearchFilter()
        {
            string query = search_txtBox?.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(query) && !_selectedDepartmentId.HasValue)
            {
                _filteredResearchPapers = _allResearchPapers.Select(p => p).ToList();
            }
            else
            {
                string q = query;
                _filteredResearchPapers = _allResearchPapers.Where(p =>
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

            _currentPage = 1;
            _selectedCard = null;
            abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";

            DisplayResearchPapers();
        }

        private void ScrollUp_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 400;
            var scroll = research_FlowLayoutPanel.VerticalScroll;
            int newValue = Math.Max(scroll.Minimum, scroll.Value - scrollAmount);
            scroll.Value = newValue;
            research_FlowLayoutPanel.PerformLayout();
        }

        private void ScrollDown_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 400;
            var scroll = research_FlowLayoutPanel.VerticalScroll;
            int newValue = Math.Min(scroll.Maximum, scroll.Value + scrollAmount);
            scroll.Value = newValue;
            research_FlowLayoutPanel.PerformLayout();
        }

        private void TryEnableDoubleBuffer(Control c)
        {
            if (c == null) return;
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch { }
        }

        private void ClearResearchFlowPanel()
        {
            if (research_FlowLayoutPanel == null) return;
            try
            {
                research_FlowLayoutPanel.SuspendLayout();

                var controls = research_FlowLayoutPanel.Controls.Cast<Control>().ToArray();
                foreach (var c in controls)
                {
                    try
                    {
                        research_FlowLayoutPanel.Controls.Remove(c);
                        DisposeControlAndChildren(c);
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                try { research_FlowLayoutPanel.ResumeLayout(false); } catch { }
            }
        }

        private void DisposeControlAndChildren(Control ctrl)
        {
            if (ctrl == null) return;

            try
            {
                var children = ctrl.Controls.Cast<Control>().ToArray();
                foreach (var ch in children)
                {
                    DisposeControlAndChildren(ch);
                }

                try { ctrl.Controls.Clear(); } catch { }
                try { ctrl.Dispose(); } catch { }
            }
            catch { }
        }

        private static class NativeMethods
        {
            [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
            public static extern IntPtr CreateRoundRectRgn(
                int nLeftRect, int nTopRect,
                int nRightRect, int nBottomRect,
                int nWidthEllipse, int nHeightEllipse
            );

            [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr hObject);
        }
    }
}