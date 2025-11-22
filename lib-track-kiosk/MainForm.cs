using lib_track_kiosk.panel_forms;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace lib_track_kiosk
{
    public partial class MainForm : Form
    {
        public Panel PanelContainer => panelContainer;

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Replace the content of the panel with the provided user control.
        /// The previous control(s) are cleaned up (images disposed, controls removed and disposed).
        /// </summary>
        public void addUserControl(UserControl userControl)
        {
            if (userControl == null) return;

            // Dispose existing children properly to avoid GDI+ / memory leaks
            DisposeAndClearPanelChildren(panelContainer);

            userControl.Dock = DockStyle.Fill;
            panelContainer.Controls.Add(userControl);
            userControl.BringToFront();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var ucWelcome = new UC_Welcome();
            addUserControl(ucWelcome);
        }

        private void ShutDown_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Settings has not yet been implemented.");
        }

        /// <summary>
        /// Disposes images held by PictureBox controls nested inside 'container' and then disposes each child control.
        /// Uses a defensive approach: enumerates a copy of Controls to avoid modification while iterating.
        /// </summary>
        /// <param name="container">The container whose children should be disposed.</param>
        private void DisposeAndClearPanelChildren(Control container)
        {
            if (container == null) return;

            // Copy children to avoid CollectionModified exceptions while disposing
            var children = container.Controls.Cast<Control>().ToArray();

            foreach (var ctrl in children)
            {
                try
                {
                    // Dispose any images held by PictureBoxes (recursively)
                    DisposeImagesInContainer(ctrl);

                    // If the control is a UserControl / Form-like object, give it a chance to cleanup by calling Dispose.
                    // Remove from parent before disposing to avoid inconsistent state.
                    try { container.Controls.Remove(ctrl); } catch { /* ignore */ }

                    try { ctrl.Dispose(); } catch { /* ignore */ }
                }
                catch
                {
                    // Swallow per-control errors so cleanup continues for other controls
                }
            }
        }

        /// <summary>
        /// Recursively disposes images assigned to PictureBoxes inside a control's subtree.
        /// Leaves the controls themselves to be disposed by caller.
        /// </summary>
        /// <param name="root">Root control to scan for PictureBoxes</param>
        private void DisposeImagesInContainer(Control root)
        {
            if (root == null) return;

            // iterate over a copy because disposing nested controls may modify Controls collection
            var nodes = root.Controls.Cast<Control>().ToArray();

            foreach (var c in nodes)
            {
                try
                {
                    if (c is PictureBox pb)
                    {
                        var img = pb.Image;
                        pb.Image = null;
                        try { img?.Dispose(); } catch { /* ignore disposal exceptions */ }
                    }

                    // recursively process nested controls
                    if (c.HasChildren)
                        DisposeImagesInContainer(c);
                }
                catch
                {
                    // ignore per-control errors
                }
            }

            // Also if the root itself is a PictureBox (when called with that control), handle it
            if (root is PictureBox rootPb)
            {
                try
                {
                    var img = rootPb.Image;
                    rootPb.Image = null;
                    try { img?.Dispose(); } catch { }
                }
                catch { }
            }
        }

        /// <summary>
        /// Ensure we clean up panel children and any resources when the form is closing.
        /// This avoids relying only on finalizers and helps prevent GDI+ leaks from undisposed Bitmaps.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                DisposeAndClearPanelChildren(panelContainer);
            }
            catch
            {
                // swallow to allow form to close
            }

            base.OnFormClosing(e);
        }
    }
}