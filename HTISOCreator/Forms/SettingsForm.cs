using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Edits a copy of <see cref="AppSettings"/>. The caller reads <see cref="Settings"/>
    /// after the dialog returns OK and is responsible for saving it.
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly AppSettings _original;

        public SettingsForm()
            : this(new AppSettings())
        {
        }

        public SettingsForm(AppSettings settings)
        {
            _original = settings ?? new AppSettings();
            InitializeComponent();
            Icon = AppIcon.Icon;
            capacityComboBox.Items.AddRange(CapacityProfile.All.Cast<object>().ToArray());
            LoadValues(_original);
        }

        /// <summary>The edited settings. Valid after the dialog closes with OK.</summary>
        public AppSettings Settings { get; private set; }

        private IsoFileSystemFormat SelectedFormat =>
            formatComboBox.SelectedIndex >= 0 ? FileSystemFormats.All[formatComboBox.SelectedIndex] : IsoFileSystemFormat.Iso9660Joliet;

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
        }

        /// <summary>Validates the inputs; returns the first error or null. Exposed for tests.</summary>
        internal string ValidateInputs()
        {
            string folder = outputFolderTextBox.Text.Trim();
            string folderError = null;
            if (folder.Length > 0)
            {
                string full;
                if (!PathValidator.TryGetFullPath(folder, out full) || !Path.IsPathRooted(folder))
                {
                    folderError = "Enter a full folder path, for example D:\\ISO.";
                }
                else if (!Directory.Exists(full))
                {
                    folderError = "This folder does not exist.";
                }
            }

            string label = volumeTextBox.Text;
            string labelError = label.Length > 0 ? VolumeLabelValidator.Validate(label, SelectedFormat) : null;

            errorProvider.SetError(outputFolderTextBox, folderError ?? string.Empty);
            errorProvider.SetError(volumeTextBox, labelError ?? string.Empty);
            return folderError ?? labelError;
        }

        /// <summary>Builds the settings from the controls. Exposed for tests.</summary>
        internal AppSettings ReadValues()
        {
            AppSettings result = _original.Clone();
            string folder = outputFolderTextBox.Text.Trim();
            result.DefaultOutputFolder = folder.Length > 0 ? Path.GetFullPath(folder) : string.Empty;
            result.DefaultVolumeLabel = volumeTextBox.Text;
            result.DefaultFormat = SelectedFormat;
            result.RememberLastSource = rememberSourceCheckBox.Checked;
            result.RememberLastOutput = rememberOutputCheckBox.Checked;
            result.ShowCompletionDialog = completionCheckBox.Checked;
            result.Theme = (AppTheme)Math.Max(0, themeComboBox.SelectedIndex);
            result.AutoVerify = autoVerifyCheckBox.Checked;
            result.VerifyHashes = verifyHashesCheckBox.Checked;
            result.VerifyWithWindowsMount = verifyMountCheckBox.Checked;
            result.CapacityProfileId = ((CapacityProfile)capacityComboBox.SelectedItem ?? CapacityProfile.All[0]).Id;
            result.CustomCapacityMegabytes = (int)customCapacityUpDown.Value;
            return result;
        }

        private void CapacityComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEnabledState();
        }

        private void UpdateEnabledState()
        {
            bool custom = capacityComboBox.SelectedItem is CapacityProfile && ((CapacityProfile)capacityComboBox.SelectedItem).Id == CapacityProfile.CustomId;
            customCapacityUpDown.Enabled = custom;
            customCapacityUnitLabel.Enabled = custom;
            verifyHashesCheckBox.Enabled = autoVerifyCheckBox.Checked;
        }

        private void LoadValues(AppSettings settings)
        {
            outputFolderTextBox.Text = settings.DefaultOutputFolder;
            volumeTextBox.Text = settings.DefaultVolumeLabel;
            formatComboBox.Items.Clear();
            formatComboBox.Items.AddRange(FileSystemFormats.All.Select(f => (object)FileSystemFormats.Describe(f)).ToArray());
            int index = FileSystemFormats.All.ToList().IndexOf(settings.DefaultFormat);
            formatComboBox.SelectedIndex = index >= 0 ? index : 0;
            rememberSourceCheckBox.Checked = settings.RememberLastSource;
            rememberOutputCheckBox.Checked = settings.RememberLastOutput;
            completionCheckBox.Checked = settings.ShowCompletionDialog;
            themeComboBox.SelectedIndex = (int)settings.Theme;
            autoVerifyCheckBox.Checked = settings.AutoVerify;
            verifyHashesCheckBox.Checked = settings.VerifyHashes;
            verifyMountCheckBox.Checked = settings.VerifyWithWindowsMount;
            if (!Services.WindowsMountChecker.IsAvailable)
            {
                // Windows 7 can't mount ISO images; the saved preference is kept for when it can be used.
                verifyMountCheckBox.Enabled = false;
                verifyMountCheckBox.Text += " (Windows 8 or later)";
            }
            capacityComboBox.SelectedItem = CapacityProfile.Find(settings.CapacityProfileId);
            customCapacityUpDown.Value = Math.Max(customCapacityUpDown.Minimum, Math.Min(customCapacityUpDown.Maximum, settings.CustomCapacityMegabytes));
            UpdateEnabledState();
        }

        private void BrowseOutputButton_Click(object sender, EventArgs e)
        {
            string chosen = FolderPicker.Show(this, "Select the default folder for ISO files", outputFolderTextBox.Text.Trim());
            if (chosen != null)
            {
                outputFolderTextBox.Text = chosen;
            }
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            ValidateInputs();
            UpdateEnabledState();
        }

        private void RestoreDefaultsButton_Click(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(this, "Restore all settings on this page to their defaults?",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
            {
                LoadValues(new AppSettings());
                ValidateInputs();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            string error = ValidateInputs();
            if (error != null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Settings = ReadValues();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
