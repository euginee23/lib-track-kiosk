using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.configs;
using lib_track_kiosk.helpers;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_GenerateReceipt : UserControl
    {
        private readonly string userType;
        private readonly int? userId;

        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        public string ReferenceNumber => referenceNumber_lbl.Text;
        public DateTime TransactionDate => DateTime.Parse(transactionDate_lbl.Text);
        public DateTime DueDate => DateTime.Parse(dueDate_lbl.Text);
        public int? UserId => userId;
        public UC_GenerateReceipt(
            string userType = "Student",
            int? userId = null,
            List<(int bookId, string bookNumber)> scannedBooks = null,
            List<int> scannedResearchPapers = null
        )
        {
            InitializeComponent();
            this.userType = userType;
            this.userId = userId;

            if (scannedBooks != null) this.scannedBooks = scannedBooks;
            if (scannedResearchPapers != null) this.scannedResearchPapers = scannedResearchPapers;

            this.Load += UC_GenerateReceipt_Load;
        }

        private async void UC_GenerateReceipt_Load(object sender, EventArgs e)
        {
            transactionDate_lbl.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            referenceNumber_lbl.Text = GenerateReferenceNumber();

            await SetDueDateAsync();

            if (userId.HasValue)
            {
                await LoadUserInformationAsync(userId.Value);
            }

            await LoadScannedItemsAsync();
        }

        private string GenerateReferenceNumber()
        {
            return $"REF-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
        }

        private async Task SetDueDateAsync()
        {
            try
            {
                int borrowDays = await SystemSettingsFetcher.GetBorrowDaysAsync(userType);

                DateTime dueDate = DateTime.Now.AddDays(borrowDays);
                dueDate_lbl.Text = dueDate.ToString("MMMM dd, yyyy");

                Console.WriteLine($"✅ Settings fetched. Borrow days for {userType}: {borrowDays}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch system settings:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadUserInformationAsync(int userId)
        {
            try
            {
                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto) =
                    await UserFetcher.GetUserInfoAsync(userId);

                fullName_lbl.Text = fullName;
                contactNumber_lbl.Text = contactNumber;
                email_lbl.Text = email;
                yearAndDepartment_lbl.Text = $"{yearLevel} - {department}";

                Console.WriteLine($"✅ User info loaded for {fullName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch user info:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadScannedItemsAsync()
        {
            borrowed_dgv.Rows.Clear();

            borrowed_dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            borrowed_dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Load books
            foreach (var (bookId, bookNumber) in scannedBooks)
            {
                try
                {
                    BookInfo book = await BookFetcher.GetBookAsync(bookId, bookNumber);

                    borrowed_dgv.Rows.Add(
                        book.Title,
                        book.Author,
                        book.ShelfLocation,
                        "Book"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to fetch book {bookId}: {ex.Message}");
                }
            }

            // Load research papers
            foreach (var researchId in scannedResearchPapers)
            {
                try
                {
                    ResearchPaper paper = await ResearchPaperFetcher.GetResearchPaperAsync(researchId);

                    borrowed_dgv.Rows.Add(
                        paper.Title,
                        paper.Authors,
                        paper.ShelfLocation,
                        "Research Paper"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to fetch research paper {researchId}: {ex.Message}");
                }
            }

            // Resize rows after adding content
            borrowed_dgv.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        public async Task UpdateScannedItemsAsync(
            List<(int bookId, string bookNumber)> newScannedBooks,
            List<int> newScannedResearchPapers
        )
        {
            // Replace current lists
            scannedBooks = new List<(int bookId, string bookNumber)>(newScannedBooks);
            scannedResearchPapers = new List<int>(newScannedResearchPapers);

            // Reload the DataGridView
            await LoadScannedItemsAsync();
        }
    }
}
