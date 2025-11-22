using lib_track_kiosk.caching; // SharedBookCache
using lib_track_kiosk.configs;
using lib_track_kiosk.helpers;   // for GetAllBooks / BookInfo
using lib_track_kiosk.loading_forms; // for Loading
using lib_track_kiosk.models;    // for GroupedBook
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using lib_track_kiosk.sub_forms;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookBooks : UserControl
    {
        // PAGINATION SETTINGS
        private const int BOOKS_PER_PAGE = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // MEMORY OPTIMIZATION: Reduced thumbnail sizes
        private const int THUMBNAIL_WIDTH = 200;
        private const int THUMBNAIL_HEIGHT = 260;
        private const int CONCURRENT_DOWNLOADS = 3;

        // switched to grouped view so we show one card per batch_registration_key
        private List<GroupedBook> _groupedBooks = new List<GroupedBook>();
        private List<GroupedBook> _allGroupedBooks = new List<GroupedBook>();
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

        // MEMORY OPTIMIZATION: Limit concurrent downloads
        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(CONCURRENT_DOWNLOADS);

        // Track outstanding download tasks so we can optionally await them during dispose
        private readonly List<Task> _outstandingDownloads = new List<Task>();

        // Track if control is visible/active
        private bool _isActive = false;

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
                search_txtBox.TextChanged -= SearchTxtBox_TextChanged;
                search_txtBox.TextChanged += SearchTxtBox_TextChanged;
                search_txtBox.KeyDown += SearchTxtBox_KeyDown;
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

            // Wire up visibility changed events for cleanup
            this.VisibleChanged += UC_LookBooks_VisibleChanged;
            this.Disposed += UC_LookBooks_Disposed;

            TryEnableDoubleBuffer(books_FlowLayoutPanel);

            // Load books from shared cache (or fetch once) asynchronously
            _ = LoadBooksAsync();
        }

        /// <summary>
        /// Called when control visibility changes. Cleanup when hidden.
        /// </summary>
        private void UC_LookBooks_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.Visible && _isActive)
                {
                    // Control is being hidden - cleanup memory
                    Console.WriteLine("🧹 UC_LookBooks hidden - cleaning up memory...");
                    CleanupMemory();
                    _isActive = false;
                }
                else if (this.Visible && !_isActive)
                {
                    _isActive = true;
                    Console.WriteLine("✓ UC_LookBooks shown");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_LookBooks_VisibleChanged error: {ex.Message}");
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
                Console.WriteLine("🧹 Starting memory cleanup for UC_LookBooks...");

                // Cancel any pending downloads
                try { _cts?.Cancel(); } catch { }

                // Close keyboard
                TryCloseKeyboard();

                // Clear all book cards and their images
                ClearBooksFlowPanel();

                // Clear tooltip
                try
                {
                    if (_cardToolTip != null)
                    {
                        _cardToolTip.RemoveAll();
                    }
                }
                catch { }

                // Clear references to book data (but keep the lists for quick reload)
                // Don't clear _allBooks, _allGroupedBooks as they're cached
                _groupedBooks?.Clear();
                _selectedCard = null;

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
                Console.WriteLine("🔄 Reloading UC_LookBooks data...");

                // Reset to first page
                _currentPage = 1;

                // Restore filtered books from cache
                _groupedBooks = _allGroupedBooks.Select(g => g).ToList();

                // Re-display
                DisplayBooks();

                // Start loading images
                _ = StartLazyDownloadForVisibleCoversAsync();

                Console.WriteLine("✓ UC_LookBooks data reloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ReloadData error: {ex.Message}");
            }
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
            Console.WriteLine("🗑️ UC_LookBooks disposing...");

            try { this.VisibleChanged -= UC_LookBooks_VisibleChanged; } catch { }

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
            catch { }

            try
            {
                if (author_cmbx != null) author_cmbx.SelectedIndexChanged -= FilterComboBox_SelectedIndexChanged;
                if (genre_cmbx != null) genre_cmbx.SelectedIndexChanged -= FilterComboBox_SelectedIndexChanged;
            }
            catch { }

            try { _osk?.Dispose(); } catch { }

            try
            {
                if (_searchDebounceTimer != null)
                {
                    _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Dispose();
                }
            }
            catch { }

            try { _cardToolTip?.Dispose(); } catch { }

            try { _cts.Cancel(); } catch { }

            try
            {
                Task[] tasks;
                lock (_outstandingDownloads) { tasks = _outstandingDownloads.ToArray(); }
                Task.WaitAll(tasks, 500);
            }
            catch { }

            try { _cts.Dispose(); } catch { }

            try { ClearBooksFlowPanel(); } catch { }

            try
            {
                foreach (var b in _allBooks)
                {
                    try { b.CoverImage = null; } catch { }
                }
                _allBooks.Clear();
                _groupedBooks.Clear();
                _allGroupedBooks.Clear();
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

            Console.WriteLine("✓ UC_LookBooks disposed");
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
            catch { }
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _searchDebounceTimer?.Stop();
                ApplyFilters();
            }
            catch { }
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

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
                var fetched = await SharedBookCache.GetOrFetchBooksAsync(() => GetAllBooks.GetAllAsync())
                    .ConfigureAwait(false);

                if (fetched != null && fetched.Count > 0)
                {
                    if (this.IsHandleCreated && this.InvokeRequired)
                    {
                        this.Invoke((Action)(() =>
                        {
                            _allBooks = fetched;
                            _groupedBooks = GroupByBatchKey(fetched);
                            _allGroupedBooks = _groupedBooks.Select(g => g).ToList();
                            _currentPage = 1;
                            PopulateAuthorAndGenreFilters();
                            DisplayBooks();
                            _ = StartLazyDownloadForVisibleCoversAsync();
                        }));
                    }
                    else
                    {
                        _allBooks = fetched;
                        _groupedBooks = GroupByBatchKey(fetched);
                        _allGroupedBooks = _groupedBooks.Select(g => g).ToList();
                        _currentPage = 1;
                        PopulateAuthorAndGenreFilters();
                        DisplayBooks();
                        _ = StartLazyDownloadForVisibleCoversAsync();
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
                    catch { }
                }
            }
        }

        private async Task StartLazyDownloadForVisibleCoversAsync()
        {
            var token = _cts.Token;

            try
            {
                var cards = books_FlowLayoutPanel.Controls.Cast<Control>().ToArray();
                foreach (Control ctrl in cards)
                {
                    if (token.IsCancellationRequested) break;
                    if (!(ctrl is Panel card)) continue;

                    var pictureBox = FindPictureBoxInCard(card);
                    if (pictureBox == null) continue;

                    if (pictureBox.Image != null) continue;

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

                    if (rep.CoverImage != null)
                    {
                        try
                        {
                            var thumb = CreateThumbnailFromImage(rep.CoverImage, new Size(pictureBox.Width, pictureBox.Height));
                            TrySetPictureBoxImageSafely(pictureBox, thumb);
                            continue;
                        }
                        catch { }
                    }

                    var rawUrl = ExtractCoverUrlFromBookInfo(rep);
                    if (string.IsNullOrWhiteSpace(rawUrl)) continue;

                    var url = NormalizeCoverUrl(rawUrl);
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    var t = Task.Run(async () =>
                    {
                        if (token.IsCancellationRequested) return;

                        await _downloadSemaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            if (token.IsCancellationRequested) return;

                            Image cachedImg = null;
                            try
                            {
                                cachedImg = await SharedBookCache.GetOrDownloadImageAsync(url, token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { return; }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Failed to download cover {url}: {ex.Message}");
                                return;
                            }

                            if (cachedImg == null) return;

                            Bitmap thumbBmp = null;
                            try
                            {
                                thumbBmp = CreateThumbnailFromImage(cachedImg, new Size(pictureBox.Width, pictureBox.Height));
                            }
                            catch (Exception)
                            {
                                try { thumbBmp = new Bitmap(cachedImg); } catch { thumbBmp = null; }
                            }

                            if (thumbBmp == null) return;

                            if (pictureBox.IsDisposed || pictureBox.Disposing) { try { thumbBmp.Dispose(); } catch { } return; }

                            try
                            {
                                if (pictureBox.InvokeRequired)
                                {
                                    pictureBox.Invoke((Action)(() => TrySetPictureBoxImageSafely(pictureBox, thumbBmp)));
                                }
                                else
                                {
                                    TrySetPictureBoxImageSafely(pictureBox, thumbBmp);
                                }
                            }
                            catch
                            {
                                try { thumbBmp.Dispose(); } catch { }
                            }
                        }
                        finally
                        {
                            try { _downloadSemaphore.Release(); } catch { }
                        }
                    }, token);

                    lock (_outstandingDownloads)
                    {
                        _outstandingDownloads.Add(t);
                        _outstandingDownloads.RemoveAll(x => x.IsCompleted);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ StartLazyDownloadForVisibleCoversAsync error: {ex.Message}");
            }
        }

        private void TrySetPictureBoxImageSafely(PictureBox pictureBox, Image img)
        {
            if (pictureBox == null) return;
            if (pictureBox.IsDisposed || pictureBox.Disposing)
            {
                img?.Dispose();
                return;
            }
            try
            {
                var old = pictureBox.Image;
                pictureBox.Image = img;
                try { old?.Dispose(); } catch { }
            }
            catch
            {
                try { pictureBox.Image = img; } catch { }
            }
        }

        private Bitmap CreateThumbnailFromImage(Image src, Size destSize)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (destSize.Width <= 0 || destSize.Height <= 0)
                throw new ArgumentException("Invalid destination size", nameof(destSize));

            var dest = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dest))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.Clear(Color.Transparent);

                float srcRatio = (float)src.Width / src.Height;
                float dstRatio = (float)destSize.Width / destSize.Height;

                Rectangle destRect;
                if (srcRatio > dstRatio)
                {
                    int h = destSize.Height;
                    int w = (int)(h * srcRatio);
                    destRect = new Rectangle((destSize.Width - w) / 2, 0, w, h);
                }
                else
                {
                    int w = destSize.Width;
                    int h = (int)(w / srcRatio);
                    destRect = new Rectangle(0, (destSize.Height - h) / 2, w, h);
                }

                var srcRect = new Rectangle(0, 0, src.Width, src.Height);
                g.DrawImage(src, new Rectangle(0, 0, destSize.Width, destSize.Height), srcRect, GraphicsUnit.Pixel);
            }
            return dest;
        }

        private PictureBox FindPictureBoxInCard(Panel card)
        {
            if (card == null) return null;
            foreach (Control c in card.Controls)
            {
                if (c is PictureBox pb) return pb;
            }
            return null;
        }

        private string NormalizeCoverUrl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return raw;

            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return raw;

            try
            {
                var prop = typeof(API_Backend).GetProperty("UploadDomain",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
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
            catch { }

            return raw;
        }

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
                    catch { }
                }
            }
            catch { }

            return null;
        }

        private string ExtractGenreNameFromBookInfo(BookInfo b)
        {
            if (b == null) return null;
            try
            {
                var t = typeof(BookInfo);
                var isUsingDeptProp = t.GetProperty("IsUsingDepartment",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
                    catch { }
                }

                if (isUsingDept)
                {
                    var deptProp = t.GetProperty("DepartmentName",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                        ?? t.GetProperty("department_name",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (deptProp != null)
                    {
                        try
                        {
                            var dv = deptProp.GetValue(b);
                            if (dv != null)
                                return dv.ToString();
                        }
                        catch { }
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
                    catch { }
                }
            }
            catch { }

            return null;
        }

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
                var isUsingDeptProp = bookInfoType.GetProperty("IsUsingDepartment",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
                        catch { }
                    }

                    if (isUsingDept)
                    {
                        var deptNameProp = bookInfoType.GetProperty("DepartmentName",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                            ?? bookInfoType.GetProperty("department_name",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (deptNameProp != null)
                        {
                            try { genreName = deptNameProp.GetValue(b)?.ToString(); } catch { }
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
                            catch { }
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
                                (!string.IsNullOrWhiteSpace(g.Representative.Title) &&
                                 g.Representative.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Author) &&
                                 g.Representative.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Status) &&
                                 g.Representative.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (!string.IsNullOrWhiteSpace(g.Representative.Year) &&
                                 g.Representative.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                            )
                        ) ||
                        (g.Copies != null && g.Copies.Any(b =>
                            (!string.IsNullOrWhiteSpace(b.Title) &&
                             b.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Author) &&
                             b.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Status) &&
                             b.Status.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (!string.IsNullOrWhiteSpace(b.Year) &&
                             b.Year.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        ))
                    ).ToList();
                }

                _currentPage = 1;
                DisplayBooks();
                _ = StartLazyDownloadForVisibleCoversAsync();
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

        private void DisplayBooks()
        {
            ClearBooksFlowPanel();
            books_FlowLayoutPanel.AutoScroll = true;

            _totalPages = (_groupedBooks.Count + BOOKS_PER_PAGE - 1) / BOOKS_PER_PAGE;
            if (_totalPages == 0) _totalPages = 1;
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            books_FlowLayoutPanel.SuspendLayout();
            try
            {
                AddPaginationInfo();

                var booksToDisplay = _groupedBooks
                    .Skip((_currentPage - 1) * BOOKS_PER_PAGE)
                    .Take(BOOKS_PER_PAGE)
                    .ToList();

                foreach (var gb in booksToDisplay)
                    AddBookCard(gb);

                if (_groupedBooks.Count == 0)
                    AddNoResultsMessageToFlow();

                AddPaginationControls();
            }
            finally
            {
                books_FlowLayoutPanel.ResumeLayout(false);
                books_FlowLayoutPanel.PerformLayout();
            }
        }

        private void AddPaginationInfo()
        {
            try
            {
                var panel = new Panel
                {
                    Width = books_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 20,
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
                    Text = $"Page {_currentPage} of {_totalPages} • Showing {Math.Min(_groupedBooks.Count, BOOKS_PER_PAGE)} of {_groupedBooks.Count} books",
                    Dock = DockStyle.Fill
                };

                panel.Controls.Add(lblInfo);
                books_FlowLayoutPanel.Controls.Add(panel);
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
                    Width = books_FlowLayoutPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 20,
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
                        DisplayBooks();
                        _ = StartLazyDownloadForVisibleCoversAsync();
                        books_FlowLayoutPanel.ScrollControlIntoView(books_FlowLayoutPanel.Controls[0]);
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
                        DisplayBooks();
                        _ = StartLazyDownloadForVisibleCoversAsync();
                        books_FlowLayoutPanel.ScrollControlIntoView(books_FlowLayoutPanel.Controls[0]);
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

                books_FlowLayoutPanel.Controls.Add(panel);
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
                var panel = new Panel
                {
                    Width = Math.Max(200, books_FlowLayoutPanel.ClientSize.Width -
                        SystemInformation.VerticalScrollBarWidth - 40),
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
                books_FlowLayoutPanel.Controls.Add(panel);
            }
            catch { }
        }

        private void AddBookCard(GroupedBook gb)
        {
            var book = gb.Representative ?? new BookInfo();

            int cardWidth = 220;
            int cardHeight = 400;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                BackColor = Color.White,
                Margin = new Padding(15),
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
                Width = THUMBNAIL_WIDTH,
                Height = THUMBNAIL_HEIGHT,
                Left = (cardWidth - THUMBNAIL_WIDTH) / 2,
                Top = 10,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke,
                Image = null
            };

            Label copiesBadge = new Label
            {
                AutoSize = false,
                Width = 100,
                Height = 22,
                Left = cardWidth - 105,
                Top = 5,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Text = $"{gb.AvailableCopies}/{gb.TotalCopies} Avail"
            };

            if (gb.AvailableCopies == 0)
            {
                copiesBadge.BackColor = Color.FromArgb(220, 53, 69);
            }

            Label title = new Label
            {
                AutoSize = false,
                Width = cardWidth - 20,
                Height = 45,
                Left = 10,
                Top = THUMBNAIL_HEIGHT + 20,
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.TopCenter,
                Text = TruncateText(book.Title, 60)
            };

            Label author = new Label
            {
                AutoSize = false,
                Width = cardWidth - 20,
                Height = 35,
                Left = 10,
                Top = THUMBNAIL_HEIGHT + 68,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.TopCenter,
                Text = TruncateText(book.Author, 50)
            };

            Label year = new Label
            {
                AutoSize = false,
                Width = cardWidth - 20,
                Height = 20,
                Left = 10,
                Top = cardHeight - 30,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
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
                _cardToolTip?.SetToolTip(card, tooltipText);
            }
            catch { }

            Action<Control> attachClickRecursively = null;
            attachClickRecursively = (ctrl) =>
            {
                try
                {
                    ctrl.Click += Card_Click;
                    foreach (Control child in ctrl.Controls)
                        attachClickRecursively(child);
                }
                catch { }
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
                MessageBox.Show($"Failed to open book viewer: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void scrollUp_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 400;
            var scroll = books_FlowLayoutPanel.VerticalScroll;
            int newValue = Math.Max(scroll.Minimum, scroll.Value - scrollAmount);
            scroll.Value = newValue;
            books_FlowLayoutPanel.PerformLayout();
        }

        private void scrollDown_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 400;
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
                var prop = typeof(Control).GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch { }
        }

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
                    catch { }
                }
            }
            catch { }
            finally
            {
                try { books_FlowLayoutPanel.ResumeLayout(false); } catch { }
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
                    catch { }
                }

                try { ctrl.Controls.Clear(); } catch { }
                try { ctrl.Dispose(); } catch { }
            }
            catch { }
        }
    }
}