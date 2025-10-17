using lib_track_kiosk.panel_forms;
using lib_track_kiosk.sub_forms;
using lib_track_kiosk.sub_user_controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_Borrow : UserControl
    {
        // 🧠 Storage for scanned data (not displayed immediately)
        private List<(int bookId, string bookNumber)> scannedBooks = new List<(int, string)>();
        private List<int> scannedResearchPapers = new List<int>();

        public UC_Borrow()
        {
            InitializeComponent();
            this.Load += UC_Borrow_Load;
        }

        private async void UC_Borrow_Load(object sender, EventArgs e)
        {
            LoadReceiptGenerationPanel();

            // Step 1: Scan Fingerprint only
            using (var scanFingerprint = new ScanFingerprint())
            {
                if (scanFingerprint.ShowDialog() == DialogResult.OK)
                {
                    int? userId = scanFingerprint.ScannedUserId;
                    if (!userId.HasValue)
                    {
                        MessageBox.Show("⚠️ No valid user detected.");
                        return;
                    }

                    userId_lbl.Text = userId.Value.ToString(); // for display only

                    // ✅ Fetch and display user info from backend
                    await FetchAndDisplayUserInformation(userId.Value);
                }
            }
        }

        /// <summary>
        /// Fetches and displays user info from backend using userId (from scanned fingerprint)
        /// </summary>
        private async Task FetchAndDisplayUserInformation(int userId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = $"http://localhost:5000/api/users/registrations/{userId}";
                    Console.WriteLine($"🌐 Fetching user info from: {apiUrl}");

                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonResponse);
                        var user = data["user"];

                        if (user != null)
                        {
                            // Fill labels
                            fullName_lbl.Text = $"{user["first_name"]} {user["last_name"]}";
                            email_lbl.Text = user["email"]?.ToString() ?? "N/A";
                            contactNumber_lbl.Text = user["contact_number"]?.ToString() ?? "N/A";
                            department_lbl.Text = user["department_name"]?.ToString() ?? "N/A";
                            position_lbl.Text = "Student"; // static for now
                            yearLevel_lbl.Text = user["year_level"]?.ToString() ?? "N/A";
                            pendingPenalties_lbl.Text = user["penalties"]?.ToString() ?? "0";
                            fines_lbl.Text = user["fines"]?.ToString() ?? "₱0.00";
                            booksCurrentlyBorrowed_lbl.Text = user["books_borrowed"]?.ToString() ?? "0";
                            maxBooksThatCanBorrow_lbl.Text = user["max_books"]?.ToString() ?? "5";

                            // Load profile image
                            string imageUrl = user["profile_image"]?.ToString();
                            if (!string.IsNullOrEmpty(imageUrl))
                                profile_pictureBox.ImageLocation = imageUrl;
                            else
                                profile_pictureBox.ImageLocation = @"E:\Library-Tracker\lib-track-admin\public\avatar-default.png";

                            profile_pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                        }
                        else
                        {
                            MessageBox.Show("⚠️ User data not found in response.");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"❌ Failed to fetch user info. Status: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🔥 Error fetching user info: {ex.Message}");
            }
        }

        // 🚪 Exit back to welcome
        private void exitBorrow_btn_Click(object sender, EventArgs e)
        {
            MainForm mainForm = (MainForm)this.ParentForm;
            if (mainForm != null)
            {
                UC_Welcome welcomeScreen = new UC_Welcome();
                mainForm.addUserControl(welcomeScreen);
            }
        }

        // 🧾 Load receipt generation section
        private void LoadReceiptGenerationPanel()
        {
            generateReceipt_panel.Controls.Clear();
            var generateReceiptUC = new UC_GenerateReceipt();
            generateReceiptUC.Dock = DockStyle.Fill;
            generateReceipt_panel.Controls.Add(generateReceiptUC);
            Console.WriteLine("✅ Receipt generation panel loaded.");
        }

        // ➕ Add more scanned items (manually triggered now)
        private void add_btn_Click(object sender, EventArgs e)
        {
            using (var scanForm = new ScanBookQR())
            {
                if (scanForm.ShowDialog() == DialogResult.OK)
                {
                    string scannedType = scanForm.ScannedType;

                    // 📘 Book
                    if (scannedType == "Book" &&
                        scanForm.ScannedBookId.HasValue &&
                        !string.IsNullOrEmpty(scanForm.ScannedBookNumber))
                    {
                        int bookId = scanForm.ScannedBookId.Value;
                        string bookNumber = scanForm.ScannedBookNumber;
                        Console.WriteLine($"➕ Added Book: ID={bookId}, Number={bookNumber}");
                        scannedBooks.Add((bookId, bookNumber));
                    }
                    // 📄 Research Paper
                    else if (scannedType == "Research Paper" &&
                             scanForm.ScannedResearchPaperId.HasValue)
                    {
                        int researchPaperId = scanForm.ScannedResearchPaperId.Value;
                        Console.WriteLine($"➕ Added Research Paper: ID={researchPaperId}");
                        scannedResearchPapers.Add(researchPaperId);
                    }
                    else
                    {
                        MessageBox.Show("⚠️ Invalid scan data. Please try again.");
                    }
                }
            }
        }

        // 👁️ View all stored scans
        private void viewScannedBooks_btn_Click(object sender, EventArgs e)
        {
            using (var viewForm = new ViewScannedBooks(scannedBooks, scannedResearchPapers, this))
            {
                viewForm.ShowDialog();
            }
        }

        // 📖 Public method for displaying scanned info dynamically
        public void ShowScannedItem(string type, int id, string bookNumber = null)
        {
            scannedType_panel.Controls.Clear();

            if (type == "Book")
            {
                var bookInfoUC = new UC_ScannedBookInformation(id, bookNumber);
                bookInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(bookInfoUC);
                Console.WriteLine($"📘 Displayed Book Info: ID={id}, BookNumber={bookNumber}");
            }
            else if (type == "Research Paper")
            {
                var researchInfoUC = new UC_ScannedResearchPaperInformation(id);
                researchInfoUC.Dock = DockStyle.Fill;
                scannedType_panel.Controls.Add(researchInfoUC);
                Console.WriteLine($"📄 Displayed Research Paper Info: ID={id}");
            }
        }
    }

    internal class UC_ScannedResearchPaperInformation : Control
    {
        private readonly int researchPaperId;
        public UC_ScannedResearchPaperInformation(int researchPaperId)
        {
            this.researchPaperId = researchPaperId;
        }
    }
}
