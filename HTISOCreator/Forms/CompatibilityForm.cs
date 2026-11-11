using System;
using System.Collections.Generic;
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
    /// Shows the filename compatibility report and a preview of every name conversion. Opened
    /// before creation (with Create / Cancel) or from the Tools menu (view only).
    /// </summary>
    public partial class CompatibilityForm : Form
    {
        private const int MaxRenameRows = 5000;
        private readonly CompatibilityReport _report;
        private readonly bool _allowContinue;
        private readonly FilenameConversionReport _conversionReport;

        public CompatibilityForm()
            : this(new CompatibilityReport(), false)
        {
        }

        /// <param name="conversionReport">Source, destination and mode for the exported TXT/CSV conversion report.</param>
        public CompatibilityForm(CompatibilityReport report, bool allowContinue, FilenameConversionReport conversionReport = null)
        {
            _report = report;
            _allowContinue = allowContinue;
            _conversionReport = conversionReport;
            InitializeComponent();
            Icon = AppIcon.Icon;
        }

        private void CompatibilityForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            ThemePalette palette = ThemeManager.Current;

            int errors = _report.Findings.Count(f => f.Severity == FindingSeverity.Error);
            int warnings = _report.Findings.Count(f => f.Severity == FindingSeverity.Warning);
            bool strictBlocked = _report.FilenameMode == FilenameMode.Strict && _report.Renames.Count > 0;
            summaryLabel.Text = strictBlocked
                ? "Strict Mode: " + _report.Renames.Count + " name(s) cannot be preserved, so the image will not be created."
                : errors > 0
                ? "The image cannot be created until " + errors + " problem group(s) are resolved."
                : warnings > 0
                    ? "The image can be created. Review " + warnings + " warning(s) and the name conversions below."
                    : "No compatibility problems were found.";
            summaryLabel.ForeColor = errors > 0 ? palette.ErrorText : warnings > 0 ? palette.WarningText : palette.Accent;

            switch (_report.Format)
            {
                case IsoFileSystemFormat.Iso9660Joliet:
                    rulesLabel.Text = "ISO 9660 + Joliet: names up to 64 Unicode characters without ; * ? : / \\. Files must be smaller than 4 GB. A plain ISO 9660 name tree is also written for older readers.";
                    break;
                case IsoFileSystemFormat.Udf:
                    rulesLabel.Text = "UDF (Windows IMAPI2): names up to " + FileSystemFormats.UdfMaxNameLength + " Unicode characters, any characters Windows allows. Files of 4 GB and larger are supported.";
                    break;
                case IsoFileSystemFormat.Iso9660JolietUdf:
                    rulesLabel.Text = "ISO 9660 + Joliet + UDF (Windows IMAPI2): UDF names up to " + FileSystemFormats.UdfMaxNameLength + " characters, plus Joliet and ISO 9660 names for older readers. Files must be smaller than 4 GB.";
                    break;
                default:
                    rulesLabel.Text = "ISO 9660 only: names use A-Z, 0-9 and _ (upper case), at most 30 characters with an extension of up to 8. At most 8 folder levels and 255-character paths are recommended. Files must be smaller than 4 GB.";
                    break;
            }

            rulesLabel.Text += Environment.NewLine +
                               (_report.FilenameMode == FilenameMode.Strict
                                   ? "Strict Mode: no name may change."
                                   : "Compatibility Mode: conversions are allowed only after you confirm them.") +
                               (_report.Format == IsoFileSystemFormat.Iso9660Joliet ? " Plain ISO 9660 alias names are not filename changes."
                                   : _report.Format == IsoFileSystemFormat.Iso9660JolietUdf ? " Joliet / ISO 9660 alias names are not filename changes."
                                   : string.Empty);

            findingsListView.BeginUpdate();
            foreach (CompatibilityFinding finding in _report.Findings)
            {
                var item = new ListViewItem(new[] { finding.Severity.ToString(), finding.Title, finding.Detail })
                {
                    Tag = finding,
                    ForeColor = finding.Severity == FindingSeverity.Error ? palette.ErrorText
                        : finding.Severity == FindingSeverity.Warning ? palette.WarningText
                        : palette.Text
                };
                findingsListView.Items.Add(item);
            }

            findingsListView.EndUpdate();
            if (findingsListView.Items.Count > 0)
            {
                findingsListView.Items[0].Selected = true;
            }

            FillRenames(string.Empty);

            bool needsConfirmation = _allowContinue && !_report.HasErrors && _report.Renames.Count > 0 &&
                                     _report.FilenameMode == FilenameMode.Compatibility;
            _needsConfirmation = needsConfirmation;
            confirmCheckBox.Visible = needsConfirmation;
            confirmCheckBox.Checked = false;
            int programFiles = _report.Renames.Count(r => !r.IsDirectory && CompatibilityService.IsProgramFile(r.SourcePath));
            confirmCheckBox.Text = "I have reviewed all " + _report.Renames.Count + " name conversion(s) and accept them. " +
                                   (programFiles > 0
                                       ? programFiles + " are DLL or program files, which may stop working when renamed."
                                       : "Software that expects the original names may not find renamed files.");
            confirmCheckBox.ForeColor = programFiles > 0 ? palette.WarningText : palette.Text;
            createButton.Visible = _allowContinue;
            closeButton.Text = _allowContinue ? "Cancel" : "Close";
            UpdateCreateButton();
        }

        /// <summary>True when the user ticked the confirmation for the listed conversions.</summary>
        internal bool ConversionsConfirmed => _needsConfirmation && confirmCheckBox.Checked;

        private bool _needsConfirmation;

        private void ConfirmCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCreateButton();
        }

        private void UpdateCreateButton()
        {
            createButton.Enabled = _allowContinue && !_report.HasErrors && (!_needsConfirmation || confirmCheckBox.Checked);
            createButton.BackColor = createButton.Enabled ? ThemeManager.Current.Accent : ThemeManager.Current.DisabledAccent;
            AcceptButton = createButton.Enabled ? createButton : closeButton;
        }

        private void FindingsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var finding = findingsListView.SelectedItems.Count > 0 ? (CompatibilityFinding)findingsListView.SelectedItems[0].Tag : null;
            itemsListBox.BeginUpdate();
            itemsListBox.Items.Clear();
            if (finding != null)
            {
                foreach (string item in finding.Items)
                {
                    itemsListBox.Items.Add(item);
                }
            }

            itemsListBox.EndUpdate();
            itemsLabel.Text = finding == null || finding.Items.Count == 0 ? "Affected items" : "Affected items (" + finding.Items.Count + ")";
        }

        private void RenameFilterTextBox_TextChanged(object sender, EventArgs e)
        {
            FillRenames(renameFilterTextBox.Text.Trim());
        }

        private void FillRenames(string filter)
        {
            IEnumerable<IsoNameChange> renames = _report.Renames;
            if (filter.Length > 0)
            {
                renames = renames.Where(r => r.SourcePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             r.ImagePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             r.Reason.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            List<IsoNameChange> shown = renames.ToList();
            renamesListView.BeginUpdate();
            renamesListView.Items.Clear();
            foreach (IsoNameChange change in shown.Take(MaxRenameRows))
            {
                // Own names side by side; descendants of a renamed folder are not repeated.
                string folder = Path.GetDirectoryName(change.SourcePath);
                var item = new ListViewItem(new[]
                {
                    Path.GetFileName(change.SourcePath), Path.GetFileName(change.ImagePath), change.Reason,
                    change.IsDirectory ? "Folder" : "File", string.IsNullOrEmpty(folder) ? "(top level)" : folder
                })
                {
                    ToolTipText = change.SourcePath + Environment.NewLine + "→ " + change.ImagePath
                };
                if (FilenameConversionReport.IsCollision(change) || (!change.IsDirectory && CompatibilityService.IsProgramFile(change.SourcePath)))
                {
                    item.ForeColor = ThemeManager.Current.WarningText;
                }

                renamesListView.Items.Add(item);
            }

            // Long names differ near the end, so both name columns are sized to show them in full
            // (within limits; the list scrolls sideways and each row's tooltip has the full paths).
            if (renamesListView.Items.Count > 0)
            {
                // .NET 4.6.2 has no LogicalToDeviceUnits; the font height gives the display scale (15 px at 100%).
                float scale = renamesListView.Font.Height / 15f;
                foreach (ColumnHeader column in new[] { sourceNameColumn, imageNameColumn })
                {
                    column.Width = -1;
                    column.Width = (int)Math.Max(160 * scale, Math.Min(column.Width + 12 * scale, 520 * scale));
                }
            }

            renamesListView.EndUpdate();
            renamesLabel.Text = _report.Renames.Count == 0
                ? "Name conversion preview: no names change"
                : "Name conversion preview (" + (shown.Count == _report.Renames.Count ? shown.Count.ToString() : shown.Count + " of " + _report.Renames.Count) +
                  (shown.Count > MaxRenameRows ? ", first " + MaxRenameRows + " shown; export for the full list" : string.Empty) + ")";
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save Compatibility Report";
                dialog.Filter = _conversionReport != null
                    ? "Text report (*.txt)|*.txt|CSV for Excel - name conversions (*.csv)|*.csv"
                    : "Text report (*.txt)|*.txt";
                dialog.FileName = "Filename compatibility report.txt";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    SaveReport(dialog.FileName);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    LoggingService.Instance.Error("Compatibility report could not be saved", ex);
                    MessageBox.Show(this, "The report could not be saved: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>CSV: the name conversion table. TXT: the findings followed by the full conversion report.</summary>
        internal void SaveReport(string path)
        {
            if (_conversionReport != null && string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                _conversionReport.Save(path);
                return;
            }

            string text = BuildReportText(_report);
            if (_conversionReport != null)
            {
                text += Environment.NewLine + _conversionReport.ToText();
            }

            File.WriteAllText(path, text, new UTF8Encoding(true));
        }

        internal static string BuildReportText(CompatibilityReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("HT ISO Creator - Compatibility Report");
            sb.AppendLine("File system: " + FileSystemFormats.Name(report.Format) + " (" + FileSystemFormats.EngineName(report.Format) + ")");
            sb.AppendLine("Filename mode: " + FilenameConversionReport.ModeName(report.FilenameMode));
            string aliasNote = CompatibilityService.AliasNoteFor(report.Format);
            if (aliasNote != null)
            {
                sb.AppendLine(aliasNote);
            }

            sb.AppendLine();
            foreach (CompatibilityFinding finding in report.Findings)
            {
                sb.AppendLine("[" + finding.Severity.ToString().ToUpperInvariant() + "] " + finding.Title + ": " + finding.Detail);
                foreach (string item in finding.Items)
                {
                    sb.AppendLine("    " + item);
                }
            }

            sb.AppendLine();
            sb.AppendLine("Name conversions (" + report.Renames.Count + ")");
            foreach (IsoNameChange change in report.Renames)
            {
                sb.AppendLine("  " + change.SourcePath + "  ->  " + change.ImagePath + "  [" + change.Reason + "]");
            }

            return sb.ToString();
        }
    }
}
