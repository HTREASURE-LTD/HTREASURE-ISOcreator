using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    public partial class MainForm : Form
    {
        private const string AppTitle = "HT ISO Creator";
        private const int ProgressScale = 1000;

        private readonly IsoCreationService _isoService;
        private readonly FolderAnalysisService _analysisService = new FolderAnalysisService();
        private readonly IsoVerificationService _verifier = new IsoVerificationService();
        private readonly SettingsService _settingsService;
        private readonly HistoryService _history;
        private readonly LoggingService _log = LoggingService.Instance;
        private readonly Stopwatch _operationStopwatch = new Stopwatch();

        private AppSettings _settings;
        private TaskbarProgress _taskbar;
        private CancellationTokenSource _cancellation;
        private CancellationTokenSource _scanCancellation;
        private bool _busy;
        private bool _scanning;
        private bool _closeRequested;
        private string _scannedPath;
        private FolderAnalysisResult _analysis;
        private long? _isoSize;
        private bool _isoSizeCalculating;
        private int _sizeCalculationId;
        private string _suggestedLabel;
        private string _suggestedOutput;
        private string _overwriteConfirmedPath;
        private string _lastCreatedIso;
        private string _statusText = "Ready";
        private bool _verifying;

        public MainForm()
            : this(new SettingsService(), new HistoryService(), new IsoCreationService())
        {
        }

        /// <summary>
        /// Used by tests: history and recovery files live next to the given settings file, so tests
        /// never touch the user's real history.
        /// </summary>
        internal MainForm(SettingsService settingsService)
            : this(settingsService,
                   new HistoryService(Path.Combine(Path.GetDirectoryName(settingsService.SettingsFile), "history.xml"), LoggingService.Instance),
                   new IsoCreationService(LoggingService.Instance, FileSystemOperations.Instance,
                       new PendingOperationsRegistry(Path.Combine(Path.GetDirectoryName(settingsService.SettingsFile), "pending-operations.txt"), LoggingService.Instance)))
        {
        }

        internal MainForm(SettingsService settingsService, HistoryService history, IsoCreationService isoService)
        {
            _settingsService = settingsService;
            _history = history;
            _isoService = isoService;
            _settings = settingsService.Load();
            InitializeComponent();
            Icon = AppIcon.Icon;
            using (var large = new Icon(AppIcon.Icon, 48, 48))
            {
                headerIcon.Image = large.ToBitmap();
            }
        }

        internal bool IsBusy => _busy;

        internal FolderAnalysisResult CurrentAnalysis => _analysis;

        internal long? CalculatedIsoSize => _isoSize;

        internal AppSettings CurrentSettings => _settings;

        internal HistoryService History => _history;

        private CapacityProfile SelectedCapacityProfile => capacityComboBox.SelectedItem as CapacityProfile ?? CapacityProfile.All[0];

        private long? SelectedCapacityBytes => SelectedCapacityProfile.GetCapacityBytes((int)customCapacityUpDown.Value);

        private IsoFileSystemFormat CurrentFormat =>
            fileSystemComboBox.SelectedIndex >= 0 ? FileSystemFormats.All[fileSystemComboBox.SelectedIndex] : IsoFileSystemFormat.Iso9660Joliet;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Oemcomma) && !_busy)
            {
                SettingsMenuItem_Click(this, EventArgs.Empty);
                return true;
            }

            if (keyData == Keys.Escape && _busy)
            {
                RequestCancel(true);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            progressBar.Maximum = ProgressScale;
            brandStripLabel.Text = BrandFooterText();
            fileSystemComboBox.Items.AddRange(FileSystemFormats.All.Select(f => (object)FileSystemFormats.Describe(f)).ToArray());
            SelectFormat(_settings.DefaultFormat);
            capacityComboBox.Items.AddRange(CapacityProfile.All.Cast<object>().ToArray());
            ApplyWorkflowSettings();
            RestoreWindowPlacement();
            FitToScreen();
            ApplyTheme();
            ResetProgressDisplay("Ready. Select or drag a source folder to begin.");

            if (_settings.RememberLastSource && Directory.Exists(_settings.LastSourceFolder))
            {
                sourceTextBox.Text = _settings.LastSourceFolder;
            }

            UpdateValidation();
            UpdateAnalysisDisplay();
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            // Taskbar progress is shown on the window's taskbar button, so it only applies when there is one.
            if (ShowInTaskbar)
            {
                _taskbar = new TaskbarProgress(Handle);
            }

            if (sourceTextBox.Text.Length > 0)
            {
                await ScanSourceAsync(false);
            }
        }

        /// <summary>Shows the saved capacity profile and verification options in the main window.</summary>
        private void ApplyWorkflowSettings()
        {
            // Read the saved values first: setting the controls raises change events that write back to _settings.
            string profileId = _settings.CapacityProfileId;
            int customMegabytes = _settings.CustomCapacityMegabytes;
            bool autoVerify = _settings.AutoVerify;
            bool verifyHashes = _settings.VerifyHashes;
            FilenameMode filenameMode = _settings.FilenameMode;
            strictModeRadioButton.Checked = filenameMode == FilenameMode.Strict;
            compatibilityModeRadioButton.Checked = filenameMode == FilenameMode.Compatibility;
            _settings.FilenameMode = filenameMode;
            capacityComboBox.SelectedItem = CapacityProfile.Find(profileId);
            customCapacityUpDown.Value = Math.Max(customCapacityUpDown.Minimum, Math.Min(customCapacityUpDown.Maximum, customMegabytes));
            hashCheckBox.Checked = verifyHashes;
            verifyCheckBox.Checked = autoVerify;
            _settings.CapacityProfileId = profileId;
            _settings.CustomCapacityMegabytes = customMegabytes;
            _settings.AutoVerify = autoVerify;
            _settings.VerifyHashes = verifyHashes;
            Capacity_Changed(this, EventArgs.Empty);
            VerifyOptions_Changed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Puts the window back where it was last closed, if that place is still on a screen (monitors
        /// may have been removed or rearranged since). The window has a fixed size, so only the position
        /// is restored; a saved size or maximised state from an earlier version is ignored.
        /// </summary>
        private void RestoreWindowPlacement()
        {
            if (_settings.WindowWidth <= 0 || _settings.WindowHeight <= 0)
            {
                return;
            }

            var saved = new Rectangle(_settings.WindowLeft, _settings.WindowTop, Width, Height);
            bool visible = Screen.AllScreens.Any(screen =>
            {
                Rectangle overlap = Rectangle.Intersect(screen.WorkingArea, saved);
                return overlap.Width >= 200 && overlap.Height >= 100;
            });
            if (!visible)
            {
                _log.Info("Saved window position is off-screen; using the default position.");
                return;
            }

            StartPosition = FormStartPosition.Manual;
            Location = saved.Location;
        }

        /// <summary>Records the window position (and its fixed size, which is not restored).</summary>
        private void SaveWindowPlacement()
        {
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            _settings.WindowLeft = bounds.Left;
            _settings.WindowTop = bounds.Top;
            _settings.WindowWidth = bounds.Width;
            _settings.WindowHeight = bounds.Height;
            _settings.WindowMaximized = false;
        }

        /// <summary>
        /// At high display scaling on a small screen the fixed-size window can be larger than the screen.
        /// It is then made to fit, and the window scrolls so that every control can still be reached.
        /// </summary>
        private void FitToScreen()
        {
            FitToScreen(Screen.FromControl(this).WorkingArea);
        }

        internal void FitToScreen(Rectangle area)
        {
            if (Height > area.Height || Width > area.Width)
            {
                AutoScrollMinSize = ClientSize;
                AutoScroll = true;
                Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
                Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            }
        }

        private void Capacity_Changed(object sender, EventArgs e)
        {
            _settings.CapacityProfileId = SelectedCapacityProfile.Id;
            _settings.CustomCapacityMegabytes = (int)customCapacityUpDown.Value;
            bool custom = SelectedCapacityProfile.Id == CapacityProfile.CustomId;
            customCapacityUpDown.Visible = custom;
            customCapacityUnitLabel.Visible = custom;
            UpdateAnalysisDisplay();
        }

        private void VerifyOptions_Changed(object sender, EventArgs e)
        {
            _settings.AutoVerify = verifyCheckBox.Checked;
            _settings.VerifyHashes = hashCheckBox.Checked;
            hashCheckBox.Enabled = verifyCheckBox.Checked && !_busy;
        }

        /// <summary>Tests only: behaves as if the user already agreed to replace <paramref name="path"/> (no message box).</summary>
        internal void ConfirmOverwriteForTests(string path)
        {
            _overwriteConfirmedPath = Path.GetFullPath(path);
        }

        /// <summary>Strict Mode unless Compatibility Mode is selected.</summary>
        internal FilenameMode SelectedFilenameMode =>
            compatibilityModeRadioButton.Checked ? FilenameMode.Compatibility : FilenameMode.Strict;

        private void FilenameMode_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
            {
                return;
            }

            if (_settings.FilenameMode != SelectedFilenameMode)
            {
                _settings.FilenameMode = SelectedFilenameMode;
                _log.Info("Filename mode changed to " + SelectedFilenameMode);
                if (!_settingsService.Save(_settings))
                {
                    _log.Warning("The filename mode could not be saved to the settings file.");
                }
            }

            UpdateFilenameStatus();
        }

        private void ExportNamesButton_Click(object sender, EventArgs e)
        {
            if (_analysis == null)
            {
                MessageBox.Show(this, "Select a source folder first. The report lists every name that would change in the image.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ReportExport.SaveConversionReport(this, FilenameConversionReport.FromAnalysis(_analysis,
                PathValidator.EnsureIsoExtension(outputTextBox.Text.Trim()), CurrentFormat, SelectedFilenameMode));
        }

        /// <summary>Number of names that would change for the current source and file system (cached per scan and format).</summary>
        private int CountNameChanges()
        {
            if (_analysis == null)
            {
                return 0;
            }

            if (!ReferenceEquals(_nameChangesAnalysis, _analysis) || _nameChangesFormat != CurrentFormat)
            {
                _nameChanges = IsoNameMapper.Map(_analysis.Directories, _analysis.Files.Select(f => f.RelativePath), CurrentFormat).Changes;
                _nameChangesAnalysis = _analysis;
                _nameChangesFormat = CurrentFormat;
            }

            return _nameChanges.Count;
        }

        private FolderAnalysisResult _nameChangesAnalysis;
        private IsoFileSystemFormat _nameChangesFormat;
        private System.Collections.Generic.List<IsoNameChange> _nameChanges = new System.Collections.Generic.List<IsoNameChange>();

        /// <summary>Filename preservation status line of the Filename Compatibility section.</summary>
        private void UpdateFilenameStatus()
        {
            ThemePalette palette = ThemeManager.Current;
            if (_analysis == null)
            {
                filenameStatusLabel.Text = _scanning ? "Checking file names..." : "Select a source folder to check its file names.";
                filenameStatusLabel.ForeColor = palette.DimText;
                exportNamesButton.Enabled = false;
                return;
            }

            int changes = CountNameChanges();
            int total = _analysis.FileCount + _analysis.DirectoryCount;
            int programFiles = _nameChanges.Count(c => !c.IsDirectory && CompatibilityService.IsProgramFile(c.SourcePath));
            if (changes == 0)
            {
                filenameStatusLabel.Text = "Every file and folder name (" + total.ToString("N0") + ") will be preserved exactly.";
                filenameStatusLabel.ForeColor = palette.Text;
            }
            else if (SelectedFilenameMode == FilenameMode.Strict)
            {
                filenameStatusLabel.Text = changes + " name(s) cannot be preserved" + (programFiles > 0 ? " (" + programFiles + " program file(s))" : string.Empty) +
                                           ". Creation is blocked.";
                filenameStatusLabel.ForeColor = palette.ErrorText;
            }
            else
            {
                filenameStatusLabel.Text = changes + " name(s) will be converted" + (programFiles > 0 ? " (" + programFiles + " program file(s))" : string.Empty) +
                                           ". Confirmation required.";
                filenameStatusLabel.ForeColor = palette.WarningText;
            }

            toolTip.SetToolTip(filenameStatusLabel, filenameStatusLabel.Text);
            exportNamesButton.Enabled = !_busy;
        }

        private void VerifyIsoMenuItem_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            string iso = GuessExistingIso();
            string source = iso == null ? null : SourceFolderFor(iso);
            using (var form = new VerificationForm(_verifier, iso, source, _settings.VerifyHashes && source != null, _settings.VerifyWithWindowsMount)
            {
                NameRule = _history.Load().Where(h => h.Outcome == CreationOutcome.Succeeded && string.Equals(h.OutputPath, iso, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(h => h.Timestamp)
                    .Select(h => h.FilenameMode).FirstOrDefault() ?? SelectedFilenameMode
            })
            {
                form.ShowDialog(this);
                if (form.LastResult != null)
                {
                    _history.RecordVerification(form.LastResult.IsoPath, form.LastResult);
                }
            }
        }

        private void IsoInfoMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new IsoInfoForm(GuessExistingIso()))
            {
                form.ShowDialog(this);
            }
        }

        private void HistoryMenuItem_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            using (var form = new HistoryForm(_history, _verifier, _settings.VerifyHashes, _settings.VerifyWithWindowsMount))
            {
                form.ShowDialog(this);
            }
        }

        private void ViewLogMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(_log.CurrentLogFile))
                {
                    Process.Start(_log.CurrentLogFile);
                }
                else
                {
                    MessageBox.Show(this, "Nothing has been logged today.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Could not open the log file", ex);
                MessageBox.Show(this, "The log file could not be opened:\n\n" + _log.CurrentLogFile, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CompatibilityMenuItem_Click(object sender, EventArgs e)
        {
            await CheckCompatibilityAsync();
        }

        private async void CompatibilityLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            await CheckCompatibilityAsync();
        }

        /// <summary>
        /// Analyses the source exactly as Create would, without writing anything, and shows the
        /// compatibility findings and name conversion preview.
        /// </summary>
        internal async Task<CompatibilityReport> CheckCompatibilityAsync(bool showWindow = true)
        {
            if (_busy)
            {
                return null;
            }

            string sourceError = PathValidator.ValidateSourceFolder(sourceTextBox.Text.Trim());
            if (sourceError != null)
            {
                if (showWindow)
                {
                    MessageBox.Show(this, sourceError, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return null;
            }

            var options = new IsoCreationOptions
            {
                SourceFolder = Path.GetFullPath(sourceTextBox.Text.Trim()),
                OutputPath = PathValidator.EnsureIsoExtension(outputTextBox.Text.Trim()),
                VolumeLabel = volumeTextBox.Text,
                Format = CurrentFormat,
                FilenameMode = SelectedFilenameMode
            };

            _cancellation = new CancellationTokenSource();
            CompatibilityReport report = null;
            FilenameConversionReport conversionReport = null;
            SetBusy(true);
            try
            {
                IsoBuildPlan plan = await _isoService.PrepareAsync(options, new Progress<IsoProgressInfo>(ShowProgress), _cancellation.Token);
                report = CompatibilityService.Build(plan, CapacityEvaluation.Evaluate(plan.ImageSize, SelectedCapacityBytes, SelectedCapacityProfile.Name));
                conversionReport = FilenameConversionReport.FromPlan(plan, false);
                ResetProgressDisplay("Compatibility check complete. Nothing was written.");
            }
            catch (OperationCanceledException)
            {
                ResetProgressDisplay("Compatibility check cancelled.");
            }
            catch (IsoCreationException ex)
            {
                ResetProgressDisplay("The compatibility check could not be completed.");
                if (showWindow)
                {
                    MessageBox.Show(this, ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
                SetBusy(false);
                _taskbar?.SetState(TaskbarProgressState.None);
            }

            if (report != null && showWindow)
            {
                using (var form = new CompatibilityForm(report, false, conversionReport))
                {
                    form.ShowDialog(this);
                }
            }

            return report;
        }

        /// <summary>The ISO most likely meant by the Tools commands: the last one created, or the output path if it exists.</summary>
        private string GuessExistingIso()
        {
            if (!string.IsNullOrEmpty(_lastCreatedIso) && File.Exists(_lastCreatedIso))
            {
                return _lastCreatedIso;
            }

            string output = outputTextBox.Text.Trim();
            string full;
            return PathValidator.TryGetFullPath(output, out full) && Path.IsPathRooted(output) && File.Exists(full) ? full : null;
        }

        /// <summary>The source folder recorded in history for an ISO, if it still exists.</summary>
        private string SourceFolderFor(string isoPath)
        {
            HistoryEntry entry = _history.Load().FirstOrDefault(h => h.Outcome == CreationOutcome.Succeeded &&
                                                                     string.Equals(h.OutputPath, isoPath, StringComparison.OrdinalIgnoreCase));
            return entry != null && Directory.Exists(entry.SourceFolder) ? entry.SourceFolder : null;
        }

        private async void BrowseButton_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            string initial = Directory.Exists(sourceTextBox.Text.Trim()) ? sourceTextBox.Text.Trim() : _settings.LastSourceFolder;
            string chosen = FolderPicker.Show(this, "Select the folder to put in the ISO image", initial);
            if (chosen == null)
            {
                return;
            }

            sourceTextBox.Text = chosen;
            await ScanSourceAsync(false);
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            await ScanSourceAsync(true);
        }

        private void SourceTextBox_TextChanged(object sender, EventArgs e)
        {
            string text = sourceTextBox.Text.Trim();
            if (_scannedPath != null && !string.Equals(text, _scannedPath, StringComparison.OrdinalIgnoreCase))
            {
                // The analysis on screen no longer describes the folder in the box.
                _scannedPath = null;
                _analysis = null;
                _isoSize = null;
                SetSourceSummary("Press Tab or Refresh to scan this folder.", ThemeManager.Current.DimText, false);
                UpdateAnalysisDisplay();
            }

            UpdateValidation();
        }

        private async void SourceTextBox_Leave(object sender, EventArgs e)
        {
            await ScanSourceAsync(false);
        }

        private void VolumeTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateValidation();
        }

        /// <summary>Selects <paramref name="format"/> in the file system list (ISO 9660 + Joliet if unknown).</summary>
        private void SelectFormat(IsoFileSystemFormat format)
        {
            int index = FileSystemFormats.All.ToList().IndexOf(format);
            fileSystemComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private async void FileSystemComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_scannedPath != null && volumeTextBox.Text == _suggestedLabel)
            {
                _suggestedLabel = SuggestLabel(_scannedPath);
                volumeTextBox.Text = _suggestedLabel;
            }

            UpdateValidation();
            await RecalculateIsoSizeAsync();
        }

        private void OutputTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateValidation();
            UpdateAnalysisDisplay();
        }

        private void OutputTextBox_Leave(object sender, EventArgs e)
        {
            string text = outputTextBox.Text.Trim();
            if (text.Length > 0)
            {
                string withExtension = PathValidator.EnsureIsoExtension(text);
                if (withExtension != outputTextBox.Text)
                {
                    outputTextBox.Text = withExtension;
                }
            }
        }

        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save ISO Image As";
                dialog.Filter = "ISO image files (*.iso)|*.iso";
                dialog.DefaultExt = "iso";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;

                string current = outputTextBox.Text.Trim();
                string currentFull;
                if (PathValidator.TryGetFullPath(current, out currentFull) && Path.IsPathRooted(current))
                {
                    string folder = Path.GetDirectoryName(currentFull);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    {
                        dialog.InitialDirectory = folder;
                    }

                    dialog.FileName = Path.GetFileName(currentFull);
                }
                else
                {
                    string folder = SuggestOutputFolder(null);
                    if (Directory.Exists(folder))
                    {
                        dialog.InitialDirectory = folder;
                    }

                    if (volumeTextBox.Text.Length > 0)
                    {
                        dialog.FileName = volumeTextBox.Text + ".iso";
                    }
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string chosen = PathValidator.EnsureIsoExtension(dialog.FileName);
                outputTextBox.Text = chosen;
                _suggestedOutput = null;

                // The dialog has already asked about replacing an existing file.
                _overwriteConfirmedPath = File.Exists(chosen) ? Path.GetFullPath(chosen) : null;
                RememberOutputFolder(chosen);
                _log.Info("Output destination selected: " + chosen);
            }
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            OutputTextBox_Leave(sender, e);
            await ScanSourceAsync(false);
            string firstError = UpdateValidation();
            if (firstError != null)
            {
                MessageBox.Show(this, firstError, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sourcePath = Path.GetFullPath(sourceTextBox.Text.Trim());
            string outputPath = Path.GetFullPath(outputTextBox.Text.Trim());
            bool outputExists = File.Exists(outputPath);
            if (outputExists && !string.Equals(_overwriteConfirmedPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                DialogResult answer = MessageBox.Show(this,
                    "The file already exists:\n\n" + outputPath + "\n\nDo you want to replace it?\n\nThe existing file is kept until the new image has been completed successfully.",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            var options = new IsoCreationOptions
            {
                SourceFolder = sourcePath,
                OutputPath = outputPath,
                VolumeLabel = volumeTextBox.Text,
                Format = CurrentFormat,
                FilenameMode = SelectedFilenameMode,
                OverwriteExisting = outputExists
            };

            _cancellation = new CancellationTokenSource();
            CancellationToken token = _cancellation.Token;
            var progress = new Progress<IsoProgressInfo>(ShowProgress);
            var stopwatch = Stopwatch.StartNew();
            var history = new HistoryEntry
            {
                SourceFolder = sourcePath,
                OutputPath = outputPath,
                VolumeLabel = options.VolumeLabel,
                Format = options.Format,
                LogReference = options.OperationId,
                FilenameMode = options.FilenameMode
            };
            bool recordHistory = true;
            IsoCreationResult created = null;
            IsoBuildPlan createdPlan = null;
            VerificationResult verification = null;
            bool verificationCancelled = false;
            SetBusy(true);

            try
            {
                IsoBuildPlan plan = await _isoService.PrepareAsync(options, progress, token);
                history.FileCount = plan.Files.Count;
                history.DirectoryCount = plan.Directories.Count;

                // Problems, warnings and every name conversion are shown before anything is written.
                CompatibilityReport report = CompatibilityService.Build(plan,
                    CapacityEvaluation.Evaluate(plan.ImageSize, SelectedCapacityBytes, SelectedCapacityProfile.Name));
                if (!plan.CanCreate || report.NeedsReview)
                {
                    DialogResult review;
                    bool confirmed;
                    using (var form = new CompatibilityForm(report, plan.CanCreate, FilenameConversionReport.FromPlan(plan, false)))
                    {
                        review = form.ShowDialog(this);
                        confirmed = form.ConversionsConfirmed;
                    }

                    if (!plan.CanCreate)
                    {
                        // Nothing was written, so an existing output file is untouched.
                        history.Outcome = CreationOutcome.Failed;
                        history.Message = (plan.StrictModeError ?? plan.Errors[0]) +
                                          (plan.Errors.Count > 1 ? " (and " + (plan.Errors.Count - 1) + " more)" : string.Empty);
                        ResetProgressDisplay(plan.StrictModeError != null
                            ? "Strict Mode: the ISO image was not created because " + plan.RenamedItems.Count + " name(s) cannot be preserved."
                            : "The ISO image was not created. Resolve the problems listed and try again.");
                        if (plan.StrictModeError != null)
                        {
                            statusLabel.ForeColor = ThemeManager.Current.ErrorText;
                        }

                        return;
                    }

                    if (review != DialogResult.OK)
                    {
                        recordHistory = false;
                        ResetProgressDisplay("ISO creation was not started.");
                        return;
                    }

                    // Compatibility Mode: the conversions shown were explicitly confirmed in the window.
                    options.ConversionsApproved = plan.RenamedItems.Count > 0 && confirmed;
                }

                foreach (IsoNameChange change in plan.RenamedItems)
                {
                    _log.Info("[" + options.OperationId + "] Renamed in image: '" + change.SourcePath + "' -> '" + change.ImagePath + "' (" + change.Reason + ")");
                }

                token.ThrowIfCancellationRequested();
                IsoCreationResult result = await _isoService.CreateAsync(plan, progress, token);
                createdPlan = plan;

                _lastCreatedIso = result.OutputPath;
                _overwriteConfirmedPath = null;
                RememberOutputFolder(result.OutputPath);
                SetProgressComplete(result);
                created = result;
                history.Outcome = CreationOutcome.Succeeded;
                history.ImageSize = result.ImageSize;
                history.DurationSeconds = result.Elapsed.TotalSeconds;

                if (verifyCheckBox.Checked)
                {
                    _verifying = true;
                    try
                    {
                        verification = await _verifier.VerifyAsync(new VerificationOptions
                        {
                            IsoPath = result.OutputPath,
                            CompareHashes = hashCheckBox.Checked,
                            MountWithWindows = _settings.VerifyWithWindowsMount,
                            ExpectedLabel = options.VolumeLabel,
                            ExpectedFormat = options.Format
                        }, plan, progress, token);
                        history.ApplyVerification(verification);
                        ShowVerificationOutcome(verification);
                    }
                    catch (OperationCanceledException)
                    {
                        // Only the verification was cancelled; the finished image is kept.
                        verificationCancelled = true;
                        history.Verification = HistoryVerificationStatus.Cancelled;
                        history.VerificationDetail = "Verification was cancelled by the user.";
                        SetProgressComplete(result);
                        statusLabel.ForeColor = ThemeManager.Current.WarningText;
                        statusLabel.Text = "ISO image created. Verification was cancelled, so the image has not been verified.";
                    }
                    finally
                    {
                        _verifying = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                history.Outcome = CreationOutcome.Cancelled;
                history.Message = "Cancelled by the user.";
                ResetProgressDisplay("Cancelled. No image was saved and the incomplete file was removed.");
                MessageBox.Show(this,
                    "ISO creation was cancelled.\n\nNo image was saved, and the incomplete output file was removed." +
                    (outputExists ? "\nThe existing file was not changed." : string.Empty),
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IsoCreationException ex)
            {
                history.Outcome = CreationOutcome.Failed;
                history.Message = ex.Message;
                _log.Warning("[" + options.OperationId + "] ISO image not created: " + ex.Message);
                ResetProgressDisplay("Failed. The ISO image was not created.");
                _taskbar?.SetState(TaskbarProgressState.Error);
                MessageBox.Show(this, ex.Message + "\n\nThe ISO image was not created.\n\nReference: " + options.OperationId + " (in the log file)",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                history.Outcome = CreationOutcome.Failed;
                history.Message = IsoCreationService.ToUserMessage(ex);
                _log.Error("[" + options.OperationId + "] Unexpected error during ISO creation", ex);
                ResetProgressDisplay("Failed. The ISO image was not created.");
                _taskbar?.SetState(TaskbarProgressState.Error);
                MessageBox.Show(this, IsoCreationService.ToUserMessage(ex) + "\n\nThe ISO image was not created.\n\nReference: " + options.OperationId +
                                      "\nLog file: " + _log.CurrentLogFile,
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (recordHistory)
                {
                    if (history.DurationSeconds <= 0)
                    {
                        history.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
                    }

                    if (!_history.Add(history))
                    {
                        _log.Warning("The creation could not be recorded in the history file.");
                    }
                }

                _cancellation.Dispose();
                _cancellation = null;
                SetBusy(false);
                _taskbar?.SetState(TaskbarProgressState.None);
                if (_closeRequested)
                {
                    BeginInvoke(new Action(Close));
                }
            }

            if (created != null && !_closeRequested)
            {
                ShowCompletion(created, verification, verificationCancelled, FilenameConversionReport.FromPlan(createdPlan, true));
            }
        }

        private void ShowVerificationOutcome(VerificationResult verification)
        {
            ThemePalette palette = ThemeManager.Current;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = ProgressScale;
            percentLabel.Text = "100%";
            currentFileLabel.Text = string.Empty;
            remainingLabel.Text = "Remaining: --";
            bytesLabel.Text = "Written: " + FileSizeFormatter.Format(verification.Image?.FileLength ?? 0) +
                              (verification.BytesHashed > 0 ? "; hashed " + FileSizeFormatter.Format(verification.BytesHashed) + " of file data" : string.Empty);
            if (verification.Passed)
            {
                statusLabel.ForeColor = palette.Text;
                statusLabel.Text = "ISO image created and verified. " + verification.LevelDescription + ".";
            }
            else
            {
                statusLabel.ForeColor = palette.ErrorText;
                statusLabel.Text = "ISO image created, but verification FAILED. See the verification report.";
                _taskbar?.SetState(TaskbarProgressState.Error);
            }

            SetStatus(statusLabel.Text);
        }

        private void CreateButton_EnabledChanged(object sender, EventArgs e)
        {
            createButton.BackColor = createButton.Enabled ? ThemeManager.Current.Accent : ThemeManager.Current.DisabledAccent;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            RequestCancel(true);
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            string target = !string.IsNullOrEmpty(_lastCreatedIso) && File.Exists(_lastCreatedIso)
                ? _lastCreatedIso
                : outputTextBox.Text.Trim();

            try
            {
                if (!ShellHelper.ShowOutput(target))
                {
                    MessageBox.Show(this, "The output folder does not exist yet. Choose an output location first.",
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Could not open output folder", ex);
                MessageBox.Show(this, "The output folder could not be opened.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenLogFolderMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(_log.LogDirectory);
                ShellHelper.OpenFolder(_log.LogDirectory);
            }
            catch (Exception ex)
            {
                _log.Error("Could not open log folder", ex);
                MessageBox.Show(this, "The log folder could not be opened:\n\n" + _log.LogDirectory, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            if (_busy)
            {
                return;
            }

            using (var form = new SettingsForm(_settings.Clone()))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                AppSettings updated = form.Settings;
                updated.LastSourceFolder = updated.RememberLastSource ? _settings.LastSourceFolder : string.Empty;
                updated.LastOutputFolder = updated.RememberLastOutput ? _settings.LastOutputFolder : string.Empty;
                _settings = updated;
            }

            if (!_settingsService.Save(_settings))
            {
                MessageBox.Show(this, "The settings could not be saved and will only apply until HT ISO Creator is closed.\n\nDetails were written to the log file.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _log.Info("Settings changed: format=" + _settings.DefaultFormat + ", theme=" + _settings.Theme +
                      ", rememberSource=" + _settings.RememberLastSource + ", rememberOutput=" + _settings.RememberLastOutput +
                      ", completionDialog=" + _settings.ShowCompletionDialog);

            ApplyTheme();
            ApplyWorkflowSettings();
            if (CurrentFormat != _settings.DefaultFormat)
            {
                SelectFormat(_settings.DefaultFormat);
            }

            if (_scannedPath != null)
            {
                SuggestDefaults(_scannedPath);
            }
        }

        private void ShortcutsMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "Ctrl+O\tSelect source folder\n" +
                "Ctrl+S\tChoose output file\n" +
                "Ctrl+R\tRefresh source analysis\n" +
                "F5\tCreate ISO\n" +
                "Esc\tCancel creation or verification\n" +
                "Ctrl+Shift+V\tVerify ISO\n" +
                "Ctrl+I\tISO information\n" +
                "Ctrl+K\tFilename compatibility\n" +
                "Ctrl+H\tCreation history\n" +
                "Ctrl+,\tSettings\n" +
                "F1\tThis list\n" +
                "Alt+F4\tExit\n\n" +
                "You can also drag a folder from File Explorer onto the window.",
                "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Opens a document from the Documentation folder installed next to the program.</summary>
        private void DocumentMenuItem_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            string path = Path.Combine(AppPaths.DocumentationFolder, (string)item.Tag);
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "The document was not found:\n\n" + path + "\n\nReinstall HT ISO Creator or download the documentation package.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ShellHelper.OpenDocument(path);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                _log.Error("Could not open document " + path, ex);
                MessageBox.Show(this, "The document could not be opened:\n\n" + path, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutForm())
            {
                about.ShowDialog(this);
            }
        }

        /// <summary>HTREASURE footer: "© {build year} HTREASURE LTD · An HTREASURE LTD product".</summary>
        internal static string BrandFooterText()
        {
            var match = System.Text.RegularExpressions.Regex.Match(AboutForm.CopyrightText(), @"\b(20\d\d)\b");
            return "© " + (match.Success ? match.Groups[1].Value + " " : string.Empty) + "HTREASURE LTD · An HTREASURE LTD product";
        }

        /// <summary>Help > Security Advisories and Help > About > Security Advisory History (HTREASURE software standards).</summary>
        private void SecurityAdvisoriesMenuItem_Click(object sender, EventArgs e)
        {
            ShowSecurityAdvisoryHistory(this);
        }

        internal static void ShowSecurityAdvisoryHistory(IWin32Window owner)
        {
            try
            {
                using (var form = new SecurityAdvisoryHistoryForm(Security.StandardsRuntime.Advisories))
                {
                    form.ShowDialog(owner);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.Security.Cryptography.CryptographicException || ex is Security.AdvisoryFormatException)
            {
                LoggingService.Instance.Error("Could not open the Security Advisory History", ex);
                MessageBox.Show(owner, "The Security Advisory History could not be opened: " + ex.Message, "Security Advisory History",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy)
            {
                if (_closeRequested)
                {
                    e.Cancel = true;
                    return;
                }

                DialogResult answer = MessageBox.Show(this,
                    "An ISO image is being created. Cancel it and close HT ISO Creator?",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                e.Cancel = true;
                if (answer == DialogResult.Yes)
                {
                    _closeRequested = true;
                    RequestCancel(false);
                }

                return;
            }

            _scanCancellation?.Cancel();
            if (!_settings.RememberLastSource)
            {
                _settings.LastSourceFolder = string.Empty;
            }

            if (!_settings.RememberLastOutput)
            {
                _settings.LastOutputFolder = string.Empty;
            }

            SaveWindowPlacement();
            _settingsService.Save(_settings);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (_busy)
            {
                e.Effect = DragDropEffects.None;
                statusStripLabel.Text = "Wait for the current ISO image to finish before dropping another folder.";
                return;
            }

            string error;
            string folder = DropValidator.GetSourceFolder(e.Data.GetData(DataFormats.FileDrop) as string[], out error);
            e.Effect = DragDropEffects.Copy;
            statusStripLabel.Text = folder != null ? "Release to use \"" + folder + "\" as the source folder." : error;
        }

        private void MainForm_DragLeave(object sender, EventArgs e)
        {
            statusStripLabel.Text = _statusText;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];

            // Finish handling after the drop returns, so Explorer is not blocked by dialogs or scanning.
            BeginInvoke(new Action(async () =>
            {
                string error = await AcceptDroppedPathsAsync(paths);
                if (error != null)
                {
                    MessageBox.Show(this, error, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }));
        }

        /// <summary>
        /// Uses a dropped folder as the source. Returns null on success, otherwise the reason it was rejected.
        /// </summary>
        internal async Task<string> AcceptDroppedPathsAsync(string[] paths)
        {
            statusStripLabel.Text = _statusText;
            if (_busy)
            {
                return "An ISO image is being created. Wait for it to finish before choosing another folder.";
            }

            string error;
            string folder = DropValidator.GetSourceFolder(paths, out error);
            if (folder == null)
            {
                _log.Info("Rejected dropped item(s): " + error);
                return error;
            }

            _log.Info("Source folder dropped: " + folder);
            sourceTextBox.Text = folder;
            Activate();
            await ScanSourceAsync(false);
            return null;
        }

        private void WebsiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                ShellHelper.OpenWebPage(ShellHelper.CompanyWebsite);
            }
            catch (Exception ex)
            {
                _log.Error("Could not open " + ShellHelper.CompanyWebsite, ex);
                MessageBox.Show(this, "The website could not be opened. You can visit it at:\n\n" + ShellHelper.CompanyWebsite,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DetailsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_analysis == null)
            {
                return;
            }

            const int MaxShown = 20;
            var text = new StringBuilder();
            if (_analysis.Errors.Count > 0)
            {
                text.AppendLine("These items cannot be read. The image cannot be created until they are readable or removed:");
                foreach (string error in _analysis.Errors.Take(MaxShown))
                {
                    text.AppendLine("• " + error);
                }

                if (_analysis.Errors.Count > MaxShown)
                {
                    text.AppendLine("...and " + (_analysis.Errors.Count - MaxShown) + " more (see the log file).");
                }
            }

            if (_analysis.Warnings.Count > 0)
            {
                if (text.Length > 0)
                {
                    text.AppendLine();
                }

                text.AppendLine("These items will not be included:");
                foreach (string warning in _analysis.Warnings.Take(MaxShown))
                {
                    text.AppendLine("• " + warning);
                }
            }

            MessageBox.Show(this, text.ToString(), "Source Folder Details", MessageBoxButtons.OK,
                _analysis.Errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void ElapsedTimer_Tick(object sender, EventArgs e)
        {
            elapsedLabel.Text = "Elapsed: " + FileSizeFormatter.FormatDuration(_operationStopwatch.Elapsed);
        }

        private void RequestCancel(bool confirm)
        {
            if (_cancellation == null || _cancellation.IsCancellationRequested)
            {
                return;
            }

            if (confirm)
            {
                DialogResult answer = MessageBox.Show(this,
                    _verifying
                        ? "Stop verifying the image?\n\nThe ISO image has already been created and will be kept, but it will be recorded as not verified."
                        : "Stop creating the ISO image?\n\nThe partially written file will be deleted. An existing file with the same name is not affected.",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                // The operation may have finished while the question was open.
                if (answer != DialogResult.Yes || _cancellation == null || _cancellation.IsCancellationRequested)
                {
                    return;
                }
            }

            _log.Info("Cancellation requested by the user");
            _cancellation.Cancel();
            cancelButton.Enabled = false;
            statusLabel.ForeColor = ThemeManager.Current.WarningText;
            statusLabel.Text = _verifying
                ? "Cancelling verification... the created image is kept."
                : "Cancelling... stopping at the next safe point and removing the incomplete file.";
            SetStatus("Cancelling...");
            _taskbar?.SetState(TaskbarProgressState.Paused);
        }

        /// <summary>
        /// Scans the folder in the source box (if it changed, or always when <paramref name="force"/>)
        /// and calculates the exact image size, both on a background thread.
        /// </summary>
        internal async Task ScanSourceAsync(bool force)
        {
            string text = sourceTextBox.Text.Trim();
            if (text.Length == 0)
            {
                _scannedPath = null;
                _analysis = null;
                SetSourceSummary("No folder selected. Tip: drag a folder from File Explorer onto this window.", ThemeManager.Current.DimText, false);
                UpdateAnalysisDisplay();
                return;
            }

            string error = PathValidator.ValidateSourceFolder(text);
            if (error != null)
            {
                _scannedPath = null;
                _analysis = null;
                SetSourceSummary(error, ThemeManager.Current.ErrorText, false);
                UpdateAnalysisDisplay();
                return;
            }

            string fullPath = Path.GetFullPath(text);
            if (!force && string.Equals(fullPath, _scannedPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _scanCancellation?.Cancel();
            var scan = new CancellationTokenSource();
            _scanCancellation = scan;
            _scanning = true;
            refreshButton.Enabled = false;
            refreshMenuItem.Enabled = false;
            _isoSizeCalculating = true;
            SetSourceSummary("Scanning folder...", ThemeManager.Current.DimText, false);
            SetStatus("Scanning " + fullPath);
            UpdateAnalysisDisplay();

            IsoFileSystemFormat format = CurrentFormat;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Tuple<FolderAnalysisResult, long?> scanned = await Task.Run(() =>
                {
                    FolderAnalysisResult analysis = _analysisService.Analyze(fullPath, scan.Token);
                    return Tuple.Create(analysis, CalculateSizeSafely(analysis, format));
                }, scan.Token);

                if (scan.IsCancellationRequested)
                {
                    return;
                }

                FolderAnalysisResult result = scanned.Item1;
                _analysis = result;
                _isoSize = scanned.Item2;
                _isoSizeCalculating = false;
                _sizeCalculationId++;
                _scannedPath = fullPath;
                if (_settings.RememberLastSource)
                {
                    _settings.LastSourceFolder = fullPath;
                }

                _log.Info("Source folder analysed: " + fullPath + " (" + result.FileCount + " files, " + result.DirectoryCount +
                          " folders, " + result.TotalBytes + " bytes, ISO size " + (_isoSize?.ToString() ?? "n/a") + ")");
                foreach (string problem in result.Errors)
                {
                    _log.Warning("Unreadable source item: " + problem);
                }

                if (result.Errors.Count > 0)
                {
                    SetSourceSummary(string.Format("{0:N0} item(s) cannot be read. The image cannot be created until this is resolved.", result.Errors.Count),
                        ThemeManager.Current.ErrorText, true);
                }
                else if (result.Warnings.Count > 0)
                {
                    SetSourceSummary(string.Format("{0:N0} item(s) will be skipped (links or junctions).", result.Warnings.Count),
                        ThemeManager.Current.WarningText, true);
                }
                else
                {
                    SetSourceSummary(string.Format("Analysis complete in {0:0.0} s.", stopwatch.Elapsed.TotalSeconds), ThemeManager.Current.DimText, false);
                }

                SetStatus("Ready");
                SuggestDefaults(fullPath);
            }
            catch (OperationCanceledException)
            {
                // A newer scan replaced this one.
            }
            catch (Exception ex)
            {
                _log.Error("Source folder scan failed: " + fullPath, ex);
                _scannedPath = null;
                _analysis = null;
                SetSourceSummary("The folder could not be read: " + FolderAnalysisService.DescribeAccessError(ex), ThemeManager.Current.ErrorText, false);
                SetStatus("Ready");
            }
            finally
            {
                if (ReferenceEquals(_scanCancellation, scan))
                {
                    _scanCancellation = null;
                    _scanning = false;
                    _isoSizeCalculating = false;
                    refreshButton.Enabled = !_busy;
                    refreshMenuItem.Enabled = !_busy;
                }

                scan.Dispose();
                UpdateValidation();
                UpdateAnalysisDisplay();
            }
        }

        /// <summary>Recalculates the image size after the file system option changes.</summary>
        private async Task RecalculateIsoSizeAsync()
        {
            FolderAnalysisResult analysis = _analysis;
            if (analysis == null || _scanning)
            {
                UpdateAnalysisDisplay();
                return;
            }

            int id = ++_sizeCalculationId;
            IsoFileSystemFormat format = CurrentFormat;
            _isoSizeCalculating = true;
            UpdateAnalysisDisplay();
            long? size = await Task.Run(() => CalculateSizeSafely(analysis, format));
            if (id != _sizeCalculationId || !ReferenceEquals(analysis, _analysis))
            {
                return;
            }

            _isoSize = size;
            _isoSizeCalculating = false;
            UpdateAnalysisDisplay();
        }

        private long? CalculateSizeSafely(FolderAnalysisResult analysis, IsoFileSystemFormat format)
        {
            try
            {
                return IsoSizeCalculator.Calculate(analysis, format);
            }
            catch (Exception ex)
            {
                _log.Error("ISO size calculation failed", ex);
                return null;
            }
        }

        private void UpdateAnalysisDisplay()
        {
            ThemePalette palette = ThemeManager.Current;
            formatValue.Text = FormatName(CurrentFormat);

            FolderAnalysisResult analysis = _analysis;
            if (analysis == null)
            {
                string placeholder = _scanning ? "Scanning..." : "--";
                filesValue.Text = placeholder;
                foldersValue.Text = placeholder;
                sourceSizeValue.Text = placeholder;
                isoSizeValue.Text = placeholder;
                isoSizeValue.ForeColor = palette.Text;
            }
            else
            {
                filesValue.Text = analysis.FileCount.ToString("N0");
                foldersValue.Text = analysis.DirectoryCount.ToString("N0");
                sourceSizeValue.Text = FileSizeFormatter.FormatWithBytes(analysis.TotalBytes);
                if (_isoSizeCalculating)
                {
                    isoSizeValue.Text = "Calculating...";
                    isoSizeValue.ForeColor = palette.Text;
                }
                else if (_isoSize.HasValue)
                {
                    isoSizeValue.Text = FileSizeFormatter.FormatWithBytes(_isoSize.Value);
                    isoSizeValue.ForeColor = palette.Text;
                }
                else
                {
                    isoSizeValue.Text = "Not available (a file is 4 GB or larger)";
                    isoSizeValue.ForeColor = palette.ErrorText;
                }
            }

            string outputFolder = GetValidOutputFolder();
            long? free = outputFolder == null ? null : DiskSpaceHelper.GetAvailableFreeSpace(outputFolder);
            if (!free.HasValue)
            {
                freeSpaceValue.Text = outputFolder == null ? "-- (choose an output location)" : "Unknown";
                freeSpaceValue.ForeColor = palette.Text;
            }
            else
            {
                bool enough = !_isoSize.HasValue || free.Value >= _isoSize.Value;
                freeSpaceValue.Text = FileSizeFormatter.Format(free.Value) + " on " + Path.GetPathRoot(outputFolder) +
                                      (enough ? string.Empty : " - not enough space");
                freeSpaceValue.ForeColor = enough ? palette.Text : palette.ErrorText;
            }

            toolTip.SetToolTip(freeSpaceValue, outputFolder ?? string.Empty);

            CapacityEvaluation capacity = CapacityEvaluation.Evaluate(analysis == null || _isoSizeCalculating ? null : _isoSize,
                SelectedCapacityBytes, SelectedCapacityProfile.Name);
            capacityStatusLabel.Text = capacity.State == CapacityState.SizeUnknown && analysis == null
                ? "Capacity " + FileSizeFormatter.Format(capacity.CapacityBytes.Value) + "."
                : capacity.Describe();
            capacityStatusLabel.ForeColor = capacity.State == CapacityState.Exceeds ? palette.ErrorText
                : capacity.State == CapacityState.Fits ? palette.Text
                : palette.DimText;
            toolTip.SetToolTip(capacityStatusLabel, capacityStatusLabel.Text +
                " A capacity profile does not change the ISO or its file system, and fitting does not guarantee that a particular drive or disc will accept the image.");
            UpdateFilenameStatus();
        }

        internal CapacityEvaluation CurrentCapacity =>
            CapacityEvaluation.Evaluate(_isoSize, SelectedCapacityBytes, SelectedCapacityProfile.Name);

        private string GetValidOutputFolder()
        {
            string output = outputTextBox.Text.Trim();
            string full;
            if (!PathValidator.TryGetFullPath(output, out full) || !Path.IsPathRooted(output))
            {
                return null;
            }

            string folder = Path.GetDirectoryName(full);
            return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : null;
        }

        private void SuggestDefaults(string sourcePath)
        {
            if (volumeTextBox.Text.Length == 0 || volumeTextBox.Text == _suggestedLabel)
            {
                _suggestedLabel = SuggestLabel(sourcePath);
                volumeTextBox.Text = _suggestedLabel;
            }

            string currentOutput = outputTextBox.Text.Trim();
            if (currentOutput.Length == 0 || currentOutput == _suggestedOutput)
            {
                string fileName = SafeFileName(FolderDisplayName(sourcePath));
                if (fileName.Length == 0)
                {
                    fileName = volumeTextBox.Text.Length > 0 ? volumeTextBox.Text : VolumeLabelValidator.DefaultLabel;
                }

                _suggestedOutput = Path.Combine(SuggestOutputFolder(sourcePath), fileName + ".iso");
                outputTextBox.Text = _suggestedOutput;
            }
        }

        private string SuggestLabel(string sourcePath)
        {
            string preferred = _settings.DefaultVolumeLabel;
            if (!string.IsNullOrEmpty(preferred) && VolumeLabelValidator.Validate(preferred, CurrentFormat) == null)
            {
                return preferred;
            }

            return VolumeLabelValidator.Suggest(FolderDisplayName(sourcePath), CurrentFormat);
        }

        /// <summary>
        /// Output folder priority: last used (when remembered), the configured default,
        /// the source folder's parent, then Documents. Never inside the source folder.
        /// </summary>
        private string SuggestOutputFolder(string sourcePath)
        {
            var candidates = new[]
            {
                _settings.RememberLastOutput ? _settings.LastOutputFolder : null,
                _settings.DefaultOutputFolder,
                sourcePath == null ? null : Directory.GetParent(sourcePath)?.FullName
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate) &&
                    (sourcePath == null || !PathValidator.IsSameOrInside(candidate, sourcePath)))
                {
                    return candidate;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void RememberOutputFolder(string outputPath)
        {
            if (_settings.RememberLastOutput)
            {
                _settings.LastOutputFolder = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            }
        }

        /// <summary>
        /// Validates all inputs, shows error icons next to fields that have content,
        /// enables or disables commands, and returns the first error (or null).
        /// </summary>
        internal string UpdateValidation()
        {
            ThemePalette palette = ThemeManager.Current;
            IsoFileSystemFormat format = CurrentFormat;
            string source = sourceTextBox.Text.Trim();
            string output = outputTextBox.Text.Trim();

            string sourceError = PathValidator.ValidateSourceFolder(source);
            string labelError = VolumeLabelValidator.Validate(volumeTextBox.Text, format);
            string outputError = PathValidator.ValidateOutputFile(PathValidator.EnsureIsoExtension(output), source);

            errorProvider.SetError(sourceTextBox, source.Length > 0 ? sourceError ?? string.Empty : string.Empty);
            errorProvider.SetError(volumeTextBox, volumeTextBox.Text.Length > 0 ? labelError ?? string.Empty : string.Empty);
            errorProvider.SetError(outputTextBox, output.Length > 0 ? outputError ?? string.Empty : string.Empty);

            int maxLength = VolumeLabelValidator.GetMaxLength(format);
            bool showLabelError = labelError != null && volumeTextBox.Text.Length > 0;
            volumeHintLabel.ForeColor = showLabelError ? palette.ErrorText : palette.DimText;
            volumeHintLabel.Text = showLabelError ? labelError : "Up to " + maxLength + " characters: A-Z, 0-9 and underscore.";
            formatStripLabel.Text = FormatName(format);

            bool valid = sourceError == null && labelError == null && outputError == null;
            createButton.Enabled = !_busy && valid;
            createIsoMenuItem.Enabled = !_busy && valid;

            return sourceError ?? labelError ?? outputError;
        }

        internal void SetBusy(bool busy)
        {
            _busy = busy;
            sourceTextBox.ReadOnly = busy;
            outputTextBox.ReadOnly = busy;
            volumeTextBox.ReadOnly = busy;
            browseButton.Enabled = !busy;
            refreshButton.Enabled = !busy && !_scanning;
            saveAsButton.Enabled = !busy;
            fileSystemComboBox.Enabled = !busy;
            selectSourceMenuItem.Enabled = !busy;
            chooseOutputMenuItem.Enabled = !busy;
            refreshMenuItem.Enabled = !busy && !_scanning;
            settingsMenuItem.Enabled = !busy;
            verifyIsoMenuItem.Enabled = !busy;
            isoInfoMenuItem.Enabled = !busy;
            compatibilityMenuItem.Enabled = !busy;
            historyMenuItem.Enabled = !busy;
            compatibilityLinkLabel.Enabled = !busy;
            strictModeRadioButton.Enabled = !busy;
            compatibilityModeRadioButton.Enabled = !busy;
            exportNamesButton.Enabled = !busy && _analysis != null;
            capacityComboBox.Enabled = !busy;
            customCapacityUpDown.Enabled = !busy;
            verifyCheckBox.Enabled = !busy;
            hashCheckBox.Enabled = !busy && verifyCheckBox.Checked;
            cancelButton.Enabled = busy;
            statusLabel.ForeColor = ThemeManager.Current.Text;

            if (busy)
            {
                _operationStopwatch.Restart();
                elapsedTimer.Start();
                ElapsedTimer_Tick(this, EventArgs.Empty);
                _taskbar?.SetState(TaskbarProgressState.Indeterminate);
            }
            else
            {
                _operationStopwatch.Stop();
                elapsedTimer.Stop();
            }

            UpdateValidation();
        }

        private void ShowProgress(IsoProgressInfo info)
        {
            if (_cancellation != null && _cancellation.IsCancellationRequested)
            {
                // Keep the "Cancelling..." message visible until the operation stops.
                return;
            }

            statusLabel.Text = info.Message;
            SetStatus(info.Message);

            double? percent = info.Percent;
            if (percent.HasValue)
            {
                if (progressBar.Style != ProgressBarStyle.Continuous)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                }

                progressBar.Value = Math.Max(0, Math.Min(ProgressScale, (int)(percent.Value * ProgressScale / 100)));
                percentLabel.Text = Math.Floor(percent.Value).ToString("0") + "%";
                _taskbar?.SetState(TaskbarProgressState.Normal);
                _taskbar?.SetValue(info.BytesWritten, info.TotalBytes);
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                percentLabel.Text = string.Empty;
                _taskbar?.SetState(TaskbarProgressState.Indeterminate);
            }

            bool verifying = info.Stage == IsoOperationStage.Verifying;
            currentFileLabel.Text = string.IsNullOrEmpty(info.CurrentFile)
                ? string.Empty
                : (info.Stage == IsoOperationStage.CheckingFiles ? "Checking: " : verifying ? "Comparing: " : "Reading: ") + info.CurrentFile;

            if (info.TotalFiles > 0)
            {
                string verb = info.Stage == IsoOperationStage.CheckingFiles ? "Files checked" : verifying ? "Files compared" : "Files written";
                filesLabel.Text = string.Format("{0}: {1:N0} of {2:N0}", verb, info.FilesProcessed, info.TotalFiles);
            }
            else
            {
                filesLabel.Text = verifying ? filesLabel.Text : "Files: --";
            }

            if (info.TotalBytes > 0)
            {
                bytesLabel.Text = (verifying ? "Read: " : "Written: ") + FileSizeFormatter.Format(info.BytesWritten) + " of " + FileSizeFormatter.Format(info.TotalBytes);
                speedLabel.Text = info.Elapsed.TotalSeconds >= 1 && info.BytesWritten > 0
                    ? "Average speed: " + FileSizeFormatter.Format((long)(info.BytesWritten / info.Elapsed.TotalSeconds)) + "/s"
                    : "Speed: measuring...";
            }
            else if (!verifying)
            {
                bytesLabel.Text = "Written: --";
                speedLabel.Text = "Speed: --";
            }

            if (info.EstimatedRemaining.HasValue)
            {
                remainingLabel.Text = "Remaining: about " + FileSizeFormatter.FormatDuration(info.EstimatedRemaining.Value);
            }
            else
            {
                remainingLabel.Text = (info.Stage == IsoOperationStage.Writing || verifying) && info.TotalBytes > 0
                    ? "Remaining: calculating..."
                    : "Remaining: --";
            }
        }

        private void SetProgressComplete(IsoCreationResult result)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = ProgressScale;
            percentLabel.Text = "100%";
            statusLabel.ForeColor = ThemeManager.Current.Text;
            statusLabel.Text = "ISO image created successfully.";
            SetStatus("Created " + result.OutputPath);
            currentFileLabel.Text = string.Empty;
            elapsedLabel.Text = "Elapsed: " + FileSizeFormatter.FormatDuration(result.Elapsed);
            remainingLabel.Text = "Remaining: --";
            bytesLabel.Text = "Written: " + FileSizeFormatter.Format(result.ImageSize);
            speedLabel.Text = result.Elapsed.TotalSeconds >= 0.5
                ? "Average speed: " + FileSizeFormatter.Format((long)(result.ImageSize / result.Elapsed.TotalSeconds)) + "/s"
                : "Speed: --";
            filesLabel.Text = string.Format("Files written: {0:N0} of {0:N0}", result.FileCount);
        }

        private void ResetProgressDisplay(string message)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            percentLabel.Text = string.Empty;
            statusLabel.ForeColor = ThemeManager.Current.Text;
            statusLabel.Text = message;
            SetStatus(message);
            currentFileLabel.Text = string.Empty;
            remainingLabel.Text = "Remaining: --";
            filesLabel.Text = "Files: --";
            bytesLabel.Text = "Written: --";
            speedLabel.Text = "Speed: --";
        }

        private void ShowCompletion(IsoCreationResult result, VerificationResult verification, bool verificationCancelled, FilenameConversionReport conversionReport)
        {
            // A failed verification is always shown, even when completion notifications are turned off.
            bool verificationFailed = verification != null && !verification.Passed;
            if (!_settings.ShowCompletionDialog && !verificationFailed)
            {
                SetStatus((verification != null ? "ISO image created and verified: " : "ISO image created successfully: ") + result.OutputPath);
                System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            using (var completion = new CompletionForm(result, verification, verificationCancelled) { ConversionReport = conversionReport })
            {
                completion.ShowDialog(this);
                if (completion.DoNotShowAgain)
                {
                    _settings.ShowCompletionDialog = false;
                    _settingsService.Save(_settings);
                    _log.Info("Completion dialog turned off from the dialog");
                }
            }
        }

        private void ApplyTheme()
        {
            ThemeManager.SetTheme(_settings.Theme);
            ThemeManager.Apply(this);
            CreateButton_EnabledChanged(this, EventArgs.Empty);
            UpdateValidation();
            UpdateAnalysisDisplay();
            if (_analysis == null && !_scanning)
            {
                sourceSummaryLabel.ForeColor = ThemeManager.Current.DimText;
            }
        }

        private void SetSourceSummary(string text, Color color, bool showDetails)
        {
            sourceSummaryLabel.Text = text;
            sourceSummaryLabel.ForeColor = color;
            detailsLinkLabel.Visible = showDetails;
        }

        private void SetStatus(string text)
        {
            _statusText = text;
            statusStripLabel.Text = text;
        }

        private static string FormatName(IsoFileSystemFormat format)
        {
            return FileSystemFormats.Name(format);
        }

        private static string FolderDisplayName(string path)
        {
            var info = new DirectoryInfo(path);
            return info.Parent == null ? info.FullName.TrimEnd('\\').TrimEnd(':') : info.Name;
        }

        private static string SafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return sb.ToString().Trim().TrimEnd('.');
        }
    }
}
