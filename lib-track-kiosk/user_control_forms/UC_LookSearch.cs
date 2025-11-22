using lib_track_kiosk.panel_forms;
using lib_track_kiosk.sub_user_controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_LookSearch : UserControl
    {
        // Keep reference to current control for cleanup
        private UserControl _currentControl;

        // Keep specific references for reuse
        private UC_LookBooks _lookBooksControl;
        private UC_LookResearchPapers _lookResearchControl;

        public UC_LookSearch()
        {
            InitializeComponent();

            // Wire up disposal event
            this.Disposed += UC_LookSearch_Disposed;
        }

        private void UC_LookSearch_Disposed(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("🗑️ UC_LookSearch disposing...");

                // Cleanup all cached controls
                CleanupCurrentControl();

                if (_lookBooksControl != null)
                {
                    try
                    {
                        _lookBooksControl.CleanupMemory();
                        _lookBooksControl.Dispose();
                        _lookBooksControl = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error disposing _lookBooksControl: {ex.Message}");
                    }
                }

                if (_lookResearchControl != null)
                {
                    try
                    {
                        // If UC_LookResearchPapers also has CleanupMemory, call it
                        // _lookResearchControl.CleanupMemory();
                        _lookResearchControl.Dispose();
                        _lookResearchControl = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error disposing _lookResearchControl: {ex.Message}");
                    }
                }

                _currentControl = null;

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.WriteLine("✓ UC_LookSearch disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_LookSearch_Disposed error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup the currently active control before switching
        /// </summary>
        private void CleanupCurrentControl()
        {
            try
            {
                if (_currentControl != null)
                {
                    Console.WriteLine($"🧹 Cleaning up current control: {_currentControl.GetType().Name}");

                    // Special cleanup for UC_LookBooks
                    if (_currentControl is UC_LookBooks lookBooks)
                    {
                        try
                        {
                            lookBooks.CleanupMemory();
                            Console.WriteLine("✓ UC_LookBooks memory cleaned");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Error cleaning UC_LookBooks: {ex.Message}");
                        }
                    }

                    // Special cleanup for UC_LookResearchPapers if it has cleanup method
                    // if (_currentControl is UC_LookResearchPapers lookResearch)
                    // {
                    //     try
                    //     {
                    //         lookResearch.CleanupMemory();
                    //         Console.WriteLine("✓ UC_LookResearchPapers memory cleaned");
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         Console.WriteLine($"⚠️ Error cleaning UC_LookResearchPapers: {ex.Message}");
                    //     }
                    // }

                    // Hide the control
                    _currentControl.Visible = false;

                    _currentControl = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ CleanupCurrentControl error: {ex.Message}");
            }
        }

        private async void AddSubUserControl(UserControl uc)
        {
            try
            {
                // Cleanup previous control BEFORE switching
                CleanupCurrentControl();

                // Small delay to ensure cleanup completes
                await Task.Delay(50);

                // Clear the panel
                lookSearchMain_panel.Controls.Clear();

                // Add new control
                uc.Dock = DockStyle.Fill;
                lookSearchMain_panel.Controls.Add(uc);
                uc.BringToFront();

                // Animate visibility
                uc.Visible = false;
                await Task.Delay(100);
                uc.Visible = true;

                // Track current control
                _currentControl = uc;

                Console.WriteLine($"✓ Switched to: {uc.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AddSubUserControl error: {ex.Message}");
            }
        }

        private void exitLook_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("🚪 Exiting Look & Search...");

                // Cleanup before leaving
                CleanupCurrentControl();

                MainForm mainForm = (MainForm)this.ParentForm;
                if (mainForm != null)
                {
                    UC_Welcome welcomeScreen = new UC_Welcome();
                    mainForm.addUserControl(welcomeScreen);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ exitLook_btn_Click error: {ex.Message}");
            }
        }

        private void books_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📚 Navigating to Books...");

                // Reuse existing control if available, otherwise create new
                if (_lookBooksControl == null || _lookBooksControl.IsDisposed)
                {
                    Console.WriteLine("Creating new UC_LookBooks instance...");
                    _lookBooksControl = new UC_LookBooks();
                }
                else
                {
                    Console.WriteLine("Reusing existing UC_LookBooks instance...");

                    // Reload data when returning to this control
                    try
                    {
                        _lookBooksControl.ReloadData();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error reloading data: {ex.Message}");
                    }
                }

                AddSubUserControl(_lookBooksControl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ books_btn_Click error: {ex.Message}");
                MessageBox.Show($"Error loading Books page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void research_papers_btn_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("📄 Navigating to Research Papers...");

                // Reuse existing control if available, otherwise create new
                if (_lookResearchControl == null || _lookResearchControl.IsDisposed)
                {
                    Console.WriteLine("Creating new UC_LookResearchPapers instance...");
                    _lookResearchControl = new UC_LookResearchPapers();
                }
                else
                {
                    Console.WriteLine("Reusing existing UC_LookResearchPapers instance...");

                    // If UC_LookResearchPapers has a ReloadData method, call it
                    // try
                    // {
                    //     _lookResearchControl.ReloadData();
                    // }
                    // catch (Exception ex)
                    // {
                    //     Console.WriteLine($"⚠️ Error reloading data: {ex.Message}");
                    // }
                }

                AddSubUserControl(_lookResearchControl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ research_papers_btn_Click error: {ex.Message}");
                MessageBox.Show($"Error loading Research Papers page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UC_LookSearch_Load(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("🔄 UC_LookSearch loading...");

                // Load default view (Books)
                if (_lookBooksControl == null || _lookBooksControl.IsDisposed)
                {
                    _lookBooksControl = new UC_LookBooks();
                }

                AddSubUserControl(_lookBooksControl);

                Console.WriteLine("✓ UC_LookSearch loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UC_LookSearch_Load error: {ex.Message}");
            }
        }
    }
}