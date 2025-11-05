using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using lib_track_kiosk.helpers;

namespace lib_track_kiosk.sub_forms
{
    public partial class Rate : Form
    {
        // current selected rating (0..5)
        private int _currentRating = 0;

        // labels representing stars (1..5)
        private readonly List<Label> _starLabels = new List<Label>();

        // message label that shows emoji + textual feedback
        private Label _ratingMessageLbl;

        // on-screen keyboard helper for the comment richtextbox
        private OnScreenKeyboard _osk;

        // optional rating target details
        private readonly int _ratingUserId;
        private readonly string _ratingItemType;
        private readonly int? _ratingItemId;
        private readonly string _ratingItemTitle;

        // Default parameterless constructor used by designer and other callers
        public Rate() : this(0, null, null, null) { }

        // New constructor that allows passing the user and item to rate
        public Rate(int userId, string itemType, int? itemId, string itemTitle)
        {
            InitializeComponent();

            _ratingUserId = userId;
            _ratingItemType = itemType;
            _ratingItemId = itemId;
            _ratingItemTitle = itemTitle;

            // set window title to show the item being rated (if available)
            try
            {
                var labelPart = !string.IsNullOrWhiteSpace(_ratingItemTitle)
                    ? _ratingItemTitle
                    : (_ratingItemId.HasValue ? _ratingItemId.Value.ToString() : "Item");
                this.Text = $"Rate {_ratingItemType ?? "Item"} - {labelPart}";
            }
            catch { /* ignore title setting */ }

            // Display the passed item details in the bookRsearchDisplay_rtbx if present
            try
            {
                if (bookRsearchDisplay_rtbx != null)
                {
                    var parts = new List<string>();

                    if (!string.IsNullOrWhiteSpace(_ratingItemType))
                        parts.Add($"Type: {_ratingItemType}");

                    if (!string.IsNullOrWhiteSpace(_ratingItemTitle))
                        parts.Add($"Title: {_ratingItemTitle}");

                    if (_ratingItemId.HasValue)
                        parts.Add($"ID: {_ratingItemId.Value}");

                    if (_ratingUserId > 0)
                        parts.Add($"User ID: {_ratingUserId}");

                    // Join with new lines and show in the richtextbox
                    bookRsearchDisplay_rtbx.Text = parts.Count > 0 ? string.Join(Environment.NewLine, parts) : "Item information not available";
                    bookRsearchDisplay_rtbx.ReadOnly = true;
                    bookRsearchDisplay_rtbx.SelectionStart = 0;
                    bookRsearchDisplay_rtbx.SelectionLength = 0;
                }
            }
            catch
            {
                // ignore any display errors
            }

            // instantiate on-screen keyboard helper (can be configured via constructor if desired)
            try
            {
                _osk = new OnScreenKeyboard();
            }
            catch
            {
                _osk = null;
            }

            // ensure the FlowLayoutPanel exists and initialize stars + message
            try
            {
                InitializeStarSelection();
            }
            catch (Exception ex)
            {
                // keep form usable even if initialization fails
                Console.WriteLine($"Failed to initialize star selection UI: {ex.Message}");
            }

            // wire comment textbox events to open/close keyboard
            try
            {
                if (comment_rtbx != null)
                {
                    // Make the comment box not be the initial active control.
                    // Keep TabStop false initially so keyboard won't be triggered by programmatic focus or tabbing on show.
                    // The user click handler will enable TabStop and focus the control when the user intentionally interacts with it.
                    comment_rtbx.TabStop = false;

                    comment_rtbx.GotFocus += Comment_Rtbx_GotFocus;
                    comment_rtbx.LostFocus += Comment_Rtbx_LostFocus;
                    comment_rtbx.Click += Comment_Rtbx_Click;
                    // Close keyboard when Enter is pressed on the comment box (covers physical and on-screen keyboard)
                    comment_rtbx.KeyDown += Comment_Rtbx_KeyDown;
                }
            }
            catch { /* ignore */ }

            // ensure we clean up resources when the form is disposed
            this.Disposed += Rate_Disposed;

            // Attach Shown event to ensure focus is set after the form is displayed.
            // Doing focus moves in Shown (and via BeginInvoke) reliably overrides any designer-assigned initial focus.
            this.Shown += Rate_Shown;
        }

