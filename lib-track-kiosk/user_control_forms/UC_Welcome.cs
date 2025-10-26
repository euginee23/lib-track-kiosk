using lib_track_kiosk.user_control_forms;
using lib_track_kiosk.models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Linq;

namespace lib_track_kiosk.panel_forms
{
    public partial class UC_Welcome : UserControl
    {
        private List<Book> _topBooks = new List<Book>();
        private List<(string Department, int Count)> _topDepartments = new List<(string, int)>();

        // Top 5 student borrowers: FirstName, LastName, DepartmentAcronym
        private List<(string FirstName, string LastName, string DeptAcronym)> _topStudents = new List<(string, string, string)>();

        // Path to default avatar image (as provided)
        private readonly string DefaultAvatarPath = @"E:\Library-Tracker\lib-track-kiosk\images\avatar-default.png";

        public UC_Welcome()
        {
            InitializeComponent();
            // enable double buffering for smoother rendering
            this.DoubleBuffered = true;

            LoadTopBooks();
            LoadTopDepartments();
            LoadTopStudents();

            // Ensure department cards adapt if the flow panel size changes (e.g., runtime resizing)
            top3Department_flp.SizeChanged += (s, e) =>
            {
                // re-build department cards to ensure widths are correct
                DisplayTopDepartments();
            };

            // Ensure top students reflow when the container size changes
            top5MostStudentBorrowers_flp.SizeChanged += (s, e) =>
            {
                DisplayTopStudents();
            };
        }

        private void lookForBooksResearch_btn_Click(object sender, EventArgs e)
        {
            var mainForm = this.FindForm() as MainForm;
            if (mainForm != null)
            {
                var lookSearchScreen = new UC_LookSearch();
                mainForm.addUserControl(lookSearchScreen);
            }
        }

        private void borrow_btn_Click(object sender, EventArgs e)
        {
            var mainForm = this.FindForm() as MainForm;
            if (mainForm != null)
            {
                var borrowScreen = new UC_Borrow();
                mainForm.addUserControl(borrowScreen);
            }
        }

        private void return_btn_Click(object sender, EventArgs e)
        {
            var mainForm = this.FindForm() as MainForm;
            if (mainForm != null)
            {
                var returnScreen = new UC_Return();
                mainForm.addUserControl(returnScreen);
            }
        }

        private void registerFingerprint_btn_Click(object sender, EventArgs e)
        {
            var mainForm = this.FindForm() as MainForm;
            if (mainForm != null)
            {
                var userLoginScreen = new UC_UserLogin();
                mainForm.addUserControl(userLoginScreen);
            }
        }

