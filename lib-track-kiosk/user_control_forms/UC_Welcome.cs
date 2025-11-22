using lib_track_kiosk.user_control_forms;
using lib_track_kiosk.models;
using lib_track_kiosk.configs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;

namespace lib_track_kiosk.panel_forms
{
    public partial class UC_Welcome : UserControl
    {
        private List<Book> _topBooks = new List<Book>();
        private List<(string Department, int Count)> _topDepartments = new List<(string, int)>();
        private List<(string FirstName, string LastName, string DeptAcronym)> _topStudents = new List<(string, string, string)>();

        private readonly string DefaultAvatarPath = FileLocations.DefaultAvatarPath;

        // store references to handlers so we can unsubscribe them later
        private EventHandler _top3DeptSizeChangedHandler;
        private EventHandler _top5StudentsSizeChangedHandler;

        // Track if control is active
        private bool _isActive = false;

        // Cancellation token for any async operations (if added later)
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public UC_Welcome()
        {
            InitializeComponent();

            // enable double buffering for smoother rendering
            this.DoubleBuffered = true;

            // prepare and keep handlers so we can detach later
            _top3DeptSizeChangedHandler = (s, e) => DisplayTopDepartments();
            _top5StudentsSizeChangedHandler = (s, e) => DisplayTopStudents();

            // attach handlers (guarding against null panels in designer/init time)
            if (top3Department_flp != null)
                top3Department_flp.SizeChanged += _top3DeptSizeChangedHandler;
            if (top5MostStudentBorrowers_flp != null)
                top5MostStudentBorrowers_flp.SizeChanged += _top5StudentsSizeChangedHandler;

            // Wire up visibility changed events for cleanup
            this.VisibleChanged += UC_Welcome_VisibleChanged;

            LoadTopBooks();
            LoadTopDepartments();
            LoadTopStudents();

            _isActive = true;
        }

        /// <summary>
        /// Called when control visibility changes. Cleanup when hidden.
        /// </summary>
        private void UC_Welcome_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.Visible && _isActive)
                {
                    Console.WriteLine("🧹 UC_Welcome hidden - cleaning up memory...");
                    CleanupMemory();
                    _isActive = false;
                }
                else if (this.Visible && !_isActive)
                {
                    _isActive = true;
                    Console.WriteLine("✓ UC_Welcome shown");

                    // Optionally reload data when returning
                    // ReloadData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_Welcome_VisibleChanged error: {ex.Message}");
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
                Console.WriteLine("🧹 Starting memory cleanup for UC_Welcome...");

                // Cancel any pending operations
                try { _cts?.Cancel(); } catch { }

                // Dispose images in all containers
                DisposeImagesInContainer(top10Books_flp);
                DisposeImagesInContainer(top3Department_flp);
                DisposeImagesInContainer(top5MostStudentBorrowers_flp);

                // Clear controls
                try { top10Books_flp?.Controls.Clear(); } catch { }
                try { top3Department_flp?.Controls.Clear(); } catch { }
                try { top5MostStudentBorrowers_flp?.Controls.Clear(); } catch { }

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
                Console.WriteLine("🔄 Reloading UC_Welcome data...");

                // Reload all sections
                LoadTopBooks();
                LoadTopDepartments();
                LoadTopStudents();

                Console.WriteLine("✓ UC_Welcome data reloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ReloadData error: {ex.Message}");
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Console.WriteLine("🗑️ UC_Welcome handle destroying...");

            try { this.VisibleChanged -= UC_Welcome_VisibleChanged; } catch { }

            try
            {
                // Unsubscribe the size-changed handlers
                if (top3Department_flp != null && _top3DeptSizeChangedHandler != null)
                    top3Department_flp.SizeChanged -= _top3DeptSizeChangedHandler;
                if (top5MostStudentBorrowers_flp != null && _top5StudentsSizeChangedHandler != null)
                    top5MostStudentBorrowers_flp.SizeChanged -= _top5StudentsSizeChangedHandler;
            }
            catch { }

            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }

