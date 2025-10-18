using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.helpers;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_ScannedBookInformation : UserControl
    {
        private readonly int _bookId;
        private readonly string _bookNumber;

        public UC_ScannedBookInformation(int bookId, string bookNumber)
        {
            InitializeComponent();
            _bookId = bookId;
            _bookNumber = bookNumber;
            this.Load += UC_ScannedBookInformation_Load;
        }

        private async void UC_ScannedBookInformation_Load(object sender, EventArgs e)
        {
            await LoadBookInformationAsync();
        }

        private async Task LoadBookInformationAsync()
        {
            try
            {
                // ✅ Fetch the book info using reusable BookFetcher
                var book = await BookFetcher.GetBookAsync(_bookId, _bookNumber);

                // 🧾 Populate UI controls
                bookTitle_rtbx.Text = book.Title;
                authors_rtbx.Text = book.Author;
                publishers_rtbx.Text = book.Publisher;
                shelfLocation_lbl.Text = book.ShelfLocation;
                year_lbl.Text = book.Year;
                edition_lbl.Text = book.Edition;
                price_lbl.Text = book.Price;
                donor_lbl.Text = book.Donor;
                bookNumberCopy_lbl.Text = book.BookNumber ?? "N/A";
                status_lbl.Text = book.Status;
                copiesAvailable_lbl.Text = book.AvailableCopies.ToString();

                // 🖼️ Handle cover image
                if (book.CoverImage != null)
                {
                    cover_pbx.Image = book.CoverImage;
                }
                else
                {
                    cover_pbx.ImageLocation = @"E:\Library-Tracker\lib-track-kiosk\images\no-cover.png";
                }

                cover_pbx.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error loading book info: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
