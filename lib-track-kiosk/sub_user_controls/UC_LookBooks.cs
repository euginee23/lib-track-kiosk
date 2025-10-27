using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using lib_track_kiosk.sub_forms; // for ViewSelectedBook
using lib_track_kiosk.helpers;   // for GetAllBooks / BookInfo
using lib_track_kiosk.models;    // for GroupedBook
using lib_track_kiosk.loading_forms; // for Loading

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookBooks : UserControl
    {
        // switched to grouped view so we show one card per batch_registration_key
        private List<GroupedBook> _groupedBooks = new List<GroupedBook>();

        // On-screen keyboard helper (reusable)
        private OnScreenKeyboard _osk;

        // track currently selected card so we can toggle selection visuals
        private Panel _selectedCard;

        public UC_LookBooks()
        {
            InitializeComponent();

            // instantiate keyboard helper (lazy alternative is possible)
            _osk = new OnScreenKeyboard();

            // wire up textbox focus/click handlers to open/close the keyboard
            if (search_txtBox != null)
            {
                search_txtBox.GotFocus += SearchTxtBox_GotFocus;
                search_txtBox.Enter += SearchTxtBox_GotFocus;
                search_txtBox.LostFocus += SearchTxtBox_LostFocus;
                search_txtBox.Click += SearchTxtBox_Click;
            }

            this.Disposed += UC_LookBooks_Disposed;

            TryEnableDoubleBuffer(books_FlowLayoutPanel);

            LoadBooks();
        }

        private void UC_LookBooks_Disposed(object sender, EventArgs e)
        {
            try
            {
                if (search_txtBox != null)
                {
                    search_txtBox.GotFocus -= SearchTxtBox_GotFocus;
                    search_txtBox.Enter -= SearchTxtBox_GotFocus;
                    search_txtBox.LostFocus -= SearchTxtBox_LostFocus;
                    search_txtBox.Click -= SearchTxtBox_Click;
                }
            }
            catch { /* ignore */ }

            try { _osk?.Dispose(); } catch { /* ignore */ }
        }

        private void SearchTxtBox_Click(object sender, EventArgs e) => TryOpenKeyboard();
        private void SearchTxtBox_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void SearchTxtBox_LostFocus(object sender, EventArgs e)
        {
            await Task.Delay(150);
            try { if (!this.ContainsFocus) TryCloseKeyboard(); } catch { }
        }

        private void TryOpenKeyboard() { try { _osk?.Open(); } catch { } }
        private void TryCloseKeyboard() { try { _osk?.Close(); } catch { } }

        /// <summary>
        /// Loads books from backend using the GetAllBooks helper.
        /// Groups returned copies by batch_registration_key so each group becomes one card.
        /// Shows the Loading form while the fetch runs, then closes it when finished (or on error).
        /// Falls back to local sample data if fetching fails or returns empty.
        /// </summary>
        private async void LoadBooks()
        {
            Loading loadingForm = null;

            // Try to show the loading form (modeless). We avoid blocking the UI thread.
            try
            {
                loadingForm = new Loading();

                // Optional: make the loading form appear centered over the parent form if available
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    // Show with parent so it stays on top of the main window
                    loadingForm.StartPosition = FormStartPosition.CenterParent;
                    loadingForm.Show(parentForm);
                }
                else
                {
                    loadingForm.StartPosition = FormStartPosition.CenterScreen;
                    loadingForm.Show();
                }

                // Ensure the loading form is painted immediately
                loadingForm.Refresh();
                Application.DoEvents();
            }
            catch
            {
                // If we fail to show the loading form for any reason, continue without it.
                try { loadingForm?.Dispose(); } catch { }
                loadingForm = null;
            }

            try
            {
                var fetched = await GetAllBooks.GetAllAsync();

                if (fetched != null && fetched.Count > 0)
                {
                    _groupedBooks = GroupByBatchKey(fetched);
                }
                else
                {
                    var samples = CreateSampleBooksFallback();
                    _groupedBooks = GroupByBatchKey(samples);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading books from API: {ex.Message}");
                var samples = CreateSampleBooksFallback();
                _groupedBooks = GroupByBatchKey(samples);
            }
            finally
            {
                // Close and dispose the loading form on the UI thread safely
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

            DisplayBooks();
        }

        private List<GroupedBook> GroupByBatchKey(List<BookInfo> books)
        {
            var grouped = books.GroupBy(b =>
                string.IsNullOrEmpty(b.BatchRegistrationKey)
                    ? $"__single__{b.BookId}"
                    : b.BatchRegistrationKey);

            var result = new List<GroupedBook>();

            foreach (var g in grouped)
            {
                var key = g.Key;
                var representative = g.FirstOrDefault(b => b.CoverImage != null) ?? g.First();
                var statusCounts = g
                    .GroupBy(b => string.IsNullOrEmpty(b.Status) ? "Unknown" : b.Status)
                    .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

                int total = g.Count();
                int available = g.Count(b => string.Equals(b.Status, "Available", StringComparison.OrdinalIgnoreCase));

                representative.AvailableCopies = available;

                var gb = new GroupedBook
                {
                    BatchKey = key,
                    Representative = representative,
                    TotalCopies = total,
                    AvailableCopies = available,
                    StatusCounts = statusCounts,
                    Copies = g.ToList()
                };

                result.Add(gb);
            }

            // keep ordering consistent (newest by BookId desc)
            return result.OrderByDescending(r => r.Representative.BookId).ToList();
        }

        private List<BookInfo> CreateSampleBooksFallback()
        {
            string path1 = @"E:\Library-Tracker\dev files\input samples\book\qwe.jpg";
            string path2 = @"E:\Library-Tracker\dev files\input samples\book\qwer.jpg";
            string path3 = @"E:\Library-Tracker\dev files\input samples\book\qwert.jpg";

            var sample = new List<BookInfo>
            {
                new BookInfo { BookId = 1, Title = "C# Programming...", Author = "John Doe", Year = "2021", CoverImage = LoadImageIfExists(path1) },
                new BookInfo { BookId = 2, Title = "Learning AI...", Author = "Jane Smith", Year = "2023", CoverImage = LoadImageIfExists(path2) },
                new BookInfo { BookId = 3, Title = "Database Systems...", Author = "Andrew Tanenbaum", Year = "2020", CoverImage = LoadImageIfExists(path3) },
                // ... other fallback items
            };

            return sample;
        }

        private Image LoadImageIfExists(string path)
        {
            if (File.Exists(path))
            {
                try { return Image.FromFile(path); } catch { }
            }
            return null;
        }

        private void DisplayBooks()
        {
            books_FlowLayoutPanel.Controls.Clear();
            books_FlowLayoutPanel.AutoScroll = true;

            foreach (var gb in _groupedBooks)
                AddBookCard(gb);
        }

        private void AddBookCard(GroupedBook gb)
        {
            var book = gb.Representative;

            int cardWidth = 240;
            int cardHeight = 420;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                BackColor = Color.White,
                Margin = new Padding(20),
                Tag = gb.BatchKey ?? (object)book.BookId,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

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
                foreach (Control ctrl in books_FlowLayoutPanel.Controls)
                {
                    if (ctrl is Panel pnl)
                        SetCardSelectedVisual(pnl, false);
                }

                _selectedCard = card;
                SetCardSelectedVisual(card, true);

                OpenViewSelectedBook(gb);
            }

            PictureBox cover = new PictureBox
            {
                Width = 210,
                Height = 270,
                Left = 15,
                Top = 15,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            if (book.CoverImage != null)
            {
                try { cover.Image = (Image)book.CoverImage.Clone(); } catch { cover.Image = book.CoverImage; }
            }

            Label copiesBadge = new Label
            {
                AutoSize = false,
                Width = 110,
                Height = 24,
                Left = cardWidth - 125,
                Top = 8,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Text = $"{gb.AvailableCopies}/{gb.TotalCopies} Available"
            };

            if (gb.AvailableCopies == 0)
            {
                copiesBadge.BackColor = Color.FromArgb(220, 53, 69);
                copiesBadge.Text = $"0/{gb.TotalCopies} Available";
            }

            Label title = new Label
            {
                AutoSize = false,
                Width = 210,
                Height = 48,
                Left = 15,
                Top = 295,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.TopCenter,
                Text = TruncateText(book.Title, 70)
            };

            Label author = new Label
            {
                AutoSize = false,
                Width = 210,
                Height = 38,
                Left = 15,
                Top = 345,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.TopCenter,
                Text = TruncateText(book.Author, 60)
            };

            Label year = new Label
            {
                AutoSize = false,
                Width = 210,
                Height = 20,
                Left = 15,
                Top = 390,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"📅 {book.Year}"
            };

            card.Controls.Add(cover);
            card.Controls.Add(copiesBadge);
            card.Controls.Add(title);
            card.Controls.Add(author);
            card.Controls.Add(year);

            try
            {
                var tooltip = new ToolTip();
                var parts = gb.StatusCounts.OrderByDescending(kv => kv.Value)
                              .Select(kv => $"{kv.Key}: {kv.Value}");
                string tooltipText = string.Join("  •  ", parts);
                tooltip.SetToolTip(card, tooltipText);
            }
            catch { /* ignore */ }

            Action<Control> attachClickRecursively = null;
            attachClickRecursively = (ctrl) =>
            {
                ctrl.Click += Card_Click;
                foreach (Control child in ctrl.Controls)
                    attachClickRecursively(child);
            };

            attachClickRecursively(card);
            card.Click += Card_Click;

            books_FlowLayoutPanel.Controls.Add(card);
        }

        private void OpenViewSelectedBook(GroupedBook gb)
        {
            try
            {
                using (var view = new ViewSelectedBook(gb))
                {
                    view.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open book viewer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void scrollUp_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 420;
            var scroll = books_FlowLayoutPanel.VerticalScroll;
            int newValue = Math.Max(scroll.Minimum, scroll.Value - scrollAmount);
            scroll.Value = newValue;
            books_FlowLayoutPanel.PerformLayout();
        }

        private void scrollDown_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 420;
            var scroll = books_FlowLayoutPanel.VerticalScroll;
            int newValue = Math.Min(scroll.Maximum, scroll.Value + scrollAmount);
            scroll.Value = newValue;
            books_FlowLayoutPanel.PerformLayout();
        }

        private string TruncateText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > maxChars ? text.Substring(0, maxChars - 3) + "..." : text;
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
    }
}