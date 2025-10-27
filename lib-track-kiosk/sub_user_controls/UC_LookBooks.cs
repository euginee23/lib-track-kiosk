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

        // Keep the original (unfiltered) grouped list so we can reapply filters
        private List<GroupedBook> _allGroupedBooks = new List<GroupedBook>();

        // Keep raw fetched book items (useful for building filters)
        private List<BookInfo> _allBooks = new List<BookInfo>();

        // On-screen keyboard helper (reusable)
        private OnScreenKeyboard _osk;

        // track currently selected card so we can toggle selection visuals
        private Panel _selectedCard;

        // Debounce timer for search textbox to avoid filtering on every keystroke immediately
        private System.Windows.Forms.Timer _searchDebounceTimer;

        public UC_LookBooks()
        {
            InitializeComponent();

            // instantiate keyboard helper (lazy alternative is possible)
            _osk = new OnScreenKeyboard();

            // ensure search textbox reference exists; try to find it if designer didn't set field
            TryResolveSearchTextBox();

            // wire up textbox focus/click handlers to open/close the keyboard
            if (search_txtBox != null)
            {
                search_txtBox.GotFocus += SearchTxtBox_GotFocus;
                search_txtBox.Enter += SearchTxtBox_GotFocus;
                search_txtBox.LostFocus += SearchTxtBox_LostFocus;
                search_txtBox.Click += SearchTxtBox_Click;

                // Use debounced text changed to avoid excessive UI updates while typing
                search_txtBox.TextChanged -= SearchTxtBox_TextChanged;
                search_txtBox.TextChanged += SearchTxtBox_TextChanged;

                // handle Enter key to run search immediately
                search_txtBox.KeyDown += SearchTxtBox_KeyDown;

                // ensure single-line textbox doesn't accept return; suppress default beep
                search_txtBox.AcceptsReturn = false;
            }

            // setup debounce timer
            _searchDebounceTimer = new System.Windows.Forms.Timer();
            _searchDebounceTimer.Interval = 300; // ms
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            // wire combo boxes (if they exist) for filter changes
            if (author_cmbx != null)
                author_cmbx.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            if (genre_cmbx != null)
                genre_cmbx.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

            this.Disposed += UC_LookBooks_Disposed;

            TryEnableDoubleBuffer(books_FlowLayoutPanel);

            LoadBooks();
        }

        private void TryResolveSearchTextBox()
        {
            // If the designer field is null for any reason, attempt to find control by name in the control tree.
            try
            {
                if (search_txtBox == null)
                {
                    var found = this.Controls.Find("search_txtBox", true).FirstOrDefault() as TextBox;
                    if (found != null)
                        search_txtBox = found;
                }
            }
            catch { /* ignore */ }
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
                    search_txtBox.TextChanged -= SearchTxtBox_TextChanged;
                    search_txtBox.KeyDown -= SearchTxtBox_KeyDown;
                }
            }
            catch { /* ignore */ }

            try
            {
                if (author_cmbx != null) author_cmbx.SelectedIndexChanged -= FilterComboBox_SelectedIndexChanged;
                if (genre_cmbx != null) genre_cmbx.SelectedIndexChanged -= FilterComboBox_SelectedIndexChanged;
            }
            catch { /* ignore */ }

            try { _osk?.Dispose(); } catch { /* ignore */ }

            try
            {
                if (_searchDebounceTimer != null)
                {
                    _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Dispose();
                    _searchDebounceTimer = null;
                }
            }
            catch { /* ignore */ }
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

        // Debounced TextChanged handler: restart the timer, actual filtering fires from timer tick
        private void SearchTxtBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_searchDebounceTimer == null)
                {
                    ApplyFilters();
                    return;
                }

                // restart debounce
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ SearchTxtBox_TextChanged error: {ex.Message}");
            }
        }

        // KeyDown handler: if user presses Enter, stop debounce and apply filters immediately
        private void SearchTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e == null) return;
                if (e.KeyCode == Keys.Enter)
                {
                    // prevent ding sound and prevent newline (if multiline)
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    _searchDebounceTimer?.Stop();
                    ApplyFilters();

                    // keep focus in textbox so user can continue typing
                    try { search_txtBox.Focus(); } catch { }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    // common UX: Escape clears query
                    _searchDebounceTimer?.Stop();
                    if (search_txtBox != null && !string.IsNullOrEmpty(search_txtBox.Text))
                    {
                        search_txtBox.Text = string.Empty;
                        ApplyFilters();
                    }
                }
            }
            catch { /* ignore */ }
        }

        // Timer tick: apply filters once typing pauses
        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer?.Stop();
                ApplyFilters();
            }
            catch { /* ignore */ }
        }

        // common handler for author/genre combobox changes
        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Loads books from backend using the GetAllBooks helper.
        /// Groups returned copies by batch_registration_key so each group becomes one card.
        /// Shows the Loading form while the fetch runs, then closes it when finished (or on error).
        /// Falls back to local sample data if fetching fails or returns empty.
        /// Also populates the author and genre combo boxes for filtering.
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
                    _allBooks = fetched;
                    _groupedBooks = GroupByBatchKey(fetched);
                }
                else
                {
                    var samples = CreateSampleBooksFallback();
                    _allBooks = samples;
                    _groupedBooks = GroupByBatchKey(samples);
                }

                // keep an unfiltered copy
                _allGroupedBooks = _groupedBooks.Select(g => g).ToList();

                // populate filter comboboxes based on fetched data
                PopulateAuthorAndGenreFilters();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading books from API: {ex.Message}");
                var samples = CreateSampleBooksFallback();
                _allBooks = samples;
                _groupedBooks = GroupByBatchKey(samples);
                _allGroupedBooks = _groupedBooks.Select(g => g).ToList();

                PopulateAuthorAndGenreFilters();
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

        /// <summary>
        /// Populate author_cmbx with unique authors and genre_cmbx with unique genres (including department names
        /// when a book uses department as genre). Adds "All" entry as index 0.
        /// Uses a reflection-safe approach so this will not throw if the BookInfo class does not contain optional fields.
        /// </summary>
        private void PopulateAuthorAndGenreFilters()
        {
            try
            {
                // Authors: collect unique author strings from raw books (Author field may contain single name)
                var authors = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var b in _allBooks)
                {
                    if (!string.IsNullOrWhiteSpace(b.Author))
                    {
                        // if b.Author contains comma-separated names, add each separately
                        var parts = b.Author.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(p => p.Trim())
                                            .Where(p => !string.IsNullOrEmpty(p));
                        foreach (var p in parts) authors.Add(p);
                    }
                }

                // Genres: we must consider that genre may come from book_genre or from departments depending on isUsingDepartment.
                // Use reflection to look for possible properties on BookInfo that the API may provide.
                var bookInfoType = typeof(BookInfo);
                var isUsingDeptProp = bookInfoType.GetProperty("IsUsingDepartment", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var genreNameProps = new[]
                {
                    bookInfoType.GetProperty("Genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    bookInfoType.GetProperty("genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    bookInfoType.GetProperty("BookGenre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    bookInfoType.GetProperty("book_genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    bookInfoType.GetProperty("DepartmentName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    bookInfoType.GetProperty("department_name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                }.Where(p => p != null).ToArray();

                var genres = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var b in _allBooks)
                {
                    string genreName = null;

                    // If the object exposes 'IsUsingDepartment' and it's truthy, prefer department name fields
                    bool isUsingDept = false;
                    if (isUsingDeptProp != null)
                    {
                        try
                        {
                            var val = isUsingDeptProp.GetValue(b);
                            if (val != null)
                            {
                                // support bool/int/string returned values
                                if (val is bool vb) isUsingDept = vb;
                                else if (val is int vi) isUsingDept = vi == 1;
                                else if (int.TryParse(val.ToString(), out var vpi)) isUsingDept = vpi == 1;
                                else if (bool.TryParse(val.ToString(), out var vpb)) isUsingDept = vpb;
                            }
                        }
                        catch { /* ignore */ }
                    }

                    // Try preferred properties in order: if department usage is true, prefer department name props first
                    if (isUsingDept)
                    {
                        // attempt to locate department name properties explicitly
                        var deptNameProp = bookInfoType.GetProperty("DepartmentName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                        ?? bookInfoType.GetProperty("department_name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (deptNameProp != null)
                        {
                            try { genreName = deptNameProp.GetValue(b)?.ToString(); } catch { /* ignore */ }
                        }
                    }

                    // fallback: try any genre-like property found earlier
                    if (string.IsNullOrWhiteSpace(genreName))
                    {
                        foreach (var p in genreNameProps)
                        {
                            try
                            {
                                var gv = p.GetValue(b);
                                if (gv != null)
                                {
                                    genreName = gv.ToString();
                                    if (!string.IsNullOrWhiteSpace(genreName)) break;
                                }
                            }
                            catch { /* ignore */ }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(genreName))
                        genres.Add(genreName.Trim());
                }

                // Populate the ComboBoxes on UI thread
                Action populate = () =>
                {
                    if (author_cmbx != null)
                    {
                        author_cmbx.Items.Clear();
                        author_cmbx.Items.Add("All Authors");
                        foreach (var a in authors) author_cmbx.Items.Add(a);
                        author_cmbx.SelectedIndex = 0;
                    }

                    if (genre_cmbx != null)
                    {
                        genre_cmbx.Items.Clear();
                        genre_cmbx.Items.Add("All Genres");
                        foreach (var g in genres) genre_cmbx.Items.Add(g);
                        genre_cmbx.SelectedIndex = 0;
                    }
                };

                if (this.InvokeRequired) this.Invoke(populate); else populate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to populate author/genre filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Reapply active filters (author combobox, genre combobox, and search text) to the grouped books.
        /// After filtering, update the displayed cards.
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                // start from the full grouped list
                _groupedBooks = _allGroupedBooks.Select(g => g).ToList();

                // author filter
                string selectedAuthor = null;
                if (author_cmbx != null && author_cmbx.SelectedItem != null)
                {
                    selectedAuthor = author_cmbx.SelectedItem.ToString();
                    if (string.Equals(selectedAuthor, "All Authors", StringComparison.OrdinalIgnoreCase))
                        selectedAuthor = null;
                }

                // genre filter
                string selectedGenre = null;
                if (genre_cmbx != null && genre_cmbx.SelectedItem != null)
                {
                    selectedGenre = genre_cmbx.SelectedItem.ToString();
                    if (string.Equals(selectedGenre, "All Genres", StringComparison.OrdinalIgnoreCase))
                        selectedGenre = null;
                }

                // search query
                string query = null;
                try
                {
                    query = search_txtBox?.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(query)) query = null;
                }
                catch { query = null; }

                if (selectedAuthor != null)
                {
                    _groupedBooks = _groupedBooks.Where(g =>
                        g.Copies != null && g.Copies.Any(b =>
                            !string.IsNullOrWhiteSpace(b.Author) &&
                            b.Author.IndexOf(selectedAuthor, StringComparison.OrdinalIgnoreCase) >= 0
                        )
                    ).ToList();
                }

                if (selectedGenre != null)
                {
                    // Use reflection-based genre extraction per book (same logic as PopulateAuthorAndGenreFilters)
                    _groupedBooks = _groupedBooks.Where(g =>
                        g.Copies != null && g.Copies.Any(b =>
                        {
                            var genreName = ExtractGenreNameFromBookInfo(b);
                            return !string.IsNullOrWhiteSpace(genreName) &&
                                   genreName.IndexOf(selectedGenre, StringComparison.OrdinalIgnoreCase) >= 0;
                        })
                    ).ToList();
                }

                if (query != null)
                {
                    var q = query;
                    _groupedBooks = _groupedBooks.Where(g =>
                        // check representative title/author/year or any copy's fields / maybe abstract if exists
                        (g.Representative != null &&
                            (
                                (!string.IsNullOrWhiteSpace(g.Representative.Title) && g.Representative.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Author) && g.Representative.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Status) && g.Representative.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Year) && g.Representative.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                            )
                        ) ||
                        // check every copy for more matches (including potential abstract/description fields if present)
                        (g.Copies != null && g.Copies.Any(b =>
                            (!string.IsNullOrWhiteSpace(b.Title) && b.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Author) && b.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Status) && b.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Year) && b.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        ))
                    ).ToList();
                }

                // refresh display
                DisplayBooks();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error applying filters: {ex.Message}");
            }
        }

        // Attempts to extract a genre/department name from a BookInfo instance using reflection.
        // Returns null when not available.
        private string ExtractGenreNameFromBookInfo(BookInfo b)
        {
            if (b == null) return null;
            try
            {
                var t = typeof(BookInfo);

                // If IsUsingDepartment is present and true, prefer department name properties
                var isUsingDeptProp = t.GetProperty("IsUsingDepartment", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                bool isUsingDept = false;
                if (isUsingDeptProp != null)
                {
                    try
                    {
                        var v = isUsingDeptProp.GetValue(b);
                        if (v is bool vb) isUsingDept = vb;
                        else if (v is int vi) isUsingDept = vi == 1;
                        else if (int.TryParse(v?.ToString(), out var vpi)) isUsingDept = vpi == 1;
                        else if (bool.TryParse(v?.ToString(), out var vpb)) isUsingDept = vpb;
                    }
                    catch { /* ignore */ }
                }

                if (isUsingDept)
                {
                    var deptProp = t.GetProperty("DepartmentName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                ?? t.GetProperty("department_name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (deptProp != null)
                    {
                        var dv = deptProp.GetValue(b);
                        if (dv != null) return dv.ToString();
                    }
                }

                // Try a set of likely genre property names
                var candidates = new[]
                {
                    t.GetProperty("Genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    t.GetProperty("genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    t.GetProperty("BookGenre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                    t.GetProperty("book_genre", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase),
                }.Where(p => p != null);

                foreach (var p in candidates)
                {
                    try
                    {
                        var gv = p.GetValue(b);
                        if (gv != null)
                        {
                            var s = gv.ToString();
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            return null;
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

            // if after applying filters there are no cards, show a friendly message
            if (_groupedBooks == null || _groupedBooks.Count == 0)
                AddNoResultsMessageToFlow();
        }

        private void AddNoResultsMessageToFlow()
        {
            try
            {
                var panel = new Panel
                {
                    Width = Math.Max(200, books_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 40),
                    Height = 120,
                    Margin = new Padding(20),
                    BackColor = Color.Transparent
                };

                var lbl = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Text = "No books match the current filters.\nTry a different search, author, or genre."
                };

                panel.Controls.Add(lbl);

                if (books_FlowLayoutPanel.InvokeRequired)
                    books_FlowLayoutPanel.Invoke((Action)(() => books_FlowLayoutPanel.Controls.Add(panel)));
                else
                    books_FlowLayoutPanel.Controls.Add(panel);
            }
            catch { /* ignore */ }
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