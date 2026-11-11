using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Shown after an image has been written successfully.
    /// </summary>
    public partial class CompletionForm : Form
    {
        private readonly IsoCreationResult _result;
        private readonly VerificationResult _verification;
        private readonly bool _verificationCancelled;

        public CompletionForm()
            : this(new IsoCreationResult(), null, false)
        {
        }

        public CompletionForm(IsoCreationResult result)
            : this(result, null, false)
        {
        }

        /// <param name="verification">The verification result, or null when no verification completed.</param>
        /// <param name="verificationCancelled">True when verification was started but cancelled.</param>
        public CompletionForm(IsoCreationResult result, VerificationResult verification, bool verificationCancelled)
        {
            _result = result;
            _verification = verification;
            _verificationCancelled = verificationCancelled;
            InitializeComponent();
            Icon = AppIcon.Icon;
        }

        /// <summary>Filename conversion report for the created image; set by the main window.</summary>
        internal FilenameConversionReport ConversionReport { get; set; }

        /// <summary>True when the user ticked "Don't show this message again".</summary>
        public bool DoNotShowAgain => doNotShowCheckBox.Checked;

        private void CompletionForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            if (string.IsNullOrEmpty(_result.OutputPath))
            {
                return;
            }

            fileNameValueLabel.Text = Path.GetFileName(_result.OutputPath) + (_result.ReplacedExisting ? "  (replaced the previous file)" : string.Empty);
            locationValueLabel.Text = Path.GetDirectoryName(_result.OutputPath);
            toolTip.SetToolTip(locationValueLabel, locationValueLabel.Text);
            sizeValueLabel.Text = FileSizeFormatter.FormatWithBytes(_result.ImageSize);
            labelValueLabel.Text = string.IsNullOrEmpty(_result.VolumeLabel) ? "--" : _result.VolumeLabel;
            formatValueLabel.Text = FileSystemFormats.Name(_result.Format) + " (" + FileSystemFormats.EngineName(_result.Format) + ")";
            contentsValueLabel.Text = string.Format("{0:N0} files, {1:N0} folders ({2})",
                _result.FileCount, _result.DirectoryCount, FileSizeFormatter.Format(_result.SourceBytes));
            timeValueLabel.Text = FileSizeFormatter.FormatDuration(_result.Elapsed);

            ThemePalette palette = ThemeManager.Current;
            namesReportLinkLabel.Visible = ConversionReport != null;
            if (ConversionReport == null)
            {
                namesValueLabel.Text = "--";
            }
            else if (ConversionReport.Changes.Count == 0)
            {
                namesValueLabel.Text = "All original names preserved";
            }
            else
            {
                namesValueLabel.Text = ConversionReport.Changes.Count + " converted (Compatibility Mode)";
                namesValueLabel.ForeColor = palette.WarningText;
            }

            viewReportButton.Visible = _verification != null;
            if (_verification != null)
            {
                verificationValueLabel.Text = _verification.Passed
                    ? "Passed - " + ShortLevel(_verification.Level)
                    : "FAILED - see the report";
                verificationValueLabel.ForeColor = _verification.Passed ? palette.Text : palette.ErrorText;
                toolTip.SetToolTip(verificationValueLabel, _verification.Summary);
                if (!_verification.Passed)
                {
                    // Never describe a failed verification as a success.
                    titleLabel.Text = "ISO image created, but verification FAILED.";
                    titleLabel.ForeColor = palette.ErrorText;
                }
            }
            else if (_verificationCancelled)
            {
                verificationValueLabel.Text = "Cancelled - the image was kept but not verified";
                verificationValueLabel.ForeColor = palette.WarningText;
            }
            else
            {
                verificationValueLabel.Text = "Not performed";
            }
        }

        private static string ShortLevel(VerificationLevel level)
        {
            switch (level)
            {
                case VerificationLevel.SourceContent:
                    return "paths, sizes and SHA-256 match the source";
                case VerificationLevel.SourcePathsAndSizes:
                    return "paths and sizes match the source";
                default:
                    return "image structure only";
            }
        }

        private void ViewReportButton_Click(object sender, EventArgs e)
        {
            using (var form = new VerificationForm(new IsoVerificationService(), _verification))
            {
                form.ShowDialog(this);
            }
        }

        private void NamesReportLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ReportExport.SaveConversionReport(this, ConversionReport);
        }

        private void CopyPathButton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(_result.OutputPath ?? string.Empty);
                copyPathButton.Text = "Copied";
            }
            catch (ExternalException ex)
            {
                LoggingService.Instance.Warning("Could not copy path to the clipboard: " + ex.Message);
                MessageBox.Show(this, "The clipboard is in use by another program. Try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ShellHelper.ShowOutput(_result.OutputPath))
                {
                    MessageBox.Show(this, "The output file no longer exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Could not open output folder", ex);
                MessageBox.Show(this, "The output folder could not be opened.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