        // Ensure that when the form is shown the initial focus is placed on a non-text control so the OS on-screen keyboard
        // does not pop up automatically. Using BeginInvoke ensures the focus call happens after WinForms finishes its own focus setup.
        private void Rate_Shown(object sender, EventArgs e)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_starLabels != null && _starLabels.Count > 0)
                        {
                            _starLabels[0].Focus();
                        }
                        else if (submit_btn != null && submit_btn.CanFocus)
                        {
                            submit_btn.Focus();
                        }
                        else if (close_btn != null && close_btn.CanFocus)
                        {
                            close_btn.Focus();
                        }
                        else
                        {
                            // Fallback: clear active control to avoid focusing a text box
                            this.ActiveControl = null;
                        }
                    }
                    catch { /* ignore focus errors */ }
                }));
            }
            catch { /* ignore */ }
        }

        private void InitializeStarSelection()
        {
            if (starSelection_flp == null)
                throw new InvalidOperationException("starSelection_flp not found on the form.");

            // Configure the FlowLayoutPanel for predictable behavior
            starSelection_flp.Controls.Clear();
            starSelection_flp.WrapContents = true; // allow the message label to wrap on a new line
            starSelection_flp.FlowDirection = FlowDirection.LeftToRight;
            starSelection_flp.AutoSize = false;

            _starLabels.Clear();

            // Create 5 star labels. Use Unicode stars so no image resources required.
            // Make them bigger
            const float starFontSize = 48f;

            for (int i = 1; i <= 5; i++)
            {
                var lbl = new Label
                {
                    AutoSize = true,
                    Text = "☆", // hollow star
                    Font = new Font("Segoe UI Symbol", starFontSize, FontStyle.Regular, GraphicsUnit.Point),
                    ForeColor = Color.Gold,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(6, 6, 6, 6),
                    Tag = i,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                };

                // Accessibility: allow keyboard focus
                lbl.TabStop = true;

                // Events for hover preview and selection
                lbl.Click += Star_Click;
                lbl.MouseEnter += Star_MouseEnter;
                lbl.MouseLeave += Star_MouseLeave;
                lbl.KeyDown += Star_KeyDown; // support keyboard selection (Enter/Space)

                // Add to flow layout panel and list
                starSelection_flp.Controls.Add(lbl);
                starSelection_flp.SetFlowBreak(lbl, false);

                _starLabels.Add(lbl);
            }

            // Ensure the message label is on its own line after the stars
            if (_starLabels.Count > 0)
            {
                starSelection_flp.SetFlowBreak(_starLabels.Last(), true);
            }

            // Create the rating message label (emoji + text)
            _ratingMessageLbl = new Label
            {
                AutoSize = false,
                Width = Math.Max(200, starSelection_flp.ClientSize.Width),
                Height = 42,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent,
                Text = "No rating yet"
            };

            starSelection_flp.Controls.Add(_ratingMessageLbl);
            starSelection_flp.SetFlowBreak(_ratingMessageLbl, true);

            // Respond to size changes so the stars + message are centered
            starSelection_flp.SizeChanged -= StarSelection_flp_SizeChanged;
            starSelection_flp.SizeChanged += StarSelection_flp_SizeChanged;

            // initialize display
            UpdateStarDisplay(_currentRating);
            CenterStarsAndAdjustMessage();
        }

        private void StarSelection_flp_SizeChanged(object sender, EventArgs e)
        {
            CenterStarsAndAdjustMessage();
        }

        private void CenterStarsAndAdjustMessage()
        {
            try
            {
                if (starSelection_flp == null) return;
                if (_starLabels == null || _starLabels.Count == 0) return;

                // Measure total width occupied by stars (including margins)
                int totalStarsWidth = 0;
                foreach (var lbl in _starLabels)
                {
                    var w = lbl.PreferredSize.Width;
                    var margin = lbl.Margin;
                    totalStarsWidth += w + margin.Left + margin.Right;
                }

                // left padding to center stars
                int availableWidth = starSelection_flp.ClientSize.Width - (starSelection_flp.Padding.Left + starSelection_flp.Padding.Right);
                int leftPadding = Math.Max(0, (availableWidth - totalStarsWidth) / 2);

                // set padding to center the star row
                starSelection_flp.Padding = new Padding(leftPadding, starSelection_flp.Padding.Top, starSelection_flp.Padding.Right, starSelection_flp.Padding.Bottom);

                // make rating message label use full available width (so text is centered)
                if (_ratingMessageLbl != null)
                {
                    _ratingMessageLbl.Width = Math.Max(200, availableWidth);
                    _ratingMessageLbl.TextAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch { /* ignore layout errors */ }
        }

        private void Star_Click(object sender, EventArgs e)
        {
            if (sender is Label lbl && lbl.Tag is int idx)
            {
                _currentRating = idx;
                UpdateStarDisplay(_currentRating);
            }
            else if (sender is Label lbl2 && int.TryParse(lbl2.Tag?.ToString(), out var idx2))
            {
                _currentRating = idx2;
                UpdateStarDisplay(_currentRating);
            }
        }

        private void Star_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Label lbl && lbl.Tag is int idx)
            {
                // preview highlight up to idx
                UpdateStarDisplay(idx);
            }
            else if (sender is Label lbl2 && int.TryParse(lbl2.Tag?.ToString(), out var idx2))
            {
                UpdateStarDisplay(idx2);
            }
        }

        private void Star_MouseLeave(object sender, EventArgs e)
        {
            // restore to actual selected rating
            UpdateStarDisplay(_currentRating);
        }

        private void Star_KeyDown(object sender, KeyEventArgs e)
        {
            // support keyboard selection on focused star: Enter or Space sets rating
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                if (sender is Label lbl && lbl.Tag is int idx)
                {
                    _currentRating = idx;
                    UpdateStarDisplay(_currentRating);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            // support left/right arrow to navigate stars when focused
            if (e.KeyCode == Keys.Right)
            {
                if (sender is Label lbl && lbl.Tag is int idx)
                {
                    int next = Math.Min(5, idx + 1);
                    _starLabels[Math.Max(0, next - 1)].Focus();
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (sender is Label lbl && lbl.Tag is int idx)
                {
                    int prev = Math.Max(1, idx - 1);
                    _starLabels[Math.Max(0, prev - 1)].Focus();
                }
            }
        }

        private void UpdateStarDisplay(int rating)
        {
            // rating: number of filled stars (0..5)
            for (int i = 0; i < _starLabels.Count; i++)
            {
                var lbl = _starLabels[i];
                if (i < rating)
                {
                    lbl.Text = "★"; // filled star
                    lbl.ForeColor = Color.FromArgb(255, 196, 37); // brighter gold
                    lbl.Font = new Font(lbl.Font.FontFamily, 48f, FontStyle.Bold);
                }
                else
                {
                    lbl.Text = "☆"; // hollow star
                    lbl.ForeColor = Color.Gold;
                    lbl.Font = new Font(lbl.Font.FontFamily, 48f, FontStyle.Regular);
                }
            }

            // Update message (emoji + text) according to rating
            if (_ratingMessageLbl != null)
            {
                var info = GetRatingMessage(rating);
                _ratingMessageLbl.Text = info.message;
                _ratingMessageLbl.ForeColor = info.color;
            }
        }

        // returns display string and color for a given rating (0..5)
        private (string message, Color color) GetRatingMessage(int rating)
        {
            switch (rating)
            {
                case 1:
                    return ("😞  Very Bad", Color.FromArgb(200, 40, 40)); // red
                case 2:
                    return ("☹️  Bad", Color.FromArgb(220, 90, 40)); // orange-red
                case 3:
                    return ("😐  Okay", Color.FromArgb(160, 120, 30)); // brownish
                case 4:
                    return ("🙂  Good", Color.FromArgb(34, 139, 34)); // green
                case 5:
                    return ("🤩  Excellent", Color.FromArgb(0, 120, 215)); // bright blue/positive
                default:
                    return ("No rating yet", Color.DimGray);
            }
        }

        // Submit button now sends rating to backend if an item and user are present
        private async void submit_btn_Click(object sender, EventArgs e)
        {
            try
            {
                var stars = _currentRating;
                var comment = string.Empty;

                try
                {
                    if (comment_rtbx != null)
                        comment = comment_rtbx.Text?.Trim();
                }
                catch { comment = string.Empty; }

                // require at least one star to submit
                if (stars <= 0)
                {
                    MessageBox.Show(this, "Please select a star rating (1–5) before submitting.", "Rating required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ensure keyboard is closed before showing confirmation / sending
                TryCloseKeyboard();

                // If no user or no item info, show local confirmation and exit (no server call)
                if (_ratingUserId <= 0 || (string.IsNullOrWhiteSpace(_ratingItemType) || !_ratingItemId.HasValue))
                {
                    // Fallback: just show summary and close
                    string ratingText = _ratingMessageLbl?.Text ?? $"{stars}/5";
                    var msg = $"You rated {stars} star{(stars == 1 ? "" : "s")}.{Environment.NewLine}{Environment.NewLine}Feedback: {ratingText}{Environment.NewLine}{Environment.NewLine}";
                    if (!string.IsNullOrEmpty(comment))
                        msg += "Comment:" + Environment.NewLine + comment;
                    else
                        msg += "(No comment provided)";

                    MessageBox.Show(this, msg, "Thank you for your feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                // Post to server via RateHelper, depending on item type
                var resp = (RateHelper.ServerResponse)null;
                try
                {
                    if (_ratingItemType != null && _ratingItemType.IndexOf("book", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        resp = await RateHelper.SendSingleBookRatingAsync(_ratingUserId, _ratingItemId.Value, stars, comment);
                    }
                    else
                    {
                        // default: treat as research paper
                        resp = await RateHelper.SendSingleResearchPaperRatingAsync(_ratingUserId, _ratingItemId.Value, stars, comment);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to send rating: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (resp == null)
                {
                    MessageBox.Show(this, "No response from server when submitting rating.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (resp.Success)
                {
                    MessageBox.Show(this, resp.Message ?? "Rating submitted. Thank you!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                else
                {
                    // try to provide helpful message
                    string err = resp.Message ?? "Failed to submit rating.";
                    if (!string.IsNullOrEmpty(resp.Error))
                        err += Environment.NewLine + resp.Error;
                    MessageBox.Show(this, err, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // keep form open to allow retry
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to submit rating: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void close_btn_Click(object sender, EventArgs e)
        {
            try
            {
                TryCloseKeyboard();
                this.Close();
            }
            catch { /* ignore */ }
        }

        // ---------------- On-screen keyboard helpers for comment_rtbx ----------------

        // When user explicitly clicks the comment box we enable its TabStop and set focus (this prevents
        // it from being the initial active control on form show while still allowing the user to tap it).
        private void Comment_Rtbx_Click(object sender, EventArgs e)
        {
            try
            {
                if (comment_rtbx != null)
                {
                    comment_rtbx.TabStop = true;
                    comment_rtbx.Focus();
                }

                TryOpenKeyboard();
            }
            catch { /* ignore */ }
        }

        private void Comment_Rtbx_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void Comment_Rtbx_LostFocus(object sender, EventArgs e)
        {
            // small delay to allow focus transfer to other controls (like submit button)
            await Task.Delay(150);
            try
            {
                if (!this.ContainsFocus)
                    TryCloseKeyboard();
            }
            catch { }
        }

        // Close keyboard when Enter is pressed in the comment box (covers both physical and on-screen keyboard Enter)
        private void Comment_Rtbx_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    TryCloseKeyboard();
                    // do not suppress the Enter key since user might want a newline; hook in OnScreenKeyboard
                    // will also close the keyboard if the OSK sends VK_RETURN.
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

        // Clean up event handlers and dispose keyboard helper
        private void Rate_Disposed(object sender, EventArgs e)
        {
            try
            {
                if (comment_rtbx != null)
                {
                    comment_rtbx.GotFocus -= Comment_Rtbx_GotFocus;
                    comment_rtbx.LostFocus -= Comment_Rtbx_LostFocus;
                    comment_rtbx.Click -= Comment_Rtbx_Click;
                    comment_rtbx.KeyDown -= Comment_Rtbx_KeyDown;
                }
            }
            catch { /* ignore */ }

            try
            {
                _osk?.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}