            try
            {
                // Dispose any images held by controls before clearing to prevent GDI+ leaks
                DisposeImagesInContainer(top10Books_flp);
                DisposeImagesInContainer(top3Department_flp);
                DisposeImagesInContainer(top5MostStudentBorrowers_flp);

                // Clear child controls (this disposes child controls)
                try { top10Books_flp?.Controls.Clear(); } catch { }
                try { top3Department_flp?.Controls.Clear(); } catch { }
                try { top5MostStudentBorrowers_flp?.Controls.Clear(); } catch { }
            }
            catch { }

            try
            {
                // Clear data lists
                _topBooks?.Clear();
                _topDepartments?.Clear();
                _topStudents?.Clear();
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

            Console.WriteLine("✓ UC_Welcome handle destroyed");

            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Recursively disposes images assigned to PictureBoxes inside a container control.
        /// Safe to call multiple times.
        /// </summary>
        private void DisposeImagesInContainer(Control container)
        {
            if (container == null) return;

            // iterate over a copy because disposing controls may modify Controls collection
            var children = container.Controls.Cast<Control>().ToArray();
            foreach (Control c in children)
            {
                try
                {
                    if (c is PictureBox pb)
                    {
                        var img = pb.Image;
                        pb.Image = null;
                        try { img?.Dispose(); } catch { /* ignore disposal errors */ }
                    }

                    // recursively clear for nested controls
                    if (c.HasChildren)
                        DisposeImagesInContainer(c);
                }
                catch
                {
                    // ignore per-control disposal errors so dispose continues for others
                }
            }
        }

        private void lookForBooksResearch_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📚 Navigating to Look & Search...");

                var mainForm = this.FindForm() as MainForm;
                if (mainForm != null)
                {
                    // Cleanup before leaving
                    CleanupMemory();

                    var lookSearchScreen = new UC_LookSearch();
                    mainForm.addUserControl(lookSearchScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ lookForBooksResearch_btn_Click error: {ex.Message}");
            }
        }

        private void borrow_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📖 Navigating to Borrow...");

                var mainForm = this.FindForm() as MainForm;
                if (mainForm != null)
                {
                    // Cleanup before leaving
                    CleanupMemory();

                    var borrowScreen = new UC_Borrow();
                    mainForm.addUserControl(borrowScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ borrow_btn_Click error: {ex.Message}");
            }
        }

        private void return_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📥 Navigating to Return...");

                var mainForm = this.FindForm() as MainForm;
                if (mainForm != null)
                {
                    // Cleanup before leaving
                    CleanupMemory();

                    var returnScreen = new UC_Return();
                    mainForm.addUserControl(returnScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ return_btn_Click error: {ex.Message}");
            }
        }

        private void registerFingerprint_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("👆 Navigating to User Login...");

                var mainForm = this.FindForm() as MainForm;
                if (mainForm != null)
                {
                    // Cleanup before leaving
                    CleanupMemory();

                    var userLoginScreen = new UC_UserLogin();
                    mainForm.addUserControl(userLoginScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ registerFingerprint_btn_Click error: {ex.Message}");
            }
        }

        private void reservation_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📅 Reservation button clicked");
                // TODO: Implement reservation functionality
                MessageBox.Show("Reservation feature coming soon!", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ reservation_btn_Click error: {ex.Message}");
            }
        }

