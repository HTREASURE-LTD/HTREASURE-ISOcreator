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
        private readonly ToolTip toolTip = new ToolTip();

        public AboutForm()
        {
            InitializeComponent();
            Icon = AppIcon.Icon;
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            versionValueLabel.Text = VersionText();
            toolTip.SetToolTip(versionValueLabel, BuildDetails());
        }

        /// <summary>"1.0.2 (build 1.0.2.0, Release)".</summary>
        internal static string VersionText()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string product = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version.ToString(3);
            string file = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? assembly.GetName().Version.ToString();
#if DEBUG
            const string configuration = "Debug";
#else
            const string configuration = "Release";
#endif
            return product + " (build " + file + ", " + configuration + ")";
        }

        /// <summary>Program, runtime and operating system details for support requests.</summary>
        internal static string BuildDetails()
        {
            return "Program: " + Assembly.GetExecutingAssembly().Location + Environment.NewLine +
                   ".NET runtime: " + Environment.Version + (Environment.Is64BitProcess ? " (64-bit)" : " (32-bit)") + Environment.NewLine +
                   "Windows: " + Environment.OSVersion.VersionString;
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

        private void LicenceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LICENSE.txt");
            try
            {
                if (File.Exists(path))
                {
                    ShellHelper.OpenDocument(path);
                }
                else
                {
                    MessageBox.Show(this, "The licence agreement file was not found:\n\n" + path, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Could not open the licence agreement", ex);
                MessageBox.Show(this, "The licence agreement could not be opened.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebsiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                ShellHelper.OpenWebPage(ShellHelper.CompanyWebsite);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Could not open " + ShellHelper.CompanyWebsite, ex);
                MessageBox.Show(this, "The website could not be opened. You can visit it at:\n\n" + ShellHelper.CompanyWebsite, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
