using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using lib_track_kiosk.models;    // GroupedBook
using lib_track_kiosk.helpers;   // BookInfo

namespace lib_track_kiosk.sub_forms
{
    public partial class ViewSelectedBook : Form
    {
        private GroupedBook _group;

        public ViewSelectedBook()
        {
            InitializeComponent();
            ConfigureFlowPanel();
            if (close_btn != null) close_btn.Click += (s, e) => this.Close();
            PopulateStaticBook();
        }

        // New constructor used by UC_LookBooks to pass grouped data
        public ViewSelectedBook(GroupedBook group)
        {
            InitializeComponent();
            ConfigureFlowPanel();
            if (close_btn != null) close_btn.Click += (s, e) => this.Close();

            _group = group ?? throw new ArgumentNullException(nameof(group));
            PopulateFromGroup(_group);
        }

        private void ConfigureFlowPanel()
        {
            try
            {
                bookInfo_flp.AutoScroll = true;
                bookInfo_flp.FlowDirection = FlowDirection.TopDown;
                bookInfo_flp.WrapContents = false;
                bookInfo_flp.Padding = new Padding(12);
            }
            catch { }
        }

        private void PopulateFromGroup(GroupedBook group)
        {
            var rep = group.Representative ?? new BookInfo { Title = "N/A", Author = "N/A" };

            // Cover
            try
            {
                if (rep.CoverImage != null && bookCover_picturebox != null)
                {
                    // clone so we don't hold original image instance
                    bookCover_picturebox.Image = (Image)rep.CoverImage.Clone();
                    bookCover_picturebox.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
            catch { }

            bookInfo_flp.SuspendLayout();
            bookInfo_flp.Controls.Clear();

            // Title
            var titleLbl = new Label
            {
                Text = rep.Title ?? "N/A",
                AutoSize = false,
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 68,
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            bookInfo_flp.Controls.Add(titleLbl);

            // Author / Publisher
            var metaPanel = new Panel
            {
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 54,
                BackColor = Color.Transparent
            };

            var authorLbl = new Label
            {
                Text = $"Author: {rep.Author ?? "N/A"}",
                AutoSize = false,
                Width = metaPanel.Width,
                Height = 24,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            metaPanel.Controls.Add(authorLbl);

            var publisherLbl = new Label
            {
                Text = $"Publisher: {rep.Publisher ?? "N/A"}",
                AutoSize = false,
                Width = metaPanel.Width,
                Height = 24,
                Top = authorLbl.Bottom,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            metaPanel.Controls.Add(publisherLbl);

            bookInfo_flp.Controls.Add(metaPanel);
            bookInfo_flp.Controls.Add(CreateSeparator(bookInfo_flp.ClientSize.Width - 24));

            // Top info row: Edition | Year | Shelf Location
            var topInfoRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 84,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            topInfoRow.Controls.Add(CreateInfoBox("Edition", rep.Edition ?? "N/A", 220));
            topInfoRow.Controls.Add(CreateInfoBox("Year", rep.Year ?? "N/A", 140));
            topInfoRow.Controls.Add(CreateInfoBox("Shelf Location", rep.ShelfLocation ?? "N/A", 320));

            bookInfo_flp.Controls.Add(topInfoRow);
            bookInfo_flp.Controls.Add(CreateSeparator(bookInfo_flp.ClientSize.Width - 24));

            // Middle row: Price | Donor | Batch Key
            var middleRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 78,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            middleRow.Controls.Add(CreateInfoBox("Price", rep.Price ?? "N/A", 180));
            middleRow.Controls.Add(CreateInfoBox("Donor", rep.Donor ?? "N/A", 260));
            middleRow.Controls.Add(CreateInfoBox("Batch", group.BatchKey ?? "N/A", 220));

            bookInfo_flp.Controls.Add(middleRow);
            bookInfo_flp.Controls.Add(CreateSeparator(bookInfo_flp.ClientSize.Width - 24));

            // Bottom row: Rating | Total Copies | Copies Available
            var bottomRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 110,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            bottomRow.Controls.Add(CreateRatingBox(0, 5, 260)); // no rating available in BookInfo - keep placeholder
            bottomRow.Controls.Add(CreateInfoBox("Total Copies", group.TotalCopies.ToString(), 160));
            bottomRow.Controls.Add(CreateInfoBox("Copies Available", group.AvailableCopies.ToString(), 160));

            bookInfo_flp.Controls.Add(bottomRow);
            bookInfo_flp.Controls.Add(CreateSeparator(bookInfo_flp.ClientSize.Width - 24));

            // Footer: quick status summary
            var footerPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = bookInfo_flp.ClientSize.Width - 24,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var summaryText = string.Join("  •  ", group.StatusCounts.Select(kv => $"{kv.Key}: {kv.Value}"));
            footerPanel.Controls.Add(CreateInfoLabel(summaryText, bookInfo_flp.ClientSize.Width - 24));
            bookInfo_flp.Controls.Add(footerPanel);

            bookInfo_flp.ResumeLayout();
        }

        private Control CreateCopyRow(BookInfo copy)
        {
            var panel = new Panel
            {
                Width = bookInfo_flp.ClientSize.Width - 60,
                Height = 36,
                BackColor = Color.White,
                Margin = new Padding(2),
                BorderStyle = BorderStyle.FixedSingle
            };

            var num = new Label
            {
                Text = copy.BookNumber ?? copy.BookId.ToString(),
                AutoSize = false,
                Width = 120,
                Height = 36,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 6
            };
            panel.Controls.Add(num);

            var statusLbl = new Label
            {
                Text = copy.Status ?? "Unknown",
                AutoSize = false,
                Width = 140,
                Height = 28,
                Left = 140,
                Top = 4,
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // color-code status
            string st = (copy.Status ?? "Unknown").ToLowerInvariant();
            if (st == "available") statusLbl.BackColor = Color.FromArgb(40, 167, 69);
            else if (st == "borrowed") statusLbl.BackColor = Color.FromArgb(255, 193, 7);
            else if (st == "returned") statusLbl.BackColor = Color.FromArgb(23, 162, 184);
            else if (st == "lost") statusLbl.BackColor = Color.FromArgb(220, 53, 69);
            else statusLbl.BackColor = Color.FromArgb(150, 150, 150);

            panel.Controls.Add(statusLbl);

            var detailsBtn = new Button
            {
                Text = "Details",
                Width = 70,
                Height = 26,
                Left = panel.Width - 82,
                Top = 4,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            detailsBtn.Click += (s, e) =>
            {
                MessageBox.Show($"Copy: {copy.BookNumber ?? copy.BookId.ToString()}\nStatus: {copy.Status}\nShelf: {copy.ShelfLocation}", "Copy Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            panel.Controls.Add(detailsBtn);

            return panel;
        }

        // original static population left for fallback / design-time preview
        private void PopulateStaticBook()
        {
            // keep existing static data UI for design-time or when no group is passed
            // you already had a working static implementation previously - reuse it
            // for brevity in this file we just call the previous sample implementation
            // (but in practice this method contains the earlier code you provided)
            // To keep this file self-contained, we'll simply call PopulateFromGroup with a synthetic sample group.

            var sample = new GroupedBook
            {
                BatchKey = "BATCH-202509",
                TotalCopies = 10,
                AvailableCopies = 4,
                StatusCounts = new System.Collections.Generic.Dictionary<string, int> { { "Available", 4 }, { "Borrowed", 5 }, { "Lost", 1 } },
                Representative = new BookInfo
                {
                    BookId = 123,
                    BookNumber = "4567",
                    Title = "C# Programming: From Basics to Advanced Applications in .NET Development",
                    Author = "John Doe, Michael White, Sarah Connor",
                    Publisher = "Acme Publishing Co.",
                    Edition = "3rd Edition",
                    Year = "2021",
                    ShelfLocation = "A3 - Row 2",
                    Price = "₱850.00",
                    Donor = "No Donor",
                    BatchRegistrationKey = "BATCH-202509"
                }
            };

            // create some copies for the sample
            for (int i = 1; i <= sample.TotalCopies; i++)
            {
                sample.Copies.Add(new BookInfo
                {
                    BookId = 1000 + i,
                    BookNumber = i.ToString(),
                    Status = i <= sample.AvailableCopies ? "Available" : (i == sample.TotalCopies ? "Lost" : "Borrowed"),
                    ShelfLocation = "A3"
                });
            }

            PopulateFromGroup(sample);
        }

        private Control CreateSeparator(int width)
        {
            return new Panel
            {
                Width = width,
                Height = 1,
                BackColor = Color.FromArgb(230, 230, 230),
                Margin = new Padding(0, 12, 0, 12)
            };
        }

        private Control CreateInfoBox(string label, string value, int width)
        {
            var panel = new Panel
            {
                Width = width,
                Height = 70,
                Margin = new Padding(8, 4, 8, 4),
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Width = panel.Width,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var val = new Label
            {
                Text = value,
                AutoSize = false,
                Width = panel.Width,
                Height = 34,
                Top = lbl.Bottom + 4,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(val);
            return panel;
        }

        private Control CreateInfoLabel(string text, int width)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Width = width,
                Height = 28,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(6, 6, 6, 6)
            };
            return lbl;
        }

        private Control CreateRatingBox(int rating, int maxRating, int width)
        {
            var panel = new Panel
            {
                Width = width,
                Height = 90,
                Margin = new Padding(8, 4, 8, 4),
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = "Rating",
                AutoSize = false,
                Width = panel.Width,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var starsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = panel.Width,
                Height = 36,
                Top = lbl.Bottom + 6
            };

            for (int i = 1; i <= maxRating; i++)
            {
                var star = new Label
                {
                    Text = i <= rating ? "★" : "☆",
                    AutoSize = false,
                    Width = 30,
                    Height = 30,
                    Font = new Font("Segoe UI Symbol", 18f),
                    ForeColor = i <= rating ? Color.FromArgb(255, 165, 0) : Color.FromArgb(200, 200, 200),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                starsPanel.Controls.Add(star);
            }

            var numeric = new Label
            {
                Text = $"{rating}/{maxRating}",
                AutoSize = false,
                Width = 60,
                Height = 30,
                Top = lbl.Bottom + 8,
                Left = starsPanel.Right + 6,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(starsPanel);
            panel.Controls.Add(numeric);
            return panel;
        }
    }
}