        // ==========================================
        // LOAD STATIC TOP BOOKS
        // ==========================================
        private void LoadTopBooks()
        {
            try
            {
                // Clear existing books before loading new ones
                _topBooks?.Clear();

                string path1 = FileLocations.SampleBookCover1;
                string path2 = FileLocations.SampleBookCover2;
                string path3 = FileLocations.SampleBookCover3;

                _topBooks = new List<Book>
                {
                    new Book { Id = 1, Title = "The Art of Computer Programming", Author = "Donald Knuth", Year = 2011, CoverImagePath = path1 },
                    new Book { Id = 2, Title = "Artificial Intelligence: Principles and Practice", Author = "Russell & Norvig", Year = 2022, CoverImagePath = path2 },
                    new Book { Id = 3, Title = "Learning AI with Real-World Projects", Author = "Jane Smith", Year = 2023, CoverImagePath = path3 },
                    new Book { Id = 4, Title = "Database Systems: The Complete Reference", Author = "C. J. Date", Year = 2020, CoverImagePath = path1 },
                    new Book { Id = 5, Title = "C# Programming: From Basics to Advanced", Author = "John Doe", Year = 2021, CoverImagePath = path2 },
                    new Book { Id = 6, Title = "Modern Web Development with React", Author = "Mark Brown", Year = 2024, CoverImagePath = path3 },
                    new Book { Id = 7, Title = "Data Structures and Algorithms Illustrated", Author = "David Knuth", Year = 2022, CoverImagePath = path1 },
                    new Book { Id = 8, Title = "Exploring Quantum Computing", Author = "Richard Feynman", Year = 2025, CoverImagePath = path2 },
                    new Book { Id = 9, Title = "Cybersecurity Essentials", Author = "Alice Li", Year = 2021, CoverImagePath = path3 },
                    new Book { Id = 10, Title = "Human-Computer Interaction Design", Author = "Don Norman", Year = 2020, CoverImagePath = path1 }
                };

                DisplayTopBooks();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ LoadTopBooks error: {ex.Message}");
            }
        }

        private void DisplayTopBooks()
        {
            try
            {
                // Dispose images that may be referenced by child controls to avoid GDI+ leaks
                DisposeImagesInContainer(top10Books_flp);

                top10Books_flp.Controls.Clear();
                top10Books_flp.AutoScroll = true;
                top10Books_flp.WrapContents = true;
                top10Books_flp.FlowDirection = FlowDirection.LeftToRight;
                top10Books_flp.Padding = new Padding(8);
                top10Books_flp.BackColor = Color.WhiteSmoke;

                // Suspend layout for better performance
                top10Books_flp.SuspendLayout();

                int rank = 1;
                foreach (var book in _topBooks)
                {
                    AddBookCard(book, rank++);
                }

                top10Books_flp.ResumeLayout(false);
                top10Books_flp.PerformLayout();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ DisplayTopBooks error: {ex.Message}");
            }
        }

