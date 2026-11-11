using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Verify ISO window: checks an image's structure and, when a source folder is given, compares
    /// paths, sizes and optionally SHA-256 hashes. Can also open showing an existing result.
    /// </summary>
    public partial class VerificationForm : Form
    {
        private readonly IsoVerificationService _service;
        private CancellationTokenSource _cancellation;
        private bool _closeWhenDone;

        public VerificationForm()
            : this(new IsoVerificationService(), null, null, false, false)
        {
        }

        public VerificationForm(IsoVerificationService service, string isoPath, string sourceFolder, bool compareHashes, bool mountWithWindows)
        {
            _service = service;
            InitializeComponent();
            Icon = AppIcon.Icon;
            isoTextBox.Text = isoPath ?? string.Empty;
            sourceTextBox.Text = sourceFolder ?? string.Empty;
            hashCheckBox.Checked = compareHashes;
            mountCheckBox.Checked = mountWithWindows;
        }

        /// <summary>Opens the window showing a result that was already produced (for example after creation).</summary>
        public VerificationForm(IsoVerificationService service, VerificationResult existing)
            : this(service, existing.IsoPath, existing.SourceFolder, existing.Level == VerificationLevel.SourceContent, false)
        {
            LastResult = existing;
        }

        /// <summary>
        /// How names are judged when a source folder is compared: Strict, Compatibility, or null to
        /// report differences only. Set before the window is shown.
        /// </summary>
        internal FilenameMode? NameRule { get; set; } = FilenameMode.Strict;

        /// <summary>The most recent completed verification, or null if none completed.</summary>
        public VerificationResult LastResult { get; private set; }

        /// <summary>True while a verification is running.</summary>
        internal bool IsRunning => _cancellation != null;

        private void VerificationForm_Load(object sender, EventArgs e)
        {
            namesComboBox.SelectedIndex = NameRule == FilenameMode.Strict ? 0 : NameRule == FilenameMode.Compatibility ? 1 : 2;
            ThemeManager.Apply(this);
            if (LastResult != null)
            {
                ShowResult(LastResult);
            }
            else
            {
                ClearResult("Choose an ISO file and select Verify.");
            }

            UpdateState();
        }

        private void IsoBrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an ISO image to verify";
                dialog.Filter = "ISO image files (*.iso)|*.iso|All files (*.*)|*.*";
                string folder = SafeDirectory(isoTextBox.Text);
                if (folder != null)
                {
                    dialog.InitialDirectory = folder;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    isoTextBox.Text = dialog.FileName;
                }
            }
        }

        private void SourceBrowseButton_Click(object sender, EventArgs e)
        {
            string chosen = FolderPicker.Show(this, "Select the folder the ISO image was created from", sourceTextBox.Text.Trim());
            if (chosen != null)
            {
                sourceTextBox.Text = chosen;
            }
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            UpdateState();
        }

        private async void VerifyButton_Click(object sender, EventArgs e)
        {
            await RunVerificationAsync();
        }

        /// <summary>Runs a verification with the current inputs. Returns the result, or null if cancelled or impossible.</summary>
        internal async Task<VerificationResult> RunVerificationAsync()
        {
            if (IsRunning)
            {
                return null;
            }

            string iso = isoTextBox.Text.Trim();
            string source = sourceTextBox.Text.Trim();
            if (!File.Exists(iso))
            {
                ClearResult("The ISO file does not exist. Choose an existing .iso file.");
                summaryLabel.ForeColor = ThemeManager.Current.ErrorText;
                return null;
            }

            if (source.Length > 0 && !Directory.Exists(source))
            {
                ClearResult("The source folder does not exist. Clear it to verify the structure only.");
                summaryLabel.ForeColor = ThemeManager.Current.ErrorText;
                return null;
            }

            var options = new VerificationOptions
            {
                IsoPath = Path.GetFullPath(iso),
                SourceFolder = source.Length > 0 ? Path.GetFullPath(source) : null,
                CompareHashes = hashCheckBox.Checked && source.Length > 0,
                MountWithWindows = mountCheckBox.Checked,
                FilenameMode = namesComboBox.SelectedIndex == 0 ? FilenameMode.Strict
                    : namesComboBox.SelectedIndex == 1 ? FilenameMode.Compatibility
                    : (FilenameMode?)null
            };

            _cancellation = new CancellationTokenSource();
            UpdateState();
            ClearResult("Verifying...");
            var progress = new Progress<IsoProgressInfo>(ShowProgress);
            try
            {
                LastResult = await _service.VerifyAsync(options, progress, _cancellation.Token);
                ShowResult(LastResult);
                return LastResult;
            }
            catch (OperationCanceledException)
            {
                ClearResult("Verification was cancelled. No result was recorded.");
                summaryLabel.ForeColor = ThemeManager.Current.WarningText;
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Verification failed unexpectedly", ex);
                ClearResult("Verification could not be completed: " + ex.Message);
                summaryLabel.ForeColor = ThemeManager.Current.ErrorText;
                return null;
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Continuous;
                UpdateState();
                if (_closeWhenDone)
                {
                    BeginInvoke(new Action(Close));
                }
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            RequestCancel();
        }

        internal void RequestCancel()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
                progressLabel.Text = "Cancelling...";
                cancelButton.Enabled = false;
            }
        }

        private void SaveReportButton_Click(object sender, EventArgs e)
        {
            if (LastResult == null)
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save Verification Report";
                dialog.Filter = "Text files (*.txt)|*.txt";
                dialog.FileName = Path.GetFileNameWithoutExtension(LastResult.IsoPath) + " verification.txt";
                string folder = SafeDirectory(LastResult.IsoPath);
                if (folder != null)
                {
                    dialog.InitialDirectory = folder;
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllText(dialog.FileName, LastResult.BuildReport());
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    LoggingService.Instance.Error("Verification report could not be saved", ex);
                    MessageBox.Show(this, "The report could not be saved: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void VerificationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsRunning)
            {
                // Stop safely first; the window closes itself once the verification has stopped.
                e.Cancel = true;
                _closeWhenDone = true;
                RequestCancel();
            }
        }

        private void ShowProgress(IsoProgressInfo info)
        {
            if (_cancellation == null || _cancellation.IsCancellationRequested)
            {
                return;
            }

            double? percent = info.Percent;
            if (percent.HasValue)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = Math.Max(0, Math.Min(1000, (int)(percent.Value * 10)));
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }

            string text = info.Message;
            if (percent.HasValue)
            {
                text += " " + Math.Floor(percent.Value) + "%";
            }

            if (info.EstimatedRemaining.HasValue)
            {
                text += ", about " + FileSizeFormatter.FormatDuration(info.EstimatedRemaining.Value) + " left";
            }

            if (!string.IsNullOrEmpty(info.CurrentFile))
            {
                text += " - " + info.CurrentFile;
            }

            progressLabel.Text = text;
        }

        private void ShowResult(VerificationResult result)
        {
            ThemePalette palette = ThemeManager.Current;
            summaryLabel.Text = result.Summary;
            summaryLabel.ForeColor = result.Passed ? palette.Accent : palette.ErrorText;
            progressLabel.Text = "Finished in " + FileSizeFormatter.FormatDuration(result.Elapsed) +
                                 (string.IsNullOrEmpty(result.ImageSha256) ? string.Empty : ". Image SHA-256: " + result.ImageSha256);

            checksListView.BeginUpdate();
            checksListView.Items.Clear();
            foreach (VerificationCheck check in result.Checks)
            {
                var item = new ListViewItem(new[] { check.Name, StatusText(check.Status), check.Detail });
                item.ForeColor = check.Status == VerificationCheckStatus.Failed ? palette.ErrorText
                    : check.Status == VerificationCheckStatus.Warning ? palette.WarningText
                    : check.Status == VerificationCheckStatus.Skipped ? palette.DimText
                    : palette.Text;
                checksListView.Items.Add(item);
            }

            checksListView.EndUpdate();

            differencesListView.BeginUpdate();
            differencesListView.Items.Clear();
            AddDifferences("Name differs from plan", result.NameMismatches);
            if (result.Checks.Any(c => c.Name == "File names" && c.Status != VerificationCheckStatus.Passed))
            {
                AddDifferences("Name changed", result.NameDifferences);
            }

            AddDifferences("Missing from image", result.MissingFromImage);
            AddDifferences("Not in source", result.ExtraInImage);
            AddDifferences("Different size", result.SizeMismatches);
            AddDifferences("Different content", result.ContentMismatches);
            if (result.Image != null)
            {
                AddDifferences("Structural problem", result.Image.Problems);
            }

            differencesListView.EndUpdate();
            differencesLabel.Text = "Differences (" + differencesListView.Items.Count + ")";
            UpdateState();
        }

        private void AddDifferences(string type, System.Collections.Generic.IEnumerable<string> items)
        {
            foreach (string path in items)
            {
                differencesListView.Items.Add(new ListViewItem(new[] { type, path }) { ForeColor = ThemeManager.Current.ErrorText });
            }
        }

        private void ClearResult(string message)
        {
            summaryLabel.Text = message;
            summaryLabel.ForeColor = ThemeManager.Current.Text;
            progressLabel.Text = string.Empty;
            checksListView.Items.Clear();
            differencesListView.Items.Clear();
            differencesLabel.Text = "Differences";
        }

        private void UpdateState()
        {
            bool running = IsRunning;
            bool hasSource = sourceTextBox.Text.Trim().Length > 0;
            isoTextBox.ReadOnly = running;
            sourceTextBox.ReadOnly = running;
            isoBrowseButton.Enabled = !running;
            sourceBrowseButton.Enabled = !running;
            hashCheckBox.Enabled = !running && hasSource;
            namesComboBox.Enabled = !running && hasSource;
            mountCheckBox.Enabled = !running;
            verifyButton.Enabled = !running && isoTextBox.Text.Trim().Length > 0;
            verifyButton.BackColor = verifyButton.Enabled ? ThemeManager.Current.Accent : ThemeManager.Current.DisabledAccent;
            cancelButton.Enabled = running;
            saveReportButton.Enabled = !running && LastResult != null;
            levelHintLabel.Text = hasSource
                ? (hashCheckBox.Checked ? "Full source-content verification: structure, paths, sizes and SHA-256 of every file."
                                        : "Source verification: structure, paths and sizes. Tick SHA-256 to also compare contents.")
                : "Structural verification only. Select the source folder to compare the image with the original files.";
        }

        private static string StatusText(VerificationCheckStatus status)
        {
            switch (status)
            {
                case VerificationCheckStatus.Passed:
                    return "Passed";
                case VerificationCheckStatus.Failed:
                    return "FAILED";
                case VerificationCheckStatus.Warning:
                    return "Warning";
                default:
                    return "Skipped";
            }
        }

        private static string SafeDirectory(string path)
        {
            string full;
            if (!PathValidator.TryGetFullPath(path, out full) || !Path.IsPathRooted(path.Trim()))
            {
                return null;
            }

            string folder = Path.GetDirectoryName(full);
            return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : null;
        }
    }
}
