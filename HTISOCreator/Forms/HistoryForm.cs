using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Creation history: search, filter, sort, delete, open the output folder, and verify an ISO
    /// from its entry.
    /// </summary>
    public partial class HistoryForm : Form
    {
        private static readonly string[] Filters =
        {
            "All entries", "Succeeded", "Failed", "Cancelled", "Verification passed", "Verification failed", "Not verified"
        };

        private readonly HistoryService _history;
        private readonly IsoVerificationService _verifier;
        private readonly bool _hashDefault;
        private readonly bool _mountDefault;
        private List<HistoryEntry> _all = new List<HistoryEntry>();
        private List<HistoryEntry> _visible = new List<HistoryEntry>();
        private int _sortColumn;
        private bool _sortAscending;

        public HistoryForm()
            : this(new HistoryService(), new IsoVerificationService(), false, false)
        {
        }

        public HistoryForm(HistoryService history, IsoVerificationService verifier, bool hashDefault, bool mountDefault)
        {
            _history = history;
            _verifier = verifier;
            _hashDefault = hashDefault;
            _mountDefault = mountDefault;
            InitializeComponent();
            Icon = AppIcon.Icon;
            filterComboBox.Items.AddRange(Filters);
            filterComboBox.SelectedIndex = 0;
        }

        internal IReadOnlyList<HistoryEntry> VisibleEntries => _visible;

        private void HistoryForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            Reload();
        }

        /// <summary>Re-reads the history file and refreshes the list.</summary>
        internal void Reload()
        {
            _all = _history.Load();
            ApplyView();
        }

        internal void SetSearch(string text)
        {
            searchTextBox.Text = text;
        }

        internal void SetFilter(int index)
        {
            filterComboBox.SelectedIndex = index;
        }

        /// <summary>Sorts by a column; choosing the same column again reverses the order.</summary>
        internal void SortBy(int column)
        {
            _sortAscending = column == _sortColumn ? !_sortAscending : column != 0;
            _sortColumn = column;
            ApplyView();
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyView();
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyView();
        }

        private void HistoryListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            SortBy(e.Column);
        }

        private void HistoryListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDetails();
        }

        private void HistoryListView_DoubleClick(object sender, EventArgs e)
        {
            OpenFolderButton_Click(sender, e);
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            HistoryEntry entry = SelectedEntries().FirstOrDefault();
            if (entry == null)
            {
                return;
            }

            try
            {
                if (!ShellHelper.ShowOutput(entry.OutputPath))
                {
                    MessageBox.Show(this, "Neither the ISO file nor its folder exists any more:\n\n" + entry.OutputPath, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Could not open output folder from history", ex);
                MessageBox.Show(this, "The folder could not be opened.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerifyButton_Click(object sender, EventArgs e)
        {
            HistoryEntry entry = SelectedEntries().FirstOrDefault();
            if (entry == null)
            {
                return;
            }

            if (!File.Exists(entry.OutputPath))
            {
                MessageBox.Show(this, "The ISO file no longer exists:\n\n" + entry.OutputPath, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string source = Directory.Exists(entry.SourceFolder) ? entry.SourceFolder : null;
            using (var form = new VerificationForm(_verifier, entry.OutputPath, source, _hashDefault && source != null, _mountDefault)
            {
                // The rule the image was created under; older entries only report differences.
                NameRule = entry.FilenameMode
            })
            {
                form.ShowDialog(this);
                if (form.LastResult != null)
                {
                    entry.ApplyVerification(form.LastResult);
                    _history.Update(entry);
                    Reload();
                }
            }
        }

        private void CopyPathButton_Click(object sender, EventArgs e)
        {
            HistoryEntry entry = SelectedEntries().FirstOrDefault();
            if (entry == null)
            {
                return;
            }

            try
            {
                Clipboard.SetText(entry.OutputPath);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show(this, "The clipboard is in use by another program. Try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export History";
                dialog.Filter = "CSV files (*.csv)|*.csv";
                dialog.FileName = "HT ISO Creator history.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllText(dialog.FileName, HistoryService.ExportCsv(_visible), new UTF8Encoding(true));
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    LoggingService.Instance.Error("History export failed", ex);
                    MessageBox.Show(this, "The history could not be exported: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            List<HistoryEntry> selected = SelectedEntries();
            if (selected.Count == 0)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(this,
                "Remove " + selected.Count + " entr" + (selected.Count == 1 ? "y" : "ies") + " from the history?\n\nThe ISO files themselves are not deleted.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
            {
                SaveOrWarn(_history.Remove(selected.Select(s => s.Id)));
                Reload();
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (_all.Count == 0)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(this,
                "Remove all " + _all.Count + " entries from the history?\n\nThe ISO files themselves are not deleted. This cannot be undone.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
            {
                SaveOrWarn(_history.Clear());
                Reload();
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SaveOrWarn(bool saved)
        {
            if (!saved)
            {
                MessageBox.Show(this, "The history file could not be updated. Details were written to the log file.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyView()
        {
            string search = searchTextBox.Text.Trim();
            IEnumerable<HistoryEntry> query = _all.Where(e => MatchesFilter(e, filterComboBox.SelectedIndex));
            if (search.Length > 0)
            {
                query = query.Where(e => Contains(e.OutputPath, search) || Contains(e.SourceFolder, search) ||
                                         Contains(e.VolumeLabel, search) || Contains(e.Message, search));
            }

            _visible = Sort(query).ToList();

            historyListView.BeginUpdate();
            historyListView.Items.Clear();
            ThemePalette palette = ThemeManager.Current;
            foreach (HistoryEntry entry in _visible)
            {
                var item = new ListViewItem(new[]
                {
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture),
                    entry.Outcome.ToString(),
                    Path.GetFileName(entry.OutputPath),
                    entry.VolumeLabel,
                    entry.FormatName,
                    entry.Outcome == CreationOutcome.Succeeded ? FileSizeFormatter.Format(entry.ImageSize) : string.Empty,
                    entry.FileCount.ToString("N0", CultureInfo.CurrentCulture),
                    FileSizeFormatter.FormatDuration(entry.Duration),
                    VerificationText(entry.Verification),
                    Path.GetDirectoryName(entry.OutputPath) ?? string.Empty
                })
                {
                    Tag = entry,
                    ForeColor = entry.Outcome == CreationOutcome.Failed || entry.Verification == HistoryVerificationStatus.Failed ? palette.ErrorText
                        : entry.Outcome == CreationOutcome.Cancelled ? palette.DimText
                        : palette.Text
                };
                historyListView.Items.Add(item);
            }

            historyListView.EndUpdate();
            countLabel.Text = _visible.Count == _all.Count
                ? _all.Count + " entr" + (_all.Count == 1 ? "y" : "ies")
                : _visible.Count + " of " + _all.Count + " entries";
            UpdateDetails();
        }

        private IEnumerable<HistoryEntry> Sort(IEnumerable<HistoryEntry> entries)
        {
            Func<HistoryEntry, IComparable> key;
            switch (_sortColumn)
            {
                case 1: key = e => e.Outcome.ToString(); break;
                case 2: key = e => Path.GetFileName(e.OutputPath); break;
                case 3: key = e => e.VolumeLabel; break;
                case 4: key = e => e.FormatName; break;
                case 5: key = e => e.ImageSize; break;
                case 6: key = e => e.FileCount; break;
                case 7: key = e => e.DurationSeconds; break;
                case 8: key = e => e.Verification.ToString(); break;
                case 9: key = e => e.OutputPath; break;
                default: key = e => e.Timestamp; break;
            }

            return _sortAscending ? entries.OrderBy(key).ThenByDescending(e => e.Timestamp) : entries.OrderByDescending(key).ThenByDescending(e => e.Timestamp);
        }

        private static bool MatchesFilter(HistoryEntry entry, int filter)
        {
            switch (filter)
            {
                case 1: return entry.Outcome == CreationOutcome.Succeeded;
                case 2: return entry.Outcome == CreationOutcome.Failed;
                case 3: return entry.Outcome == CreationOutcome.Cancelled;
                case 4: return entry.Verification == HistoryVerificationStatus.Passed;
                case 5: return entry.Verification == HistoryVerificationStatus.Failed;
                case 6: return entry.Outcome == CreationOutcome.Succeeded && entry.Verification != HistoryVerificationStatus.Passed && entry.Verification != HistoryVerificationStatus.Failed;
                default: return true;
            }
        }

        private void UpdateDetails()
        {
            List<HistoryEntry> selected = SelectedEntries();
            HistoryEntry entry = selected.FirstOrDefault();
            bool one = selected.Count == 1;
            openFolderButton.Enabled = one;
            verifyButton.Enabled = one && entry.Outcome == CreationOutcome.Succeeded;
            copyPathButton.Enabled = one;
            deleteButton.Enabled = selected.Count > 0;
            clearButton.Enabled = _all.Count > 0;
            exportButton.Enabled = _visible.Count > 0;

            if (entry == null)
            {
                detailsTextBox.Text = _all.Count == 0 ? "No ISO images have been created yet." : "Select an entry to see its details.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Created: " + entry.Timestamp.ToString("F", CultureInfo.CurrentCulture) + "    Result: " + entry.Outcome +
                          (string.IsNullOrEmpty(entry.LogReference) ? string.Empty : "    Log reference: " + entry.LogReference));
            sb.AppendLine("Source: " + entry.SourceFolder);
            sb.AppendLine("ISO file: " + entry.OutputPath + (File.Exists(entry.OutputPath) || entry.Outcome != CreationOutcome.Succeeded ? string.Empty : "  (no longer exists)"));
            sb.AppendLine("Volume: " + entry.VolumeLabel + "    Format: " + entry.FormatName + "    Files: " + entry.FileCount.ToString("N0", CultureInfo.CurrentCulture) +
                          "    Folders: " + entry.DirectoryCount.ToString("N0", CultureInfo.CurrentCulture));
            sb.AppendLine("Verification: " + VerificationText(entry.Verification) +
                          (entry.VerifiedAt.HasValue ? " on " + entry.VerifiedAt.Value.ToString("g", CultureInfo.CurrentCulture) : string.Empty) +
                          (string.IsNullOrEmpty(entry.VerificationDetail) ? string.Empty : " - " + entry.VerificationDetail));
            if (!string.IsNullOrEmpty(entry.Message))
            {
                sb.AppendLine("Message: " + entry.Message);
            }

            detailsTextBox.Text = sb.ToString().TrimEnd();
        }

        private List<HistoryEntry> SelectedEntries()
        {
            return historyListView.SelectedItems.Cast<ListViewItem>().Select(i => (HistoryEntry)i.Tag).ToList();
        }

        private static string VerificationText(HistoryVerificationStatus status)
        {
            switch (status)
            {
                case HistoryVerificationStatus.Passed: return "Passed";
                case HistoryVerificationStatus.Failed: return "FAILED";
                case HistoryVerificationStatus.Cancelled: return "Cancelled";
                default: return "Not verified";
            }
        }

        private static bool Contains(string value, string search)
        {
            return value != null && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
