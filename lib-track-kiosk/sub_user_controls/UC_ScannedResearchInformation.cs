using lib_track_kiosk.helpers;
using lib_track_kiosk.sub_forms;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_ScannedResearchInformation : UserControl
    {
        private readonly int researchPaperId;
        private string researchPaperAbstract;

        public UC_ScannedResearchInformation(int researchPaperId)
        {
            InitializeComponent();
            this.researchPaperId = researchPaperId;
            this.Load += UC_ScannedResearchInformation_Load;
        }

        private async void UC_ScannedResearchInformation_Load(object sender, EventArgs e)
        {
            await LoadResearchPaperAsync();
        }

        private async Task LoadResearchPaperAsync()
        {
            try
            {
                // ✅ Fetch research paper info using reusable ResearchPaperFetcher
                var paper = await ResearchPaperFetcher.GetResearchPaperAsync(researchPaperId);

                // 🧾 Populate UI controls
                title_rtbx.Text = paper.Title;
                authors_rtbx.Text = paper.Authors;
                shelfLocation_lbl.Text = paper.ShelfLocation;
                year_lbl.Text = paper.Year;
                department_lbl.Text = paper.Department;
                price_lbl.Text = paper.Price;
                status_lbl.Text = paper.Status;

                // Save abstract for later viewing
                researchPaperAbstract = paper.Abstract;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Error loading research paper info: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🔹 View Abstract button click
        private void viewAbstract_btn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(researchPaperAbstract))
            {
                var abstractForm = new ViewResearchAbstract();
                abstractForm.SetAbstract(researchPaperAbstract);
                abstractForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("No abstract available for this research paper.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
