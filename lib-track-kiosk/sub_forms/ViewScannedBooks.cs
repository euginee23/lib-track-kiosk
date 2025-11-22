using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using lib_track_kiosk.user_control_forms;

namespace lib_track_kiosk.sub_forms
{
    public partial class ViewScannedBooks : Form
    {
        private readonly List<(int bookId, string bookNumber)> _books;
        private readonly List<int> _researchPaperIds;
        private readonly UC_Borrow _borrowUC;
        private readonly HttpClient _httpClient;

        public ViewScannedBooks(
            List<(int bookId, string bookNumber)> books,
            List<int> researchPaperIds,
            UC_Borrow borrowUC)
        {
            InitializeComponent();
            _books = books ?? new List<(int, string)>();
            _researchPaperIds = researchPaperIds ?? new List<int>();
            _borrowUC = borrowUC;

            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };

            this.Load += ViewScannedBooks_Load;
        }

        private async void ViewScannedBooks_Load(object sender, EventArgs e)
        {
            try
            {
                bookResearch_dgv.AutoGenerateColumns = false;
                bookResearch_dgv.Rows.Clear();

                await LoadBookData();
                await LoadResearchPaperData();

                bookResearch_dgv.CellContentClick -= BookResearch_dgv_CellContentClick;
                bookResearch_dgv.CellContentClick += BookResearch_dgv_CellContentClick;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error loading data: {ex.Message}");
            }
        }

        private async Task LoadBookData()
        {
            foreach (var book in _books)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"books/book/{book.bookId}");
                    string typeIcon = "📘 Book";

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject obj = JObject.Parse(json);
                        var data = obj["data"];

                        string title = data?["book_title"]?.ToString() ?? "Unknown Title";
                        string author = data?["author"]?.ToString() ?? "Unknown Author";

                        int rowIndex = bookResearch_dgv.Rows.Add(typeIcon, title, author, "Remove", "View");
                        bookResearch_dgv.Rows[rowIndex].Tag = new
                        {
                            Type = "Book",
                            BookId = book.bookId,
                            BookNumber = book.bookNumber
                        };
                    }
                    else
                    {
                        bookResearch_dgv.Rows.Add(typeIcon, $"Book ID {book.bookId}", "❌ Not Found", "Remove", "View");
                    }
                }
                catch (Exception ex)
                {
                    bookResearch_dgv.Rows.Add("📘 Book", $"Book ID {book.bookId}", $"⚠️ {ex.Message}", "Remove", "View");
                }
            }
        }

        private async Task LoadResearchPaperData()
        {
            foreach (int paperId in _researchPaperIds)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"research-papers/{paperId}");
                    string typeIcon = "📄 Research Paper";

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject obj = JObject.Parse(json);
                        var data = obj["data"];

                        string title = data?["research_title"]?.ToString() ?? "Unknown Title";
                        string authors = data?["authors"]?.ToString() ?? "Unknown Author(s)";

                        int rowIndex = bookResearch_dgv.Rows.Add(typeIcon, title, authors, "Remove", "View");
                        bookResearch_dgv.Rows[rowIndex].Tag = new
                        {
                            Type = "Research Paper",
                            ResearchPaperId = paperId
                        };
                    }
                    else
                    {
                        bookResearch_dgv.Rows.Add(typeIcon, $"Research Paper ID {paperId}", "❌ Not Found", "Remove", "View");
                    }
                }
                catch (Exception ex)
                {
                    bookResearch_dgv.Rows.Add("📄 Research Paper", $"Research Paper ID {paperId}", $"⚠️ {ex.Message}", "Remove", "View");
                }
            }
        }

        private void BookResearch_dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            string columnName = bookResearch_dgv.Columns[e.ColumnIndex].HeaderText;

            if (columnName.Equals("Remove", StringComparison.OrdinalIgnoreCase))
            {
                HandleRemove(e.RowIndex);
            }
            else if (columnName.Equals("View", StringComparison.OrdinalIgnoreCase))
            {
                HandleView(e.RowIndex);
            }
        }

        private void HandleRemove(int rowIndex)
        {
            try
            {
                var row = bookResearch_dgv.Rows[rowIndex];
                string title = row.Cells["Column2"].Value?.ToString();
                string type = row.Cells["Column1"].Value?.ToString();

                var confirm = MessageBox.Show(
                    $"Remove '{title}' ({type}) from the list?",
                    "Confirm Remove",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirm == DialogResult.Yes)
                {
                    bookResearch_dgv.Rows.RemoveAt(rowIndex);
                    MessageBox.Show($"✅ '{title}' removed successfully.", "Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error removing item: {ex.Message}");
            }
        }

        private void HandleView(int rowIndex)
        {
            try
            {
                dynamic tagData = bookResearch_dgv.Rows[rowIndex].Tag;
                if (tagData == null)
                {
                    MessageBox.Show("⚠️ Missing data for selected item.");
                    return;
                }

                if (tagData.Type == "Book")
                {
                    _borrowUC.ShowScannedItem("Book", tagData.BookId, tagData.BookNumber);
                }
                else if (tagData.Type == "Research Paper")
                {
                    _borrowUC.ShowScannedItem("Research Paper", tagData.ResearchPaperId);
                }

                // ✅ Close form after view
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error opening view: {ex.Message}");
            }
        }

        private void closeView_btn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
