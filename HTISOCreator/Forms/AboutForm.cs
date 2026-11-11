using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            Icon = AppIcon.Icon;
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            versionValueLabel.Text = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void NoticesLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
            try
            {
                if (File.Exists(path))
                {
                    System.Diagnostics.Process.Start(path);
                }
                else
                {
                    MessageBox.Show(this, "The third-party notices file was not found:\n\n" + path, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Could not open third-party notices", ex);
                MessageBox.Show(this, "The third-party notices file could not be opened.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
