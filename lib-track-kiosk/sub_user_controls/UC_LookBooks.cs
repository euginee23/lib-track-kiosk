using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using lib_track_kiosk.models;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookBooks : UserControl
    {
        private List<Book> _books = new List<Book>();

        public UC_LookBooks()
        {
            InitializeComponent();
            LoadBooks();
        }

        private void LoadBooks()
        {
            string path1 = @"E:\Library-Tracker\dev files\input samples\book\qwe.jpg";
            string path2 = @"E:\Library-Tracker\dev files\input samples\book\qwer.jpg";
            string path3 = @"E:\Library-Tracker\dev files\input samples\book\qwert.jpg";

            _books = new List<Book>
            {
                new Book { Id = 1, Title = "C# Programming: From Basics to Advanced Applications in .NET Development", Author = "John Doe, Michael White, Sarah Connor, Others", Year = 2021, CoverImagePath = path1 },
                new Book { Id = 2, Title = "Learning Artificial Intelligence with Real-World Projects", Author = "Jane Smith, A. Johnson", Year = 2023, CoverImagePath = path2 },
                new Book { Id = 3, Title = "Database Systems: The Complete Reference", Author = "Andrew Tanenbaum, C. J. Date, Others", Year = 2020, CoverImagePath = path3 },
                new Book { Id = 4, Title = "Object-Oriented Programming in Depth", Author = "Robert Martin, Alan Kay, Barbara Liskov, Others", Year = 2019, CoverImagePath = path1 },
                new Book { Id = 5, Title = "Modern Web Development with React and Node.js", Author = "Mark Brown, Lisa Johnson", Year = 2024, CoverImagePath = path2 },
                new Book { Id = 6, Title = "Data Structures and Algorithms Illustrated", Author = "David Knuth, Grace Hopper, Others", Year = 2022, CoverImagePath = path3 },
                new Book { Id = 7, Title = "Exploring Quantum Computing", Author = "Niels Bohr, Richard Feynman", Year = 2025, CoverImagePath = path1 },
                new Book { Id = 8, Title = "Cybersecurity Essentials for the Modern Age", Author = "Edward Snowden, Alice Li, Others", Year = 2021, CoverImagePath = path2 },
                new Book { Id = 9, Title = "Machine Learning: Concepts, Applications, and Future Trends", Author = "Andrew Ng, Ian Goodfellow, Others", Year = 2023, CoverImagePath = path3 },
                new Book { Id = 10, Title = "The Art of Computer Programming", Author = "Donald Knuth", Year = 2011, CoverImagePath = path1 },
                new Book { Id = 11, Title = "Deep Learning for Vision Systems", Author = "Fei-Fei Li, Alex Krizhevsky", Year = 2024, CoverImagePath = path2 },
                new Book { Id = 12, Title = "Cloud Infrastructure and DevOps Automation", Author = "Kelsey Hightower, Brendan Burns", Year = 2023, CoverImagePath = path3 },
                new Book { Id = 13, Title = "Human-Computer Interaction Design", Author = "Don Norman, Jakob Nielsen, Others", Year = 2020, CoverImagePath = path1 },
                new Book { Id = 14, Title = "Introduction to Embedded Systems", Author = "Peter Marwedel, David Patterson", Year = 2018, CoverImagePath = path2 },
                new Book { Id = 15, Title = "Artificial Intelligence: Principles and Practice", Author = "Stuart Russell, Peter Norvig", Year = 2022, CoverImagePath = path3 }
            };

            DisplayBooks();
        }

        private void DisplayBooks()
        {
            books_FlowLayoutPanel.Controls.Clear();

            // Enable smooth scrolling for large content
            books_FlowLayoutPanel.AutoScroll = true;

            foreach (var book in _books)
                AddBookCard(book);
        }

        private void AddBookCard(Book book)
        {
            Panel card = new Panel
            {
                Width = 240,
                Height = 400,
                BackColor = Color.White,
                Margin = new Padding(20),
                Tag = book.Id,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Hover shadow effect
            card.MouseEnter += (s, e) =>
            {
                card.BackColor = Color.FromArgb(250, 250, 250);
                card.Padding = new Padding(2);
            };
            card.MouseLeave += (s, e) =>
            {
                card.BackColor = Color.White;
                card.Padding = new Padding(0);
            };

            PictureBox cover = new PictureBox
            {
                Width = 210,
                Height = 270,
                Left = 15,
                Top = 15,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            if (File.Exists(book.CoverImagePath))
                cover.Image = Image.FromFile(book.CoverImagePath);

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
                Top = 380,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"📅 {book.Year}"
            };

            card.Controls.Add(cover);
            card.Controls.Add(title);
            card.Controls.Add(author);
            card.Controls.Add(year);

            card.DoubleClick += (s, e) => OpenBookDetails(book);

            books_FlowLayoutPanel.Controls.Add(card);
        }

        private void OpenBookDetails(Book book)
        {
            MessageBox.Show(
                $"📘 {book.Title}\n👤 {book.Author}\n📅 {book.Year}",
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
    }
}