        // ==========================================
        // LOAD STATIC TOP BOOKS
        // ==========================================
        private void LoadTopBooks()
        {
            // NOTE:
            // Replace these paths with application resources or relative paths in production.
            string path1 = @"E:\Library-Tracker\dev files\input samples\book\qwe.jpg";
            string path2 = @"E:\Library-Tracker\dev files\input samples\book\qwer.jpg";
            string path3 = @"E:\Library-Tracker\dev files\input samples\book\qwert.jpg";

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

        private void DisplayTopBooks()
        {
            top10Books_flp.Controls.Clear();
            top10Books_flp.AutoScroll = true;
            top10Books_flp.WrapContents = true;
            top10Books_flp.FlowDirection = FlowDirection.LeftToRight;
            top10Books_flp.Padding = new Padding(8);
            top10Books_flp.BackColor = Color.WhiteSmoke;

            int rank = 1;
            foreach (var book in _topBooks)
            {
                AddBookCard(book, rank++);
            }
        }

        // ==========================================
        // CREATE BOOK CARD
        // ==========================================
        private void AddBookCard(Book book, int rank)
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
            if (!string.IsNullOrEmpty(book.CoverImagePath) && File.Exists(book.CoverImagePath))
            {
                try
                {
                    using (var fs = File.OpenRead(book.CoverImagePath))
                    {
                        var img = Image.FromStream(fs);
                        cover.Image = new Bitmap(img);
                    }
                }
                catch
                {
                    cover.Image = SystemIcons.Application.ToBitmap();
                }
            }
            else
            {
                cover.Image = SystemIcons.Application.ToBitmap();
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

            // click/double-click to show details
            EventHandler showHandler = (s, e) => ShowBookDetails(book);
            card.Click += showHandler;
            foreach (Control c in card.Controls) c.Click += showHandler;
            card.DoubleClick += (s, e) => ShowBookDetails(book);

            top10Books_flp.Controls.Add(card);
        }

        // ==========================================
        // TOP 3 DEPARTMENTS (CARD UI)
        // ==========================================
        private void LoadTopDepartments()
        {
            // Static sample data for design. Replace with real data from your database.
            _topDepartments = new List<(string, int)>
            {
                ("Bachelor of Elementary Education", 142),
                ("Bachelor of Arts in Political Science", 97),
                ("Bachelor of Science in Computer Science", 86)
            };

            DisplayTopDepartments();
        }

        private void DisplayTopDepartments()
        {
            top3Department_flp.Controls.Clear();
            top3Department_flp.AutoScroll = true;
            top3Department_flp.FlowDirection = FlowDirection.TopDown;
            top3Department_flp.WrapContents = false;
            top3Department_flp.Padding = new Padding(8);
            top3Department_flp.BackColor = Color.Transparent;

            int rank = 1;
            foreach (var dept in _topDepartments)
            {
                AddDepartmentCard(dept.Department, rank++, dept.Item2);
            }
        }

        private void AddDepartmentCard(string departmentName, int rank, int borrowCount)
        {
            // Fixed card width that fits inside the flow panel when flow panel size is 361 width.
            // Use a safe fixed width to avoid relying on ClientSize being available at init.
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

            // Left color accent stripe
            var accent = new Panel
            {
                Width = 8,
                Height = card.Height,
                Left = 0,
                Top = 0,
                BackColor = rank == 1 ? Color.FromArgb(70, 60, 160) : Color.FromArgb(170, 170, 200),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };

            // Rank circle (placed to the right of the accent stripe)
            var rankCircle = new Panel
            {
                Width = 36,
                Height = 36,
                Left = accent.Right + 12,
                Top = 12,
                BackColor = Color.FromArgb(70, 60, 160),
                Margin = new Padding(0),
            };
            // make visual circle
            rankCircle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(rankCircle.BackColor))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, rankCircle.Width - 1, rankCircle.Height - 1);
                }
            };

            // rankLabel inside the circle for perfect centering
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

            // Department name
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

            // Subtext: borrow count
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

            // subtle hover
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 250, 250);
            card.MouseLeave += (s, e) => card.BackColor = Color.White;

            // click shows a simple message (replace with navigation if needed)
            EventHandler clickHandler = (s, e) =>
            {
                MessageBox.Show($"{departmentName}\nTotal borrowed: {borrowCount}", "Department details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Add controls
            card.Controls.Add(accent);
            card.Controls.Add(rankCircle);
            card.Controls.Add(deptLabel);
            card.Controls.Add(countLabel);

            // attach click handlers to card and children
            card.Click += clickHandler;
            foreach (Control c in card.Controls) c.Click += clickHandler;

            top3Department_flp.Controls.Add(card);
        }

        // ==========================================
        // TOP 5 STUDENTS (2 per row, fit inside 361x372 panel)
        // ==========================================
        private void LoadTopStudents()
        {
            // Static sample data. Replace with database-driven content later.
            // Department acronyms shown under the name.
            _topStudents = new List<(string, string, string)>
            {
                ("Maria", "Santiago", "BEED"),
                ("Andres", "Lopez", "BPS"),
                ("Karen", "Delacruz", "BSCS"),
                ("John", "Reyes", "BSED"),
                ("Eugine Bryan S.", "Cadiz", "BSIT") // added an intentionally long first+middle to test wrapping/truncation
            };

            DisplayTopStudents();
        }

        private void DisplayTopStudents()
        {
            top5MostStudentBorrowers_flp.Controls.Clear();
            top5MostStudentBorrowers_flp.Padding = new Padding(8);
            top5MostStudentBorrowers_flp.BackColor = Color.Transparent;
            top5MostStudentBorrowers_flp.FlowDirection = FlowDirection.LeftToRight;
            top5MostStudentBorrowers_flp.WrapContents = true;
            top5MostStudentBorrowers_flp.AutoScroll = false; // prefer fitting; enable if you want scrolling

            // Card margin used below for layout math
            int cardMargin = 6;

            // Respect current client width; if unavailable, fallback to safe values.
            int availableWidth = top5MostStudentBorrowers_flp.ClientSize.Width;
            if (availableWidth <= 0) availableWidth = top5MostStudentBorrowers_flp.Width > 0 ? top5MostStudentBorrowers_flp.Width : 361;

            int horizontalPadding = top5MostStudentBorrowers_flp.Padding.Left + top5MostStudentBorrowers_flp.Padding.Right;

            // For two cards per row: subtract combined margins of both cards.
            // Each card has Left + Right margin => (cardMargin * 2). For two cards that's (cardMargin * 4).
            int totalMarginsForTwoCards = cardMargin * 4;

            int cardWidth = (availableWidth - horizontalPadding - totalMarginsForTwoCards) / 2;
            // Bound the card width into a reasonable range to avoid layout glitches on very small/large sizes
            cardWidth = Math.Max(140, Math.Min(170, cardWidth));

            // Card height chosen so that 3 rows (2,2,1) comfortably fit into 372 height
            // Calculation: totalHeight = paddingTop+paddingBottom + rows*cardHeight + rows*(cardMargin*2)
            int flowPanelHeight = top5MostStudentBorrowers_flp.ClientSize.Height;
            if (flowPanelHeight <= 0) flowPanelHeight = top5MostStudentBorrowers_flp.Height > 0 ? top5MostStudentBorrowers_flp.Height : 372;
            int rows = 3;
            int verticalPadding = top5MostStudentBorrowers_flp.Padding.Top + top5MostStudentBorrowers_flp.Padding.Bottom;
            int totalVerticalMargins = rows * (cardMargin * 2);
            int cardHeight = (flowPanelHeight - verticalPadding - totalVerticalMargins) / rows;
            // Compact, readable height that allows room for up to two lines for name
            cardHeight = Math.Max(84, Math.Min(100, cardHeight));

            int rank = 1;
            foreach (var s in _topStudents.Take(5))
            {
                AddStudentCard(s.FirstName, s.LastName, s.DeptAcronym, rank++, cardWidth, cardHeight, cardMargin);
            }
        }

        private void AddStudentCard(string firstName, string lastName, string deptAcronym, int rank, int cardWidth, int cardHeight, int cardMargin)
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

            // Hover effect
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 250, 250);
            card.MouseLeave += (s, e) => card.BackColor = Color.White;

            // Rank badge circle (small) top-left
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

            // Avatar (left) - reduced size to save vertical space
            int avatarSize = 44;
            var avatar = new PictureBox
            {
                Width = avatarSize,
                Height = avatarSize,
                Left = 10,
                Top = 8 + ((cardHeight - 8 * 2 - avatarSize) / 2), // avoid overlapping top badge; keep small top gap
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            // Load default avatar image if available, else generate initials avatar
            Image avatarImg = null;
            if (File.Exists(DefaultAvatarPath))
            {
                try
                {
                    using (var fs = File.OpenRead(DefaultAvatarPath))
                    {
                        var img = Image.FromStream(fs);
                        avatarImg = CreateCircularAvatarImage(new Bitmap(img), new Size(avatarSize, avatarSize));
                    }
                }
                catch
                {
                    avatarImg = CreateInitialsAvatar(firstName, lastName, new Size(avatarSize, avatarSize));
                }
            }
            else
            {
                avatarImg = CreateInitialsAvatar(firstName, lastName, new Size(avatarSize, avatarSize));
            }
            avatar.Image = avatarImg;

            // Name label (to the right of avatar) - allow up to 2 lines and ellipsis if still too long
            var nameLabel = new Label
            {
                Text = $"{firstName} {lastName}",
                AutoSize = false,
                Width = card.Width - (avatar.Left + avatar.Width) - 14,
                Height = 40, // allow up to 2 lines
                Left = avatar.Right + 8,
                Top = avatar.Top - 2,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true
            };
            // This ensures wrapping happens (AutoEllipsis only shows after wrap)
            nameLabel.MaximumSize = new Size(nameLabel.Width, nameLabel.Height);

            // Department acronym under name
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

            // click shows details (replace with navigation)
            EventHandler clickHandler = (s, e) =>
            {
                MessageBox.Show($"{firstName} {lastName}\nDepartment: {deptAcronym}", "Student details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // add controls and attach click handlers
            card.Controls.Add(rankCircle);
            card.Controls.Add(avatar);
            card.Controls.Add(nameLabel);
            card.Controls.Add(deptLabel);

            card.Click += clickHandler;
            foreach (Control c in card.Controls) c.Click += clickHandler;

            top5MostStudentBorrowers_flp.Controls.Add(card);
        }

        // Produce circular avatar from rectangular image
        private Image CreateCircularAvatarImage(Image src, Size size)
        {
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

                        // Draw the source image scaled to fit square and centered
                        var srcRect = GetScaledSourceRect(src.Size, size);
                        g.DrawImage(src, new Rectangle(0, 0, size.Width, size.Height), srcRect, GraphicsUnit.Pixel);
                    }
                }
                return dest;
            }
            catch
            {
                return CreateInitialsAvatar("", "", size);
            }
        }

        // Calculate source rectangle to crop/scale image while preserving aspect ratio (center-crop)
        private Rectangle GetScaledSourceRect(Size srcSize, Size destSize)
        {
            float srcRatio = (float)srcSize.Width / srcSize.Height;
            float destRatio = (float)destSize.Width / destSize.Height;

            if (srcRatio > destRatio)
            {
                // source is wider -> crop sides
                int newWidth = (int)(srcSize.Height * destRatio);
                int x = (srcSize.Width - newWidth) / 2;
                return new Rectangle(x, 0, newWidth, srcSize.Height);
            }
            else
            {
                // source is taller -> crop top/bottom
                int newHeight = (int)(srcSize.Width / destRatio);
                int y = (srcSize.Height - newHeight) / 2;
                return new Rectangle(0, y, srcSize.Width, newHeight);
            }
        }

        // If no image, create colored circle with initials
        private Image CreateInitialsAvatar(string firstName, string lastName, Size size)
        {
            string initials = GetInitials(firstName, lastName);
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // background color - choose a pleasant purple
                Color bg = Color.FromArgb(90, 80, 170);
                using (var brush = new SolidBrush(bg))
                {
                    g.FillEllipse(brush, 0, 0, size.Width - 1, size.Height - 1);
                }

                // draw initials
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(Color.White))
                {
                    float fontSize = size.Width / 2.2f;
                    using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        g.DrawString(initials, font, brush, new RectangleF(0, 0, size.Width, size.Height), sf);
                    }
                }
            }
            return bmp;
        }

        private string GetInitials(string firstName, string lastName)
        {
            string a = string.IsNullOrWhiteSpace(firstName) ? "" : firstName.Trim()[0].ToString().ToUpper();
            string b = string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim()[0].ToString().ToUpper();
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return "U";
            if (string.IsNullOrEmpty(b)) return a;
            if (string.IsNullOrEmpty(a)) return b;
            return a + b;
        }

        // ==========================================
        // SHOW DETAILS
        // ==========================================
        private void ShowBookDetails(Book book)
        {
            if (book == null) return;

            MessageBox.Show(
                $"Title: {book.Title}\nAuthor: {book.Author}\nYear: {book.Year}",
                "Book Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private string TruncateText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > maxChars ? text.Substring(0, maxChars - 3) + "..." : text;
        }
    }
}