        // ==========================================
        // CREATE BOOK CARD
        // ==========================================
        private void AddBookCard(Book book, int rank)
        {
            try
            {
                var card = new Panel
                {
                    Width = 140,
                    Height = 230,
                    BackColor = Color.White,
                    Margin = new Padding(8),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = book.Id,
                    Cursor = Cursors.Hand
                };

                // Hover subtle effect
                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(247, 247, 247);
                card.MouseLeave += (s, e) => card.BackColor = Color.White;

                // Rank badge
                var rankPanel = new Panel
                {
                    Width = 36,
                    Height = 20,
                    Left = 4,
                    Top = 4,
                    BackColor = Color.FromArgb(70, 60, 160),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };

                var rankLabel = new Label
                {
                    Text = $"#{rank}",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                rankPanel.Controls.Add(rankLabel);

                // Book cover
                var cover = new PictureBox
                {
                    Width = 100,
                    Height = 140,
                    Left = (card.Width - 100) / 2,
                    Top = 30,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.WhiteSmoke
                };

                // Load image safely (clone to avoid file lock)
                if (!string.IsNullOrWhiteSpace(book.CoverImagePath) && File.Exists(book.CoverImagePath))
                {
                    try
                    {
                        using (var fs = File.OpenRead(book.CoverImagePath))
                        using (var img = Image.FromStream(fs))
                        {
                            cover.Image = new Bitmap(img);
                        }
                    }
                    catch
                    {
                        try { cover.Image = SystemIcons.Application.ToBitmap(); } catch { cover.Image = new Bitmap(1, 1); }
                    }
                }
                else
                {
                    try { cover.Image = SystemIcons.Application.ToBitmap(); } catch { cover.Image = new Bitmap(1, 1); }
                }

                // Title
                var title = new Label
                {
                    AutoSize = false,
                    Width = card.Width - 16,
                    Height = 36,
                    Left = 8,
                    Top = cover.Bottom + 6,
                    Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.TopCenter,
                    Text = TruncateText(book.Title, 48)
                };

                // Author (smaller italic)
                var author = new Label
                {
                    AutoSize = false,
                    Width = card.Width - 16,
                    Height = 20,
                    Left = 8,
                    Top = title.Bottom + 2,
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.TopCenter,
                    Text = TruncateText(book.Author, 40)
                };

                // Add to card
                card.Controls.Add(rankPanel);
                card.Controls.Add(cover);
                card.Controls.Add(title);
                card.Controls.Add(author);

                // click to show details
                EventHandler showHandler = (s, e) => ShowBookDetails(book);
                card.Click += showHandler;
                foreach (Control c in card.Controls) c.Click += showHandler;
                card.DoubleClick += (s, e) => ShowBookDetails(book);

                top10Books_flp.Controls.Add(card);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddBookCard error: {ex.Message}");
            }
        }

        // ==========================================
        // TOP 3 DEPARTMENTS (CARD UI)
        // ==========================================
        private void LoadTopDepartments()
        {
            try
            {
                // Clear existing departments before loading new ones
                _topDepartments?.Clear();

                _topDepartments = new List<(string, int)>
                {
                    ("Bachelor of Elementary Education", 142),
                    ("Bachelor of Arts in Political Science", 97),
                    ("Bachelor of Science in Computer Science", 86)
                };

                DisplayTopDepartments();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ LoadTopDepartments error: {ex.Message}");
            }
        }

        private void DisplayTopDepartments()
        {
            try
            {
                DisposeImagesInContainer(top3Department_flp);
                top3Department_flp.Controls.Clear();
                top3Department_flp.AutoScroll = true;
                top3Department_flp.FlowDirection = FlowDirection.TopDown;
                top3Department_flp.WrapContents = false;
                top3Department_flp.Padding = new Padding(8);
                top3Department_flp.BackColor = Color.Transparent;

                // Suspend layout for better performance
                top3Department_flp.SuspendLayout();

                int rank = 1;
                foreach (var dept in _topDepartments)
                {
                    AddDepartmentCard(dept.Department, rank++, dept.Item2);
                }

                top3Department_flp.ResumeLayout(false);
                top3Department_flp.PerformLayout();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ DisplayTopDepartments error: {ex.Message}");
            }
        }

        private void AddDepartmentCard(string departmentName, int rank, int borrowCount)
        {
            try
            {
                int cardWidth = Math.Min(330, Math.Max(220, top3Department_flp.Width - 24));
                if (cardWidth <= 0) cardWidth = 330; // fallback during init

                var card = new Panel
                {
                    Width = cardWidth,
                    Height = 84,
                    BackColor = Color.White,
                    Margin = new Padding(6),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                var accent = new Panel
                {
                    Width = 8,
                    Height = card.Height,
                    Left = 0,
                    Top = 0,
                    BackColor = rank == 1 ? Color.FromArgb(70, 60, 160) : Color.FromArgb(170, 170, 200),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
                };

                var rankCircle = new Panel
                {
                    Width = 36,
                    Height = 36,
                    Left = accent.Right + 12,
                    Top = 12,
                    BackColor = Color.FromArgb(70, 60, 160),
                    Margin = new Padding(0),
                };
                rankCircle.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(rankCircle.BackColor))
                    {
                        e.Graphics.FillEllipse(brush, 0, 0, rankCircle.Width - 1, rankCircle.Height - 1);
                    }
                };

                var rankLabel = new Label
                {
                    Text = $"#{rank}",
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                rankCircle.Controls.Add(rankLabel);

                var deptLabel = new Label
                {
                    Text = TruncateText(departmentName, 70),
                    Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    AutoSize = false,
                    Width = card.Width - (rankCircle.Right + 12) - 12,
                    Height = 40,
                    Left = rankCircle.Right + 8,
                    Top = 8,
                    TextAlign = ContentAlignment.TopLeft
                };

                var countLabel = new Label
                {
                    Text = $"{borrowCount} borrowed",
                    Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Width = deptLabel.Width,
                    Height = 18,
                    Left = deptLabel.Left,
                    Top = deptLabel.Bottom + 2,
                    TextAlign = ContentAlignment.TopLeft
                };

                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 250, 250);
                card.MouseLeave += (s, e) => card.BackColor = Color.White;

                EventHandler clickHandler = (s, e) =>
                {
                    MessageBox.Show($"{departmentName}\nTotal borrowed: {borrowCount}", "Department details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                card.Controls.Add(accent);
                card.Controls.Add(rankCircle);
                card.Controls.Add(deptLabel);
                card.Controls.Add(countLabel);

                card.Click += clickHandler;
                foreach (Control c in card.Controls) c.Click += clickHandler;

                top3Department_flp.Controls.Add(card);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddDepartmentCard error: {ex.Message}");
            }
        }

        // ==========================================
        // TOP 5 STUDENTS (2 per row)
        // ==========================================
        private void LoadTopStudents()
        {
            try
            {
                // Clear existing students before loading new ones
                _topStudents?.Clear();

                _topStudents = new List<(string, string, string)>
                {
                    ("Maria", "Santiago", "BEED"),
                    ("Andres", "Lopez", "BPS"),
                    ("Karen", "Delacruz", "BSCS"),
                    ("John", "Reyes", "BSED"),
                    ("Klent Dan O.", "Daluyon", "BSIT")
                };

                DisplayTopStudents();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ LoadTopStudents error: {ex.Message}");
            }
        }

        private void DisplayTopStudents()
        {
            try
            {
                DisposeImagesInContainer(top5MostStudentBorrowers_flp);
                top5MostStudentBorrowers_flp.Controls.Clear();
                top5MostStudentBorrowers_flp.Padding = new Padding(8);
                top5MostStudentBorrowers_flp.BackColor = Color.Transparent;
                top5MostStudentBorrowers_flp.FlowDirection = FlowDirection.LeftToRight;
                top5MostStudentBorrowers_flp.WrapContents = true;
                top5MostStudentBorrowers_flp.AutoScroll = false;

                // Suspend layout for better performance
                top5MostStudentBorrowers_flp.SuspendLayout();

                int cardMargin = 6;

                int availableWidth = top5MostStudentBorrowers_flp.ClientSize.Width;
                if (availableWidth <= 0) availableWidth = top5MostStudentBorrowers_flp.Width > 0 ? top5MostStudentBorrowers_flp.Width : 361;

                int horizontalPadding = top5MostStudentBorrowers_flp.Padding.Left + top5MostStudentBorrowers_flp.Padding.Right;
                int totalMarginsForTwoCards = cardMargin * 4;

                int cardWidth = (availableWidth - horizontalPadding - totalMarginsForTwoCards) / 2;
                cardWidth = Math.Max(140, Math.Min(170, cardWidth));

                int flowPanelHeight = top5MostStudentBorrowers_flp.ClientSize.Height;
                if (flowPanelHeight <= 0) flowPanelHeight = top5MostStudentBorrowers_flp.Height > 0 ? top5MostStudentBorrowers_flp.Height : 372;
                int rows = 3;
                int verticalPadding = top5MostStudentBorrowers_flp.Padding.Top + top5MostStudentBorrowers_flp.Padding.Bottom;
                int totalVerticalMargins = rows * (cardMargin * 2);
                int cardHeight = (flowPanelHeight - verticalPadding - totalVerticalMargins) / rows;
                cardHeight = Math.Max(84, Math.Min(100, cardHeight));

                int rank = 1;
                foreach (var s in _topStudents.Take(5))
                {
                    AddStudentCard(s.FirstName, s.LastName, s.DeptAcronym, rank++, cardWidth, cardHeight, cardMargin);
                }

                top5MostStudentBorrowers_flp.ResumeLayout(false);
                top5MostStudentBorrowers_flp.PerformLayout();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ DisplayTopStudents error: {ex.Message}");
            }
        }

        private void AddStudentCard(string firstName, string lastName, string deptAcronym, int rank, int cardWidth, int cardHeight, int cardMargin)
        {
            try
            {
                var card = new Panel
                {
                    Width = cardWidth,
                    Height = cardHeight,
                    BackColor = Color.White,
                    Margin = new Padding(cardMargin),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 250, 250);
                card.MouseLeave += (s, e) => card.BackColor = Color.White;

                var rankCircle = new Panel
                {
                    Width = 24,
                    Height = 24,
                    Left = 6,
                    Top = 6,
                    BackColor = Color.FromArgb(70, 60, 160)
                };
                rankCircle.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(rankCircle.BackColor))
                    {
                        e.Graphics.FillEllipse(brush, 0, 0, rankCircle.Width - 1, rankCircle.Height - 1);
                    }
                };
                var rankLabel = new Label
                {
                    Text = rank.ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold)
                };
                rankCircle.Controls.Add(rankLabel);

                int avatarSize = 44;
                var avatar = new PictureBox
                {
                    Width = avatarSize,
                    Height = avatarSize,
                    Left = 10,
                    Top = 8 + ((cardHeight - 8 * 2 - avatarSize) / 2),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };

                // Load default avatar or generate initials - wrap in try/catch and ensure result is a new Bitmap instance
                Image avatarImg = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(DefaultAvatarPath) && File.Exists(DefaultAvatarPath))
                    {
                        using (var fs = File.OpenRead(DefaultAvatarPath))
                        using (var img = Image.FromStream(fs))
                        using (var srcBmp = new Bitmap(img))
                        {
                            avatarImg = CreateCircularAvatarImage(srcBmp, new Size(avatarSize, avatarSize));
                        }
                    }
                    else
                    {
                        avatarImg = CreateInitialsAvatar(firstName, lastName, new Size(avatarSize, avatarSize));
                    }
                }
                catch
                {
                    try { avatarImg?.Dispose(); } catch { }
                    avatarImg = CreateInitialsAvatar(firstName, lastName, new Size(avatarSize, avatarSize));
                }

                avatar.Image = avatarImg;

                var nameLabel = new Label
                {
                    Text = $"{firstName} {lastName}",
                    AutoSize = false,
                    Width = card.Width - (avatar.Left + avatar.Width) - 14,
                    Height = 40,
                    Left = avatar.Right + 8,
                    Top = avatar.Top - 2,
                    Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    TextAlign = ContentAlignment.TopLeft,
                    AutoEllipsis = true
                };
                nameLabel.MaximumSize = new Size(nameLabel.Width, nameLabel.Height);

                var deptLabel = new Label
                {
                    Text = deptAcronym,
                    AutoSize = false,
                    Width = nameLabel.Width,
                    Height = 18,
                    Left = nameLabel.Left,
                    Top = nameLabel.Bottom - 2,
                    Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                EventHandler clickHandler = (s, e) =>
                {
                    MessageBox.Show($"{firstName} {lastName}\nDepartment: {deptAcronym}", "Student details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                card.Controls.Add(rankCircle);
                card.Controls.Add(avatar);
                card.Controls.Add(nameLabel);
                card.Controls.Add(deptLabel);

                card.Click += clickHandler;
                foreach (Control c in card.Controls) c.Click += clickHandler;

                top5MostStudentBorrowers_flp.Controls.Add(card);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddStudentCard error: {ex.Message}");
            }
        }

        // Produce circular avatar from rectangular image
        private Image CreateCircularAvatarImage(Image src, Size size)
        {
            if (src == null || size.Width <= 0 || size.Height <= 0)
            {
                return new Bitmap(1, 1);
            }

            try
            {
                var dest = new Bitmap(size.Width, size.Height);
                using (var g = Graphics.FromImage(dest))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(0, 0, size.Width - 1, size.Height - 1);
                        g.SetClip(path);
                        g.Clear(Color.Transparent);

                        var srcRect = GetScaledSourceRect(src.Size, size);
                        g.DrawImage(src, new Rectangle(0, 0, size.Width, size.Height), srcRect, GraphicsUnit.Pixel);
                    }
                }
                return dest;
            }
            catch
            {
                try { return CreateInitialsAvatar("", "", size); } catch { return new Bitmap(1, 1); }
            }
        }

        // Calculate source rectangle to crop/scale image while preserving aspect ratio (center-crop)
        private Rectangle GetScaledSourceRect(Size srcSize, Size destSize)
        {
            if (srcSize.Width <= 0 || srcSize.Height <= 0) return new Rectangle(0, 0, destSize.Width, destSize.Height);

            float srcRatio = (float)srcSize.Width / srcSize.Height;
            float destRatio = (float)destSize.Width / destSize.Height;

            if (srcRatio > destRatio)
            {
                int newWidth = (int)(srcSize.Height * destRatio);
                int x = Math.Max(0, (srcSize.Width - newWidth) / 2);
                return new Rectangle(x, 0, Math.Max(1, newWidth), srcSize.Height);
            }
            else
            {
                int newHeight = (int)(srcSize.Width / destRatio);
                int y = Math.Max(0, (srcSize.Height - newHeight) / 2);
                return new Rectangle(0, y, srcSize.Width, Math.Max(1, newHeight));
            }
        }

        // If no image, create colored circle with initials
        private Image CreateInitialsAvatar(string firstName, string lastName, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return new Bitmap(1, 1);

            string initials = GetInitials(firstName, lastName);
            var bmp = new Bitmap(size.Width, size.Height);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Color bg = Color.FromArgb(90, 80, 170);
                    using (var brush = new SolidBrush(bg))
                    {
                        g.FillEllipse(brush, 0, 0, size.Width - 1, size.Height - 1);
                    }

                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var brush = new SolidBrush(Color.White))
                    {
                        float fontSize = Math.Max(6f, Math.Min(size.Width / 2.2f, size.Height / 2.2f));
                        try
                        {
                            using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                            {
                                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                                g.DrawString(initials, font, brush, new RectangleF(0, 0, size.Width, size.Height), sf);
                            }
                        }
                        catch
                        {
                            using (var font = SystemFonts.DefaultFont)
                            {
                                g.DrawString(initials, font, brush, new RectangleF(0, 0, size.Width, size.Height), sf);
                            }
                        }
                    }
                }
            }
            catch
            {
                try { bmp.Dispose(); } catch { }
                return new Bitmap(1, 1);
            }

            return bmp;
        }

        private string GetInitials(string firstName, string lastName)
        {
            char? a = null, b = null;
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                var parts = firstName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && parts[0].Length > 0) a = char.ToUpperInvariant(parts[0][0]);
            }
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                var parts = lastName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && parts[0].Length > 0) b = char.ToUpperInvariant(parts[0][0]);
            }

            if (a == null && b == null) return "U";
            if (a == null) return b.ToString();
            if (b == null) return a.ToString();
            return string.Concat(a.Value, b.Value);
        }

        // ==========================================
        // SHOW DETAILS
        // ==========================================
        private void ShowBookDetails(Book book)
        {
            if (book == null) return;

            try
            {
                MessageBox.Show(
                    $"Title: {book.Title}\nAuthor: {book.Author}\nYear: {book.Year}",
                    "Book Details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ShowBookDetails error: {ex.Message}");
            }
        }

        private string TruncateText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > maxChars ? text.Substring(0, maxChars - 3) + "..." : text;
        }
    }
}