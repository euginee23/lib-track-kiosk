using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;

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

        public Rate()
        {
            InitializeComponent();

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
                    comment_rtbx.GotFocus += Comment_Rtbx_GotFocus;
                    comment_rtbx.LostFocus += Comment_Rtbx_LostFocus;
                    comment_rtbx.Click += Comment_Rtbx_Click;
                }
            }
            catch { /* ignore */ }

            // ensure we clean up resources when the form is disposed
            this.Disposed += Rate_Disposed;
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

        private void submit_btn_Click(object sender, EventArgs e)
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

                // Compose message including the emoji/text shown in the form
                string ratingText = _ratingMessageLbl?.Text ?? (stars == 0 ? "No rating" : $"{stars}/5");

                var msg = $"You rated {stars} star{(stars == 1 ? "" : "s")}.";
                msg += Environment.NewLine + Environment.NewLine + $"Feedback: {ratingText}";

                if (!string.IsNullOrEmpty(comment))
                {
                    msg += Environment.NewLine + Environment.NewLine + "Comment:" + Environment.NewLine + comment;
                }
                else
                {
                    msg += Environment.NewLine + Environment.NewLine + "(No comment provided)";
                }

                // Ensure keyboard is closed before showing confirmation
                TryCloseKeyboard();

                MessageBox.Show(this, msg, "Thank you for your feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Optionally close the dialog after submit:
                // this.DialogResult = DialogResult.OK;
                // this.Close();
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

        private void Comment_Rtbx_Click(object sender, EventArgs e) => TryOpenKeyboard();

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