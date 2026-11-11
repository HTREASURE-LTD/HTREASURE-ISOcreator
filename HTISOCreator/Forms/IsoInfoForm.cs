using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// Shows what an existing ISO image records about itself. Every value comes from the image's
    /// volume descriptors and directory records; values the image does not record are shown as
    /// "Not recorded" rather than guessed.
    /// </summary>
    public partial class IsoInfoForm : Form
    {
        private const int MaxTreeNodes = 20000;
        private const string NotRecorded = "Not recorded";
        private IsoImageInfo _info;
        private CancellationTokenSource _cancellation;

        public IsoInfoForm()
            : this(null)
        {
        }

        public IsoInfoForm(string isoPath)
        {
            InitializeComponent();
            Icon = AppIcon.Icon;
            pathTextBox.Text = isoPath ?? string.Empty;
        }

        internal IsoImageInfo CurrentInfo => _info;

        private async void IsoInfoForm_Load(object sender, EventArgs e)
        {
            ThemeManager.Apply(this);
            ShowInfo(null, "Choose an ISO file to inspect.");
            if (File.Exists(pathTextBox.Text.Trim()))
            {
                await OpenImageAsync();
            }
        }

        private async void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an ISO image";
                dialog.Filter = "ISO image files (*.iso)|*.iso|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                pathTextBox.Text = dialog.FileName;
            }

            await OpenImageAsync();
        }

        private async void OpenButton_Click(object sender, EventArgs e)
        {
            await OpenImageAsync();
        }

        /// <summary>Reads the image named in the path box on a background thread.</summary>
        internal async Task OpenImageAsync()
        {
            string path = pathTextBox.Text.Trim();
            if (!File.Exists(path))
            {
                ShowInfo(null, "The file does not exist.");
                return;
            }

            _cancellation?.Cancel();
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            openButton.Enabled = false;
            ShowInfo(null, "Reading the image...");
            try
            {
                IsoImageInfo info = await Task.Run(() => IsoImageReader.Read(path, cancellation.Token), cancellation.Token);
                ShowInfo(info, null);
            }
            catch (OperationCanceledException)
            {
                // Replaced by a newer request or the window closed.
            }
            catch (InvalidIsoImageException ex)
            {
                ShowInfo(null, ex.Message);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                LoggingService.Instance.Error("ISO information could not be read: " + path, ex);
                ShowInfo(null, "The file could not be read: " + ex.Message);
            }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null;
                    openButton.Enabled = true;
                }

                cancellation.Dispose();
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (_info == null)
            {
                return;
            }

            try
            {
                Clipboard.SetText(BuildText(_info));
                copyButton.Text = "Copied";
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show(this, "The clipboard is in use by another program. Try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void IsoInfoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancellation?.Cancel();
        }

        private void ShowInfo(IsoImageInfo info, string message)
        {
            _info = info;
            copyButton.Enabled = info != null;
            copyButton.Text = "&Copy Details";
            fileNameValue.Text = info == null ? "--" : Path.GetFileName(info.FilePath);
            fileSizeValue.Text = info == null ? "--" : FileSizeFormatter.FormatWithBytes(info.FileLength);
            declaredSizeValue.Text = info == null ? "--" : FileSizeFormatter.FormatWithBytes(info.DeclaredVolumeBytes);
            filesValue.Text = info == null ? "--" : info.FileCount.ToString("N0", CultureInfo.CurrentCulture);
            foldersValue.Text = info == null ? "--" : info.DirectoryCount.ToString("N0", CultureInfo.CurrentCulture);
            bootValue.Text = info == null ? "--" : info.HasBootRecord ? "El Torito boot record present" : "No boot record";
            labelValue.Text = info == null ? "--" : info.HasIso9660 ? info.VolumeLabel ?? NotRecorded : "No ISO 9660 descriptor";
            jolietLabelValue.Text = info == null ? "--" : (info.HasJoliet ? (info.JolietVolumeLabel ?? NotRecorded) : "No Joliet descriptor") +
                                                          (info.HasUdf ? "    UDF: " + (info.UdfVolumeLabel ?? NotRecorded) : string.Empty);
            fileSystemValue.Text = info == null ? "--" : DescribeFileSystems(info);
            createdValue.Text = info == null ? "--" : FormatDate(info.VolumeCreationTime);
            modifiedValue.Text = info == null ? "--" : FormatDate(info.VolumeModificationTime);
            applicationValue.Text = info == null ? "--" : info.ApplicationIdentifier ?? NotRecorded;

            if (info == null)
            {
                statusLabel.Text = message;
                statusLabel.ForeColor = message != null && message.StartsWith("Reading", StringComparison.Ordinal) || message == "Choose an ISO file to inspect."
                    ? ThemeManager.Current.DimText
                    : ThemeManager.Current.ErrorText;
            }
            else if (info.Problems.Count == 0)
            {
                statusLabel.Text = "The image structure is valid. Dates are shown as recorded in the image (UTC offset applied).";
                statusLabel.ForeColor = ThemeManager.Current.DimText;
            }
            else
            {
                statusLabel.Text = info.Problems.Count + " structural problem(s): " + info.Problems[0];
                statusLabel.ForeColor = ThemeManager.Current.ErrorText;
            }

            toolTip.SetToolTip(statusLabel, info == null ? message : string.Join(Environment.NewLine, info.Problems));
            FillTree(info);
        }

        private void FillTree(IsoImageInfo info)
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            treeLabel.Text = "Directory structure";
            if (info != null)
            {
                string rootText = (info.DisplayLabel ?? Path.GetFileName(info.FilePath)) + " (root)";
                TreeNode root = treeView.Nodes.Add(rootText);
                var folders = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { { string.Empty, root } };
                int added = 0;
                IEnumerable<IsoImageEntry> ordered = info.Entries
                    .OrderBy(e => e.Path.Count(c => c == '\\'))
                    .ThenBy(e => e.IsDirectory ? 0 : 1)
                    .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase);
                foreach (IsoImageEntry entry in ordered)
                {
                    if (added >= MaxTreeNodes)
                    {
                        break;
                    }

                    int slash = entry.Path.LastIndexOf('\\');
                    string parentPath = slash < 0 ? string.Empty : entry.Path.Substring(0, slash);
                    string name = slash < 0 ? entry.Path : entry.Path.Substring(slash + 1);
                    TreeNode parent;
                    if (!folders.TryGetValue(parentPath, out parent))
                    {
                        continue;
                    }

                    TreeNode node = parent.Nodes.Add(entry.IsDirectory ? name : name + "  (" + FileSizeFormatter.Format(entry.Length) + ")");
                    node.ToolTipText = entry.Path + (entry.RecordedTime.HasValue
                        ? Environment.NewLine + "Recorded date: " + entry.RecordedTime.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                        : string.Empty);
                    if (entry.IsDirectory)
                    {
                        folders[entry.Path] = node;
                    }

                    added++;
                }

                root.Expand();
                treeLabel.Text = added < info.Entries.Count
                    ? "Directory structure (showing the first " + added.ToString("N0", CultureInfo.CurrentCulture) + " of " + info.Entries.Count.ToString("N0", CultureInfo.CurrentCulture) + " items)"
                    : "Directory structure (" + info.Entries.Count.ToString("N0", CultureInfo.CurrentCulture) + " items" + (info.HasUdf ? ", UDF names" : info.HasJoliet ? ", Joliet names" : string.Empty) + ")";
            }

            treeView.EndUpdate();
        }

        private static string FormatDate(DateTime? utc)
        {
            return utc.HasValue
                ? utc.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC (as recorded)"
                : NotRecorded;
        }

        private static string BuildText(IsoImageInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ISO file: " + info.FilePath);
            sb.AppendLine("File size: " + FileSizeFormatter.FormatWithBytes(info.FileLength));
            sb.AppendLine("Declared volume size: " + FileSizeFormatter.FormatWithBytes(info.DeclaredVolumeBytes));
            sb.AppendLine("Volume label (ISO 9660): " + (info.HasIso9660 ? info.VolumeLabel ?? NotRecorded : "No ISO 9660 descriptor"));
            sb.AppendLine("Volume label (Joliet): " + (info.HasJoliet ? info.JolietVolumeLabel ?? NotRecorded : "No Joliet descriptor"));
            sb.AppendLine("Volume label (UDF): " + (info.HasUdf ? info.UdfVolumeLabel ?? NotRecorded : "No UDF file system"));
            sb.AppendLine("File system: " + DescribeFileSystems(info));
            sb.AppendLine("Boot record: " + (info.HasBootRecord ? "El Torito boot record present" : "No boot record"));
            sb.AppendLine("Files: " + info.FileCount + ", folders: " + info.DirectoryCount);
            sb.AppendLine("Volume created: " + FormatDate(info.VolumeCreationTime));
            sb.AppendLine("Volume modified: " + FormatDate(info.VolumeModificationTime));
            sb.AppendLine("Application identifier: " + (info.ApplicationIdentifier ?? NotRecorded));
            sb.AppendLine("Structural problems: " + (info.Problems.Count == 0 ? "none" : string.Join("; ", info.Problems)));
            return sb.ToString();
        }

        /// <summary>The file systems found, as recorded (UDF revision from the logical volume descriptor).</summary>
        internal static string DescribeFileSystems(IsoImageInfo info)
        {
            if (!info.HasUdf)
            {
                return info.HasJoliet ? "ISO 9660 with Joliet extensions" : "ISO 9660 only";
            }

            var parts = new System.Collections.Generic.List<string>();
            if (info.HasIso9660)
            {
                parts.Add("ISO 9660");
            }

            if (info.HasJoliet)
            {
                parts.Add("Joliet");
            }

            if (info.HasUdf)
            {
                parts.Add("UDF" + (info.UdfRevision != null ? " " + info.UdfRevision : string.Empty));
            }

            return string.Join(" + ", parts) + (parts.Count == 1 ? " only" : string.Empty);
        }
    }
}
