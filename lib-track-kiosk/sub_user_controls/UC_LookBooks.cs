using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using lib_track_kiosk.sub_forms; // for ViewSelectedBook
using lib_track_kiosk.helpers;   // for GetAllBooks / BookInfo
using lib_track_kiosk.models;    // for GroupedBook
using lib_track_kiosk.loading_forms; // for Loading
using lib_track_kiosk.caching; // SharedBookCache

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

        // Shared tooltip (reused per-card to avoid creating many ToolTip objects)
        private readonly ToolTip _cardToolTip;

        // Cancellation token to cancel per-control async operations when disposing
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public UC_LookBooks()
        {
            InitializeComponent();

            // instantiate keyboard helper
            _osk = new OnScreenKeyboard();

            // shared tooltip
            _cardToolTip = new ToolTip { ShowAlways = true };

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

            // Load books from shared cache (or fetch once) asynchronously
            _ = LoadBooksAsync();
        }

        private void TryResolveSearchTextBox()
        {
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
                }
            }
            catch { /* ignore */ }

            try { _cardToolTip?.Dispose(); } catch { /* ignore */ }

            // Cancel per-control async work
            try { _cts.Cancel(); } catch { /* ignore */ }
            try { _cts.Dispose(); } catch { /* ignore */ }

            // Clear flow panel controls (disposing images & controls)
            try { ClearBooksFlowPanel(); } catch { /* ignore */ }
        }

        private void SearchTxtBox_Click(object sender, EventArgs e) => TryOpenKeyboard();
        private void SearchTxtBox_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void SearchTxtBox_LostFocus(object sender, EventArgs e)
        {
            await Task.Delay(150).ConfigureAwait(false);
            try { if (!this.ContainsFocus) TryCloseKeyboard(); } catch { }
        }

        private void TryOpenKeyboard() { try { _osk?.Open(); } catch { } }
        private void TryCloseKeyboard() { try { _osk?.Close(); } catch { } }

        private void SearchTxtBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_searchDebounceTimer == null)
                {
                    ApplyFilters();
                    return;
                }

                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ SearchTxtBox_TextChanged error: {ex.Message}");
            }
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
                    ApplyFilters();

                    try { search_txtBox.Focus(); } catch { }
                }
                else if (e.KeyCode == Keys.Escape)
                {
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

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer?.Stop();
                ApplyFilters();
            }
            catch { /* ignore */ }
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Loads books from the shared cache using SharedBookCache.GetOrFetchBooksAsync.
        /// The first control that loads will fetch from API; subsequent controls will reuse the cached list.
        /// </summary>
        private async Task LoadBooksAsync()
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
            catch { try { loadingForm?.Dispose(); } catch { } loadingForm = null; }

            try
            {
                // Use the shared cache to avoid re-fetching on reload/navigation
                var fetched = await SharedBookCache.GetOrFetchBooksAsync(() => GetAllBooks.GetAllAsync()).ConfigureAwait(false);

                if (fetched != null && fetched.Count > 0)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke((Action)(() =>
                        {
                            _allBooks = fetched;
                            _groupedBooks = GroupByBatchKey(fetched);
                            _allGroupedBooks = _groupedBooks.Select(g => g).ToList();
                            PopulateAuthorAndGenreFilters();
                            DisplayBooks();
                            StartLazyDownloadForVisibleCovers();
                        }));
                    }
                    else
                    {
                        _allBooks = fetched;
                        _groupedBooks = GroupByBatchKey(fetched);
                        _allGroupedBooks = _groupedBooks.Select(g => g).ToList();
                        PopulateAuthorAndGenreFilters();
                        DisplayBooks();
                        StartLazyDownloadForVisibleCovers();
                    }
                }
                else
                {
                    _allBooks = new List<BookInfo>();
                    _groupedBooks = new List<GroupedBook>();
                    _allGroupedBooks = new List<GroupedBook>();
                    PopulateAuthorAndGenreFilters();
                    DisplayBooks();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading books from API/cache: {ex.Message}");
                _allBooks = new List<BookInfo>();
                _groupedBooks = new List<GroupedBook>();
                _allGroupedBooks = new List<GroupedBook>();
                PopulateAuthorAndGenreFilters();
                DisplayBooks();
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
                    catch { /* ignore */ }
                }
            }
        }

        // Lazy download pipeline using shared image cache so downloads aren't repeated across UC instances
        private void StartLazyDownloadForVisibleCovers()
        {
            try
            {
                var token = _cts.Token;

                foreach (Control ctrl in books_FlowLayoutPanel.Controls)
                {
                    if (!(ctrl is Panel card)) continue;

                    var pictureBox = FindPictureBoxInCard(card);
                    if (pictureBox == null) continue;

                    if (pictureBox.Image != null) continue; // already set

                    // find representative BookInfo for card
                    BookInfo rep = null;
                    var tag = card.Tag;
                    if (tag != null)
                    {
                        string batchKey = tag as string;
                        if (!string.IsNullOrWhiteSpace(batchKey) && !batchKey.StartsWith("__single__"))
                        {
                            rep = _allBooks.FirstOrDefault(b => b.BatchRegistrationKey == batchKey);
                        }
                        else
                        {
                            if (tag is int bid)
                                rep = _allBooks.FirstOrDefault(b => b.BookId == bid);
                            else if (int.TryParse(tag?.ToString() ?? "", out int parsedId))
                                rep = _allBooks.FirstOrDefault(b => b.BookId == parsedId);
                        }
                    }

                    if (rep == null) continue;

                    // If inline CoverImage present use it immediately
                    if (rep.CoverImage != null)
                    {
                        try { pictureBox.Image = (Image)rep.CoverImage.Clone(); } catch { pictureBox.Image = rep.CoverImage; }
                        continue;
                    }

                    // extract and normalize URL
                    var rawUrl = ExtractCoverUrlFromBookInfo(rep);
                    if (string.IsNullOrWhiteSpace(rawUrl)) continue;

                    var url = NormalizeCoverUrl(rawUrl);
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    // Spawn a fire-and-forget task that uses the SHARED cache to download (or reuse) the image
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var img = await SharedBookCache.GetOrDownloadImageAsync(url, token).ConfigureAwait(false);
                            if (img == null) return;

                            // assign to picture box on UI thread, only if it's still valid
                            if (pictureBox == null) return;
                            if (pictureBox.IsDisposed || pictureBox.Disposing) return;

                            if (pictureBox.InvokeRequired)
                            {
                                pictureBox.Invoke((Action)(() => TrySetPictureBoxImageSafely(pictureBox, img)));
                            }
                            else
                            {
                                TrySetPictureBoxImageSafely(pictureBox, img);
                            }

                            // Cache on model (optional)
                            try { rep.CoverImage = img; } catch { /* ignore */ }
                        }
                        catch (OperationCanceledException) { /* ignore */ }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Exception in cover download task: {ex.Message}");
                        }
                    }, token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ StartLazyDownloadForVisibleCovers error: {ex.Message}");
            }
        }

        private void TrySetPictureBoxImageSafely(PictureBox pictureBox, Image img)
        {
            if (pictureBox == null) return;
            if (pictureBox.IsDisposed || pictureBox.Disposing) return;
            try
            {
                var old = pictureBox.Image;
                pictureBox.Image = (Image)img.Clone();
                try { old?.Dispose(); } catch { }
            }
            catch
            {
                try { pictureBox.Image = img; } catch { }
            }
        }

        // Finds the PictureBox control inside a card panel (assumes the first PictureBox in Controls)
        private PictureBox FindPictureBoxInCard(Panel card)
        {
            if (card == null) return null;
            foreach (Control c in card.Controls)
            {
                if (c is PictureBox pb) return pb;
            }
            return null;
        }

        // Normalize cover URL: if relative, prefix with uploads base derived from API_Backend or BaseUrl
        private string NormalizeCoverUrl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return raw;

            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return raw; // data URI (not expected)

            try
            {
                var prop = typeof(API_Backend).GetProperty("UploadDomain", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                string baseUploads = null;
                if (prop != null)
                    baseUploads = prop.GetValue(null) as string;

                if (string.IsNullOrWhiteSpace(baseUploads))
                {
                    baseUploads = API_Backend.BaseUrl;
                    if (!string.IsNullOrWhiteSpace(baseUploads))
                    {
                        var idx = baseUploads.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
                        if (idx > 0) baseUploads = baseUploads.Substring(0, idx);
                    }
                }

                if (!string.IsNullOrWhiteSpace(baseUploads))
                {
                    baseUploads = baseUploads.TrimEnd('/');
                    var rel = raw.StartsWith("/") ? raw : "/" + raw;
                    return baseUploads + rel;
                }
            }
            catch { /* ignore */ }

            return raw;
        }

        // Attempt to extract a cover URL from BookInfo using common property names
        private string ExtractCoverUrlFromBookInfo(BookInfo b)
        {
            if (b == null) return null;
            try
            {
                var t = b.GetType();

                var explicitProps = new[] { "CoverImageUrl", "CoverUrl", "ImageUrl", "book_cover", "bookCover" };
                foreach (var name in explicitProps)
                {
                    try
                    {
                        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (prop != null)
                        {
                            var val = prop.GetValue(b);
                            if (val != null)
                            {
                                var s = val.ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(s)) return s;
                            }
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            return null;
        }

        // --- Fix: provide ExtractGenreNameFromBookInfo used by ApplyFilters ---
        private string ExtractGenreNameFromBookInfo(BookInfo b)
        {
            if (b == null) return null;
            try
            {
                var t = typeof(BookInfo);
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
                        try
                        {
                            var dv = deptProp.GetValue(b);
                            if (dv != null)
                                return dv.ToString();
                        }
                        catch { /* ignore */ }
                    }
                }

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
        // --- end fix ---

        // Populate author_cmbx with unique authors and genre_cmbx with unique genres (including department names
        // when a book uses department as genre). Adds "All" entry as index 0.
        private void PopulateAuthorAndGenreFilters()
        {
            try
            {
                var authors = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var b in _allBooks)
                {
                    if (!string.IsNullOrWhiteSpace(b.Author))
                    {
                        var parts = b.Author.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(p => p.Trim())
                                            .Where(p => !string.IsNullOrEmpty(p));
                        foreach (var p in parts) authors.Add(p);
                    }
                }

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

                    bool isUsingDept = false;
                    if (isUsingDeptProp != null)
                    {
                        try
                        {
                            var val = isUsingDeptProp.GetValue(b);
                            if (val is bool vb) isUsingDept = vb;
                            else if (val is int vi) isUsingDept = vi == 1;
                            else if (int.TryParse(val?.ToString(), out var vpi)) isUsingDept = vpi == 1;
                            else if (bool.TryParse(val?.ToString(), out var vpb)) isUsingDept = vpb;
                        }
                        catch { /* ignore */ }
                    }

                    if (isUsingDept)
                    {
                        var deptNameProp = bookInfoType.GetProperty("DepartmentName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                        ?? bookInfoType.GetProperty("department_name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (deptNameProp != null)
                        {
                            try { genreName = deptNameProp.GetValue(b)?.ToString(); } catch { /* ignore */ }
                        }
                    }

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

        private void ApplyFilters()
        {
            try
            {
                _groupedBooks = _allGroupedBooks.Select(g => g).ToList();

                string selectedAuthor = null;
                if (author_cmbx != null && author_cmbx.SelectedItem != null)
                {
                    selectedAuthor = author_cmbx.SelectedItem.ToString();
                    if (string.Equals(selectedAuthor, "All Authors", StringComparison.OrdinalIgnoreCase))
                        selectedAuthor = null;
                }

                string selectedGenre = null;
                if (genre_cmbx != null && genre_cmbx.SelectedItem != null)
                {
                    selectedGenre = genre_cmbx.SelectedItem.ToString();
                    if (string.Equals(selectedGenre, "All Genres", StringComparison.OrdinalIgnoreCase))
                        selectedGenre = null;
                }

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
                        (g.Representative != null &&
                            (
                                (!string.IsNullOrWhiteSpace(g.Representative.Title) && g.Representative.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Author) && g.Representative.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Status) && g.Representative.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Year) && g.Representative.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                            )
                        ) ||
                        (g.Copies != null && g.Copies.Any(b =>
                            (!string.IsNullOrWhiteSpace(b.Title) && b.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Author) && b.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Status) && b.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Year) && b.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        ))
                    ).ToList();
                }

                DisplayBooks();
                StartLazyDownloadForVisibleCovers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error applying filters: {ex.Message}");
            }
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

            return result.OrderByDescending(r => r.Representative.BookId).ToList();
        }

        private List<BookInfo> CreateSampleBooksFallback()
        {
            var sample = new List<BookInfo>
            {
                new BookInfo { BookId = 1, Title = "Sample Book A", Author = "Author A", Year = "2021" },
                new BookInfo { BookId = 2, Title = "Sample Book B", Author = "Author B", Year = "2022" }
            };

            return sample;
        }

        private void DisplayBooks()
        {
            // Dispose existing controls and their images properly to avoid handle leaks
            ClearBooksFlowPanel();
            books_FlowLayoutPanel.AutoScroll = true;

            // Suspend layout while adding many controls
            books_FlowLayoutPanel.SuspendLayout();
            try
            {
                foreach (var gb in _groupedBooks)
                    AddBookCard(gb);

                if (_groupedBooks == null || _groupedBooks.Count == 0)
                    AddNoResultsMessageToFlow();
            }
            finally
            {
                books_FlowLayoutPanel.ResumeLayout(false);
                books_FlowLayoutPanel.PerformLayout();
            }
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
            var book = gb.Representative ?? new BookInfo();

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
                var parts = gb.StatusCounts.OrderByDescending(kv => kv.Value)
                              .Select(kv => $"{kv.Key}: {kv.Value}");
                string tooltipText = string.Join("  •  ", parts);

                // Use shared tooltip instance
                _cardToolTip?.SetToolTip(card, tooltipText);
            }
            catch { /* ignore */ }

            Action<Control> attachClickRecursively = null;
            attachClickRecursively = (ctrl) =>
            {
                try
                {
                    ctrl.Click += Card_Click;
                    foreach (Control child in ctrl.Controls)
                        attachClickRecursively(child);
                }
                catch { /* ignore */ }
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

        // Safely dispose and remove all controls from the flow panel
        private void ClearBooksFlowPanel()
        {
            if (books_FlowLayoutPanel == null) return;
            try
            {
                books_FlowLayoutPanel.SuspendLayout();

                var controls = books_FlowLayoutPanel.Controls.Cast<Control>().ToArray();
                foreach (var c in controls)
                {
                    try
                    {
                        books_FlowLayoutPanel.Controls.Remove(c);
                        DisposeControlAndChildren(c);
                    }
                    catch { /* ignore per-control errors */ }
                }
            }
            catch { /* ignore */ }
            finally
            {
                try { books_FlowLayoutPanel.ResumeLayout(false); } catch { }
            }
        }

        // Recursively dispose children, and specially handle PictureBox.Image disposal
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

                if (ctrl is PictureBox pb)
                {
                    try
                    {
                        if (pb.Image != null)
                        {
                            try { pb.Image.Dispose(); } catch { }
                            pb.Image = null;
                        }
                    }
                    catch { /* ignore */ }
                }

                try { ctrl.Controls.Clear(); } catch { }

                try { ctrl.Dispose(); } catch { /* ignore */ }
            }
            catch { /* ignore */ }
        }
    }
}