using lib_track_kiosk.helpers;
using lib_track_kiosk.panel_forms;
using lib_track_kiosk.sub_forms;
using lib_track_kiosk.sub_user_controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Borrow : UserControl
    {
        // STORAGE FOR SCANNED ITEMS
        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        private int? currentUserId;

        private UC_GenerateReceipt receiptUCInstance;

        public UC_Borrow()
        {
            InitializeComponent();
            this.Load += UC_Borrow_Load;
        }

        // FORM LOAD - SCAN FINGERPRINT 
        private async void UC_Borrow_Load(object sender, EventArgs e)
        {
            using (var scanFingerprint = new ScanFingerprint())
            {
                var result = scanFingerprint.ShowDialog();

                if (result == DialogResult.Cancel)
                {
                    MainForm mainForm = (MainForm)this.ParentForm;
                    if (mainForm != null)
                    {
                        UC_Welcome welcomeScreen = new UC_Welcome();
                        mainForm.addUserControl(welcomeScreen);
                    }
                    return;
                }

                if (result == DialogResult.OK)
                {
                    int? userId = scanFingerprint.ScannedUserId;
                    if (!userId.HasValue)
                    {
                        MessageBox.Show("⚠️ No valid user detected.");
                        return;
                    }

                    await FetchAndDisplayUserInformation(userId.Value);
                }
            }
        }

        // FETCH USER INFORMATION FROM API
        private async Task FetchAndDisplayUserInformation(int userId)
        {
            try
            {
                var (fullName, email, contactNumber, department, position, yearLevel, profilePhoto)
                    = await UserFetcher.GetUserInfoAsync(userId);

                fullName_lbl.Text = fullName;
                email_lbl.Text = email;
                contactNumber_lbl.Text = contactNumber;
                department_lbl.Text = department;
                position_lbl.Text = position;
                yearLevel_lbl.Text = yearLevel;

                pendingPenalties_lbl.Text = "0";
                fines_lbl.Text = "₱0.00";
                booksCurrentlyBorrowed_lbl.Text = "0";

                // DISPLAY PROFILE PHOTO
                if (profilePhoto != null)
                {
                    profile_pictureBox.Image = profilePhoto;
                }
                else
                {
                    profile_pictureBox.ImageLocation = @"E:\Library-Tracker\lib-track-admin\public\avatar-default.png";
                }
                profile_pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

                // LOAD BORROWING LIMITS
                string userType = position ?? "Student";
                var (_, _, studentMaxBooks, facultyMaxBooks) =
                    await lib_track_kiosk.configs.SystemSettingsFetcher.GetBorrowingLimitsAsync();

                int maxBooks = userType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                    ? studentMaxBooks
                    : facultyMaxBooks;

                maxBooksThatCanBorrow_lbl.Text = maxBooks.ToString();

                LoadReceiptGenerationPanel(userType, userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error fetching user info: {ex.Message}");
            }
        }

        // EXIT BORROW PANEL
        private void exitBorrow_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Welcome welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
            }
        }

        // LOAD RECEIPT GENERATION PANEL
        private void LoadReceiptGenerationPanel(string userType, int userId)
        {
            if (receiptUCInstance == null)
            {
                receiptUCInstance = new UC_GenerateReceipt(
                    userType,
                    userId,
                    scannedBooks,
                    scannedResearchPapers
                );
                receiptUCInstance.Dock = DockStyle.Fill;
                generateReceipt_panel.Controls.Clear();
                generateReceipt_panel.Controls.Add(receiptUCInstance);
            }
        }


        // ADD BOOKS OR RESEARCH PAPERS
        private void add_btn_Click(object sender, EventArgs e)
        {
            using (var scanForm = new ScanBookQR())
            {
                if (scanForm.ShowDialog() == DialogResult.OK)
                {
                    string scannedType = scanForm.ScannedType;

                    scannedType_panel.Controls.Clear();

                    if (scannedType == "Book" &&
                        scanForm.ScannedBookId.HasValue &&
                        !string.IsNullOrEmpty(scanForm.ScannedBookNumber))
                    {
                        int bookId = scanForm.ScannedBookId.Value;
                        string bookNumber = scanForm.ScannedBookNumber;
                        scannedBooks.Add((bookId, bookNumber));

                        var bookInfoUC = new UC_ScannedBookInformation(bookId, bookNumber);
                        bookInfoUC.Dock = DockStyle.Fill;
                        scannedType_panel.Controls.Add(bookInfoUC);
                    }
                    // 📄 Research Paper
                    else if (scannedType == "Research Paper" &&
                             scanForm.ScannedResearchPaperId.HasValue)
                    {
                        int researchPaperId = scanForm.ScannedResearchPaperId.Value;

                        scannedResearchPapers.Add(researchPaperId);

                        var researchInfoUC = new UC_ScannedResearchInformation(researchPaperId);
                        researchInfoUC.Dock = DockStyle.Fill;
                        scannedType_panel.Controls.Add(researchInfoUC);
                    }
                    else
                    {
                        MessageBox.Show("⚠️ Invalid scan data. Please try again.");
                    }
                }
            }

            if (receiptUCInstance != null)
            {
                receiptUCInstance.UpdateScannedItemsAsync(scannedBooks, scannedResearchPapers);
            }
        }

        // VIEW SCANNED BOOKS OR RESEARCH PAPERS
        private void viewScannedBooks_btn_Click(object sender, EventArgs e)
        {
            using (var viewForm = new ViewScannedBooks(scannedBooks, scannedResearchPapers, this))
            {
                viewForm.ShowDialog();
            }
        }

        // DYNAMIC DISPLAY OF SCANNED ITEM
        public void ShowScannedItem(string type, int itemId, string bookNumber = null)
        {
            scannedType_panel.Controls.Clear();

            if (type == "Book")
            {
                var bookInfoUC = new UC_ScannedBookInformation(itemId, bookNumber);
                bookInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(bookInfoUC);

            }
            else if (type == "Research Paper")
            {
                var researchInfoUC = new UC_ScannedResearchInformation(researchPaperId: itemId);
                researchInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(researchInfoUC);

            }
        }

        // BORROW BUTTON CLICK
        private async void borrow_btn_Click(object sender, EventArgs e)
        {
            if (scannedBooks.Count == 0 && scannedResearchPapers.Count == 0)
            {
                MessageBox.Show("⚠️ No books or research papers scanned. Please add items first.");
                return;
            }

            if (receiptUCInstance == null || !receiptUCInstance.UserId.HasValue)
            {
                MessageBox.Show("⚠️ No valid user detected.");
                return;
            }

            // BUILD BORROW REQUEST
            var borrowRequest = new BorrowRequest
            {
                ReferenceNumber = receiptUCInstance.ReferenceNumber,
                UserId = receiptUCInstance.UserId.Value,
                DueDate = receiptUCInstance.DueDate
            };

            // ADD SCANNED BOOKS
            foreach (var (bookId, _) in scannedBooks)
                borrowRequest.BookIds.Add(bookId);

            // ADD SCANNED RESEARCH PAPERS  
            borrowRequest.ResearchPaperIds.AddRange(scannedResearchPapers);

            // ATTACH RECEIPT IMAGE
            string receiptPath = GenerateReceiptImageFromPanel();
            if (!string.IsNullOrEmpty(receiptPath))
                borrowRequest.ReceiptFilePath = receiptPath;

            try
            {
                var response = await BorrowFetcher.BorrowAsync(borrowRequest);

                if (response.Success)
                {
                    MessageBox.Show($"✅ Borrow successful!\nItems borrowed: {borrowRequest.BookIds.Count + borrowRequest.ResearchPaperIds.Count}\nReference: {borrowRequest.ReferenceNumber}");

                    // CLEAR SCANNED ITEMS
                    scannedBooks.Clear();
                    scannedResearchPapers.Clear();
                    scannedType_panel.Controls.Clear();

                    // UPDATE RECEIPT PANEL
                    receiptUCInstance?.UpdateScannedItemsAsync(scannedBooks, scannedResearchPapers);

                    // CONFIRMATION - RETURN TO WELCOME SCREEN
                    MainForm mainForm = this.ParentForm as MainForm;
                    if (mainForm != null)
                    {
                        UC_Welcome welcomeScreen = new UC_Welcome();
                        mainForm.addUserControl(welcomeScreen);
                    }
                }
                else
                {
                    MessageBox.Show($"❌ Borrow failed: {response.Message}\nError: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Unexpected error: {ex.Message}");
            }
        }

        // HELPER TO GENERATE RECEIPT IMAGE
        private string GenerateReceiptImageFromPanel()
        {
            if (generateReceipt_panel.Controls.Count == 0)
                return null;

            Bitmap bitmap = new Bitmap(generateReceipt_panel.Width, generateReceipt_panel.Height);
            generateReceipt_panel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            string tempPath = Path.Combine(Path.GetTempPath(), $"Receipt_{DateTime.Now:yyyyMMddHHmmss}.jpg");
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            return tempPath;
        }
    }

    // USER CONTROL FOR DISPLAYING SCANNED RESEARCH PAPER INFO
    internal class UC_ScannedResearchPaperInformation : Control
    {
        private readonly int researchPaperId;
        public UC_ScannedResearchPaperInformation(int researchPaperId)
        {
            this.researchPaperId = researchPaperId;
        }
    }
}
