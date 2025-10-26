using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using lib_track_kiosk.models;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.sub_user_controls
{
    public partial class UC_LookResearchPapers : UserControl
    {
        private List<ResearchPaper> _researchPapers = new List<ResearchPaper>();

        private readonly List<Color> _accentColors = new List<Color>
        {
            Color.FromArgb(20, 54, 100),
            Color.FromArgb(15, 86, 63),
            Color.FromArgb(115, 28, 36),
            Color.FromArgb(72, 42, 95),
            Color.FromArgb(85, 93, 80),
            Color.FromArgb(140, 60, 12),
            Color.FromArgb(12, 93, 93),
            Color.FromArgb(100, 20, 60),
            Color.FromArgb(60, 60, 60),
            Color.FromArgb(44, 62, 80)
        };

        private readonly Random _rand = new Random();

        // On-screen keyboard helper (reusable)
        private OnScreenKeyboard _osk;

        // track currently selected card so we can toggle selection visuals
        private Panel _selectedCard;

        public UC_LookResearchPapers()
        {
            InitializeComponent();

            // instantiate keyboard helper (lazy alternative is possible)
            _osk = new OnScreenKeyboard();

            // Wire keyboard open/close to the search textbox if it exists
            if (search_txtBox != null)
            {
                search_txtBox.GotFocus += SearchTxtBox_GotFocus;
                search_txtBox.Enter += SearchTxtBox_GotFocus;
                search_txtBox.Click += SearchTxtBox_Click;
                search_txtBox.LostFocus += SearchTxtBox_LostFocus;
            }

            // clean up on disposal (avoid designer Dispose conflict)
            this.Disposed += UC_LookResearchPapers_Disposed;

            // reduce flicker by enabling double buffering on the flow panel (non-public property)
            TryEnableDoubleBuffer(research_FlowLayoutPanel);

            LoadResearchPapers();
            abstract_rtbx.Text = "📄 Please select a research paper to view its abstract.";

            // 🖱️ Scroll button handlers
            scrollUp_btn.Click += ScrollUp_btn_Click;
            scrollDown_btn.Click += ScrollDown_btn_Click;
        }

        private void UC_LookResearchPapers_Disposed(object sender, EventArgs e)
        {
            try
            {
                if (search_txtBox != null)
                {
                    search_txtBox.GotFocus -= SearchTxtBox_GotFocus;
                    search_txtBox.Enter -= SearchTxtBox_GotFocus;
                    search_txtBox.Click -= SearchTxtBox_Click;
                    search_txtBox.LostFocus -= SearchTxtBox_LostFocus;
                }
            }
            catch { /* ignore */ }

            try
            {
                _osk?.Dispose();
            }
            catch { /* ignore */ }
        }

        private void SearchTxtBox_Click(object sender, EventArgs e) => TryOpenKeyboard();
        private void SearchTxtBox_GotFocus(object sender, EventArgs e) => TryOpenKeyboard();

        private async void SearchTxtBox_LostFocus(object sender, EventArgs e)
        {
            // Debounce so quick focus moves within this control don't close the keyboard immediately
            await Task.Delay(150);
            try
            {
                // If focus has left this UserControl entirely, close the keyboard
                if (!this.ContainsFocus)
                    TryCloseKeyboard();
            }
            catch { /* best-effort */ }
        }

        private void TryOpenKeyboard()
        {
            try { _osk?.Open(); } catch { /* ignore */ }
        }

        private void TryCloseKeyboard()
        {
            try { _osk?.Close(); } catch { /* ignore */ }
        }

        private void LoadResearchPapers()
        {
            _researchPapers = new List<ResearchPaper>
            {
                new ResearchPaper { Id = 1, Title = "THE PERCEPTION ON THE POLITICAL DEVELOPMENT OF THE MUNICIPALITY OF AURORA FROM THE COMMONWEALTH PERIOD UP TO THE 2004 NATIONAL AND LOCAL ELECTION", Authors = "Jane Doe, Michael Green, Peter Cruz, Anna Villanueva, Ronald Flores", Year = 2024, Abstract = "This study investigates the political development of the Municipality of Aurora spanning several decades..." },
                new ResearchPaper { Id = 2, Title = "Evaluating Data Privacy Techniques in Cloud Computing", Authors = "John Smith, Lisa Brown, Daniel Rivera", Year = 2023, Abstract = "This paper provides a comparative evaluation of data privacy mechanisms used in cloud environments..." },
                new ResearchPaper { Id = 3, Title = "Blockchain Applications for Secure Health Records", Authors = "Alice Tan, Robert Miles, Francis Lim, Chloe Santos", Year = 2022, Abstract = "This study explores blockchain technology as a decentralized framework for securing patient health data..." },
                new ResearchPaper { Id = 4, Title = "Quantum Machine Learning: Concepts and Challenges", Authors = "David Liu, Sarah Connor", Year = 2025, Abstract = "Quantum machine learning merges quantum computing principles with AI..." },
                new ResearchPaper { Id = 5, Title = "Natural Language Processing in Educational Systems", Authors = "Paul Adams, Maria Santos, Henry Tan, Grace Yu", Year = 2021, Abstract = "This paper examines how NLP enhances personalized learning..." },
                new ResearchPaper { Id = 6, Title = "Computer Vision for Smart Cities", Authors = "Henry Zhao, Bryan Chua, Patricia Lim", Year = 2024, Abstract = "The research investigates the deployment of computer vision algorithms..." },
                new ResearchPaper { Id = 7, Title = "Augmented Reality for Interactive Learning", Authors = "Lisa Cruz, James Lee, Arthur Gomez", Year = 2022, Abstract = "This study evaluates AR-based tools for enhancing classroom engagement..." },
                new ResearchPaper { Id = 8, Title = "Cybersecurity Threat Modeling in IoT Devices", Authors = "Sean Yu, Claire Tan", Year = 2023, Abstract = "The research explores threat modeling strategies for Internet of Things ecosystems..." },
                new ResearchPaper { Id = 9, Title = "Renewable Energy Optimization using AI Algorithms", Authors = "Dennis Flores, Carlos Rivera, Nina Santos", Year = 2025, Abstract = "This research applies AI-driven optimization in renewable energy systems..." },
                new ResearchPaper { Id = 10, Title = "Digital Transformation in Small Enterprises", Authors = "Olivia Cruz, Ethan Walker", Year = 2024, Abstract = "This paper examines how digital adoption affects small business productivity..." },
                new ResearchPaper { Id = 11, Title = "Automated Grading Systems using Neural Networks", Authors = "Mark Reyes, Angela Wong, Peter Tan", Year = 2023, Abstract = "The study proposes a neural network model for grading academic essays automatically..." },
                new ResearchPaper { Id = 12, Title = "The Role of Big Data in Healthcare Prediction", Authors = "Hannah Lee, Joseph Chan", Year = 2024, Abstract = "This research analyzes predictive models leveraging big data for patient diagnostics..." },
                new ResearchPaper { Id = 13, Title = "Sustainable Urban Design through GIS Mapping", Authors = "Raymond Sy, Julia Torres, Anna Lopez", Year = 2021, Abstract = "This study integrates GIS-based analysis for sustainable city planning..." },
                new ResearchPaper { Id = 14, Title = "E-Government Implementation Challenges", Authors = "Victor Tan, Maria dela Cruz", Year = 2022, Abstract = "The research examines the barriers in implementing digital governance systems..." },
                new ResearchPaper { Id = 15, Title = "Machine Ethics in Autonomous Vehicles", Authors = "Albert King, Laura Park, Ron Villanueva", Year = 2025, Abstract = "This paper explores ethical frameworks applied to autonomous vehicle decision-making..." }
            };

            DisplayResearchPapers();
        }

        private void DisplayResearchPapers()
        {
            research_FlowLayoutPanel.Controls.Clear();
            research_FlowLayoutPanel.AutoScroll = true;
            research_FlowLayoutPanel.WrapContents = true;
            research_FlowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;

            foreach (var paper in _researchPapers)
                AddResearchCard(paper);
        }

        private void AddResearchCard(ResearchPaper paper)
        {
            int cardWidth = 410;
            int cardHeight = 150;
            int padding = 16;
            int badgeWidth = 56;
            int badgeHeight = 28;
            int gap = 12;

            Color accent = _accentColors[_rand.Next(_accentColors.Count)];

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                BackColor = Color.White,
                Margin = new Padding(12, 10, 12, 10),
                Tag = paper.Id,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = accent
            };

            int contentWidth = cardWidth - (padding * 2) - badgeWidth - gap;
            if (contentWidth < 180) contentWidth = 180;

            var authorsList = paper.Authors.Split(',').Select(a => a.Trim()).ToList();
            string authorsDisplay = string.Join(", ", authorsList.Take(3));
            if (authorsList.Count > 3) authorsDisplay += " + others";

            Label title = new Label
            {
                AutoSize = false,
                Left = padding,
                Top = 12,
                Width = contentWidth,
                Height = 64,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.TopLeft,
                Text = paper.Title,
                AutoEllipsis = true
            };

            Label authors = new Label
            {
                AutoSize = false,
                Left = padding,
                Top = title.Top + title.Height + 6,
                Width = contentWidth,
                Height = 22,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = authorsDisplay,
                AutoEllipsis = true
            };

            int badgeLeft = padding + contentWidth + gap;
            int badgeTop = title.Top + (title.Height / 2) - (badgeHeight / 2);
            if (badgeTop < 10) badgeTop = 10;

            Label year = new Label
            {
                AutoSize = false,
                Width = badgeWidth,
                Height = badgeHeight,
                Left = badgeLeft,
                Top = badgeTop,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = accent,
                Text = paper.Year.ToString()
            };

            year.Region = System.Drawing.Region.FromHrgn(
                NativeMethods.CreateRoundRectRgn(0, 0, year.Width, year.Height, 8, 8)
            );

            // Selection visual setter (no hover visuals)
            void SetCardSelectedVisual(Panel p, bool selected)
            {
                if (p == null) return;
                if (selected)
                {
                    p.BackColor = Color.FromArgb(235, 245, 255);
                    p.BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    p.BackColor = Color.White;
                    p.BorderStyle = BorderStyle.FixedSingle;
                }
            }

            // click handler selects this card and shows the abstract
            void Card_Click(object sender, EventArgs e)
            {
                // deselect all cards first
                foreach (Control ctrl in research_FlowLayoutPanel.Controls)
                {
                    if (ctrl is Panel pnl)
                        SetCardSelectedVisual(pnl, false);
                }

                // select clicked card
                _selectedCard = card;
                SetCardSelectedVisual(card, true);

                ShowAbstract(paper);
            }

            // Add controls then wire clicks — forward child clicks to the card click handler
            card.Controls.Add(topBar);
            card.Controls.Add(title);
            card.Controls.Add(authors);
            card.Controls.Add(year);

            // Attach click handlers to the card and all children so clicks anywhere select it.
            Action<Control> attachClickRecursively = null;
            attachClickRecursively = (ctrl) =>
            {
                ctrl.Click += Card_Click;
                foreach (Control child in ctrl.Controls)
                    attachClickRecursively(child);
            };

            attachClickRecursively(card);

            research_FlowLayoutPanel.Controls.Add(card);
        }

        private void ShowAbstract(ResearchPaper paper)
        {
            if (abstract_rtbx == null)
            {
                MessageBox.Show("abstract_rtbx control not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            abstract_rtbx.Clear();

            var authorsList = paper.Authors.Split(',').Select(a => a.Trim()).ToList();
            string formattedAuthors = "👥 Authors:\n👤 " + string.Join("\n👤 ", authorsList);

            abstract_rtbx.Text =
                $"📘 {paper.Title}\n\n{formattedAuthors}\n\n📅 {paper.Year}\n\n{paper.Abstract}";
            abstract_rtbx.SelectionStart = 0;
            abstract_rtbx.ScrollToCaret();
        }

        // 🖱 Scroll up/down by one card
        private void ScrollUp_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 160; // card height + spacing
            research_FlowLayoutPanel.AutoScrollPosition = new Point(
                0,
                Math.Max(0, research_FlowLayoutPanel.VerticalScroll.Value - scrollAmount)
            );
        }

        private void ScrollDown_btn_Click(object sender, EventArgs e)
        {
            int scrollAmount = 160;
            int newValue = research_FlowLayoutPanel.VerticalScroll.Value + scrollAmount;
            int maxValue = research_FlowLayoutPanel.VerticalScroll.Maximum;

            if (newValue > maxValue)
                newValue = maxValue;

            research_FlowLayoutPanel.AutoScrollPosition = new Point(0, newValue);
        }

        private void TryEnableDoubleBuffer(Control c)
        {
            if (c == null) return;
            try
            {
                // set protected DoubleBuffered property using reflection
                var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch
            {
                // best-effort
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
            public static extern IntPtr CreateRoundRectRgn(
                int nLeftRect, int nTopRect,
                int nRightRect, int nBottomRect,
                int nWidthEllipse, int nHeightEllipse
            );
        }
    }
}