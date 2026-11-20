using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Security;
using HTISOCreator.Services;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Help → About → Security Advisory History (and Help → Security Advisories): the mandatory HTREASURE component.
    /// Opens instantly from local data, so it works offline. A banner always says what kind of data is shown, and the
    /// window never presents a lack of known advisories as proof that the software is free from vulnerabilities.
    /// </summary>
    public sealed class SecurityAdvisoryHistoryForm : Form
    {
        private readonly SecurityAdvisoryCenter _center;
        private readonly Panel _banner = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(20, 0, 20, 0), Name = "bannerPanel" };
        private readonly Label _bannerText = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Name = "bannerLabel" };
        private readonly TableLayoutPanel _cards = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 2, Height = 172, Padding = new Padding(14, 10, 14, 2), BackColor = BrandColors.PageGrey, Name = "cardsPanel" };
        private readonly ListView _list = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false,
            BorderStyle = BorderStyle.None, HeaderStyle = ColumnHeaderStyle.Nonclickable, Name = "advisoryListView",
            AccessibleName = "Advisory history"
        };

        private readonly Panel _emptyState = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false, Name = "emptyStatePanel" };
        private readonly Label _listTitle = new Label { Dock = DockStyle.Top, Height = 32, ForeColor = BrandColors.DeepNavy, TextAlign = ContentAlignment.BottomLeft, Name = "listTitleLabel" };
        private readonly Label _warnings = new Label { AutoSize = true, ForeColor = BrandColors.Amber, Margin = new Padding(0, 4, 0, 0), Name = "warningsLabel" };
        private readonly Label _status = new Label { AutoSize = true, ForeColor = BrandColors.MutedText, Margin = new Padding(0, 6, 0, 0), Name = "statusLabel" };
        private readonly Button _checkAdvisories = Button("Check for Advisories", "checkAdvisoriesButton");
        private readonly Button _export = Button("Export Security Report...", "exportReportButton");
        private readonly Button _details = Button("View Details", "viewDetailsButton");
        private readonly Button _checkUpdates = Button("Check for Security Updates", "checkUpdatesButton");
        private readonly Button _close = Button("Close", "closeButton");
        private readonly ToolTip _toolTip = new ToolTip();
        private SecurityAdvisoryView _view;

        public SecurityAdvisoryHistoryForm(SecurityAdvisoryCenter center)
        {
            _center = center;
            Text = "Security Advisory History - " + center.Application.Name;
            Name = "SecurityAdvisoryHistoryForm";
            Icon = AppIcon.Icon;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(960, 640);
            MinimumSize = new Size(820, 560);
            BackColor = BrandColors.PageGrey;
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;

            _bannerText.Font = new Font(Font, FontStyle.Bold);
            _listTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _warnings.Font = new Font("Segoe UI", 8.25F);

            for (int i = 0; i < 4; i++)
            {
                _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            _list.Columns.Add("Advisory ID", 140);
            _list.Columns.Add("Severity", 75);
            _list.Columns.Add("Title", 250);
            _list.Columns.Add("Affected versions", 140);
            _list.Columns.Add("Fixed version", 100);
            _list.Columns.Add("Published", 85);
            _list.Columns.Add("Status", 100);
            _list.DoubleClick += (s, e) => ShowDetails();
            _list.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ShowDetails();
                }
            };
            _list.SelectedIndexChanged += (s, e) => _details.Enabled = _list.SelectedItems.Count > 0;

            BuildEmptyState();

            var listCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 0, 14, 10) };
            listCard.Controls.Add(_list);
            listCard.Controls.Add(_emptyState);
            listCard.Controls.Add(_listTitle);
            var listHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 6), BackColor = BrandColors.PageGrey };
            listHost.Controls.Add(listCard);

            var policy = new Label
            {
                Text = SecurityAdvisoryView.PolicyStatement, AutoSize = true, ForeColor = BrandColors.MutedText,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Italic), Margin = new Padding(0, 2, 0, 0), Name = "policyLabel"
            };
            var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            left.Controls.Add(policy);
            left.Controls.Add(_status);
            left.Controls.Add(_warnings);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Name = "buttonsPanel" };
            buttons.Controls.AddRange(new Control[] { _close, _checkUpdates, _details, _export, _checkAdvisories });
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 112, Padding = new Padding(20, 6, 20, 12), BackColor = BrandColors.PageGrey, Name = "footerPanel" };
            footer.Controls.Add(left);
            footer.Controls.Add(buttons);

            _banner.Controls.Add(_bannerText);

            // Dock order: the last control added is docked first (topmost).
            Controls.Add(listHost);
            Controls.Add(footer);
            Controls.Add(_cards);
            Controls.Add(_banner);
            Controls.Add(Header("Security Advisory History", center.Application.Name + " · " + center.Application.Version));

            _checkAdvisories.Click += (s, e) => CheckAdvisories();
            _export.Click += (s, e) => ExportReport();
            _details.Click += (s, e) => ShowDetails();
            _close.Click += (s, e) => Close();
            _checkUpdates.Enabled = false;
            _toolTip.SetToolTip(_checkUpdates, "The HTREASURE software update service is not configured for HT ISO Creator.");
            _toolTip.SetToolTip(_checkAdvisories, "Checks the HTREASURE advisory service. It never installs anything.");
            _details.Enabled = false;
            CancelButton = _close;

            _view = center.LoadOffline();
            Render();
        }

        /// <summary>The data currently shown (for tests).</summary>
        internal SecurityAdvisoryView CurrentView
        {
            get { return _view; }
        }

        internal string BannerText
        {
            get { return _bannerText.Text; }
        }

        internal static string Format(DateTimeOffset? d)
        {
            return d.HasValue ? d.Value.ToLocalTime().ToString("d MMM yyyy HH:mm", CultureInfo.CurrentCulture) : "Never";
        }

        internal static string FormatDate(DateTimeOffset? d)
        {
            return d.HasValue ? d.Value.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture) : "Unknown";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Button Button(string text, string name)
        {
            return new Button
            {
                Text = text, Name = name, AutoSize = true, MinimumSize = new Size(90, 30), Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(6, 0, 0, 0), UseVisualStyleBackColor = true
            };
        }

        private static Control Header(string title, string subtitle)
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BrandColors.DeepNavy, Name = "headerPanel" };
            var t = new Label
            {
                Text = title, AutoSize = true, ForeColor = Color.White, BackColor = BrandColors.DeepNavy,
                Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold), Location = new Point(18, 6)
            };
            var s = new Label { Text = subtitle, AutoSize = true, ForeColor = BrandColors.SilverGrey, BackColor = BrandColors.DeepNavy, Location = new Point(20, 34) };
            var accent = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = BrandColors.ElectricBlue };
            header.Controls.Add(t);
            header.Controls.Add(s);
            header.Controls.Add(accent);
            return header;
        }

        private void BuildEmptyState()
        {
            var title = new Label
            {
                Text = "No verified advisory records loaded", AutoSize = true, ForeColor = BrandColors.DeepNavy,
                Font = new Font("Segoe UI", 11.25F, FontStyle.Bold), Location = new Point(6, 30), Name = "emptyTitleLabel"
            };
            var body = new Label
            {
                AutoSize = true, MaximumSize = new Size(760, 0), ForeColor = BrandColors.MutedText, Location = new Point(6, 60), Name = "emptyBodyLabel",
                Text = "Security advisories and previously fixed vulnerabilities will appear here when verified records are available. " +
                       "An empty list is not proof that this software is free from vulnerabilities."
            };
            _emptyState.Controls.Add(title);
            _emptyState.Controls.Add(body);
        }

        private void Render()
        {
            InstalledApplication app = _view.Application;
            switch (_view.Source)
            {
                case AdvisorySource.LiveVerified:
                    SetBanner(BrandColors.BlueBackground, BrandColors.TechnologyBlue,
                        "Live advisory information verified " + Format(_view.LastSuccessfulCheck ?? _view.DataGeneratedAt) + ".");
                    break;
                case AdvisorySource.TestFixture:
                    SetBanner(BrandColors.RedBackground, BrandColors.Red, "TEST FIXTURE DATA - illustrative records for testing only. Not real security information.");
                    break;
                case AdvisorySource.BundledSnapshot:
                    SetBanner(BrandColors.AmberBackground, BrandColors.Amber,
                        "Offline history: snapshot bundled with this release on " + FormatDate(_view.DataGeneratedAt) +
                        ". This is historical information, not a current security assessment.");
                    break;
                case AdvisorySource.LiveCachedHistorical:
                    SetBanner(BrandColors.AmberBackground, BrandColors.Amber,
                        "Showing advisory data last verified on " + FormatDate(_view.DataGeneratedAt) + ". It may be out of date; check for advisories when online.");
                    break;
                default:
                    SetBanner(BrandColors.AmberBackground, BrandColors.Amber, "No verified advisory data is available. The security status of this installation is Unknown.");
                    break;
            }

            List<AdvisoryAssessment> outstanding = _view.Outstanding.ToList();
            int affected = outstanding.Count(a => a.Status == AdvisoryStatus.Affected);
            _cards.SuspendLayout();
            foreach (Control c in _cards.Controls.Cast<Control>().ToList())
            {
                c.Dispose();
            }

            _cards.Controls.Clear();
            _cards.Controls.Add(Card("Installed version", app.Version, app.Channel + " channel · " + app.Platform, null), 0, 0);
            _cards.Controls.Add(Card("Advisory assessment", _view.OverallAssessment, AssessmentNote(), AssessmentColour()), 1, 0);
            _cards.Controls.Add(Card("Last advisory check", _view.LastSuccessfulCheck.HasValue ? Format(_view.LastSuccessfulCheck) : "Never",
                _view.LastCheckError ?? (_view.AdvisoryServiceConfigured ? "HTREASURE advisory service" : "Advisory service not configured"), null), 2, 0);
            _cards.Controls.Add(Card("Advisory data source", SourceTitle(), _view.SourceDescription, null), 3, 0);
            _cards.Controls.Add(Card("Security fixes included", _view.FixesIncluded.Count().ToString(CultureInfo.CurrentCulture), "Verified as corrected in this version", BrandColors.Green), 0, 1);
            _cards.Controls.Add(Card("Outstanding advisories", outstanding.Count.ToString(CultureInfo.CurrentCulture),
                outstanding.Count == 0 ? "None known in this data" : affected + " affected · " + (outstanding.Count - affected) + " under review or unknown",
                affected > 0 ? BrandColors.Red : outstanding.Count > 0 ? (Color?)BrandColors.Amber : null), 1, 1);
            _cards.Controls.Add(Card("Security update availability", "Not checked", "Update service not configured", null), 2, 1);
            _cards.Controls.Add(Card("Bundled snapshot", _view.BundledSnapshotDate.HasValue ? FormatDate(_view.BundledSnapshotDate) : "None", "Available offline", null), 3, 1);
            _cards.ResumeLayout();

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (AdvisoryAssessment a in _view.Assessments)
            {
                SecurityAdvisory adv = a.Advisory;
                string ids = adv.Id + (adv.Aliases != null && adv.Aliases.Count > 0 ? " / " + string.Join(", ", adv.Aliases.Select(x => x.Id)) : string.Empty);
                var item = new ListViewItem(ids) { Tag = a, UseItemStyleForSubItems = false };
                item.SubItems.Add(SecurityReport.SeverityText(adv.SeverityLevel), Color.White, BrandColors.For(adv.SeverityLevel), new Font(_list.Font, FontStyle.Bold));
                item.SubItems.Add(adv.Title);
                item.SubItems.Add(adv.AffectedVersionsText);
                item.SubItems.Add(adv.FixedVersionText);
                item.SubItems.Add(adv.Published);
                item.SubItems.Add(SecurityReport.StatusText(a.Status), Color.White, BrandColors.For(a.Status), new Font(_list.Font, FontStyle.Bold));
                _list.Items.Add(item);
            }

            _list.EndUpdate();
            _listTitle.Text = _view.Assessments.Count == 0 ? "Advisory history" : "Advisory history (" + _view.Assessments.Count + ")";
            bool empty = _view.Assessments.Count == 0;
            _emptyState.Visible = empty;
            _list.Visible = !empty;
            if (_list.Items.Count > 0)
            {
                _list.Items[0].Selected = true;
            }

            _warnings.Text = string.Join("  ", _view.Warnings.Distinct());
            _toolTip.SetToolTip(_warnings, _warnings.Text);
        }

        private void SetBanner(Color back, Color fore, string text)
        {
            _banner.BackColor = back;
            _bannerText.ForeColor = fore;
            _bannerText.Text = text;
        }

        private string SourceTitle()
        {
            switch (_view.Source)
            {
                case AdvisorySource.LiveVerified:
                    return "Live (verified)";
                case AdvisorySource.LiveCachedHistorical:
                    return "Cached (historical)";
                case AdvisorySource.BundledSnapshot:
                    return "Bundled snapshot";
                case AdvisorySource.TestFixture:
                    return "Test fixtures";
                default:
                    return "None";
            }
        }

        private string AssessmentNote()
        {
            return _view.Source == AdvisorySource.None ? "No verified advisory data"
                : _view.IsHistorical ? "Based on historical data" : "Based on live verified data";
        }

        private Color? AssessmentColour()
        {
            switch (_view.OverallAssessment)
            {
                case "Action required":
                    return BrandColors.Red;
                case "Review pending":
                case "Unknown":
                    return BrandColors.Amber;
                default:
                    return null;
            }
        }

        private Control Card(string label, string value, string note, Color? accent)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(5), Padding = new Padding(0, 6, 6, 2) };
            var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 0, 0) };
            var l = new Label { Text = label.ToUpperInvariant(), Dock = DockStyle.Top, Height = 16, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = BrandColors.MutedText };
            var v = new Label
            {
                Text = value, Dock = DockStyle.Top, Height = 26, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", value.Length > 20 ? 10F : 12F, FontStyle.Bold), ForeColor = accent ?? BrandColors.DeepNavy
            };
            var n = new Label { Text = note, Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 8.25F), ForeColor = BrandColors.MutedText };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent ?? BrandColors.ElectricBlue };
            inner.Controls.Add(n);
            inner.Controls.Add(v);
            inner.Controls.Add(l);
            p.Controls.Add(inner);
            p.Controls.Add(bar);
            p.AccessibleName = label + ": " + value + ". " + note;
            _toolTip.SetToolTip(v, value);
            _toolTip.SetToolTip(n, note);
            return p;
        }

        private void ShowDetails()
        {
            if (_list.SelectedItems.Count == 0)
            {
                return;
            }

            var a = _list.SelectedItems[0].Tag as AdvisoryAssessment;
            if (a == null)
            {
                return;
            }

            using (var dlg = new AdvisoryDetailsForm(a, _view))
            {
                dlg.ShowDialog(this);
            }
        }

        private void CheckAdvisories()
        {
            string message;
            _view = _center.CheckOnline(out message);
            Render();
            _status.Text = message;
        }

        private void ExportReport()
        {
            using (var save = new SaveFileDialog
            {
                Title = "Export Security Report",
                Filter = "Markdown report (*.md)|*.md|JSON report (*.json)|*.json",
                FileName = _view.Application.AppId + "-" + _view.Application.Version + "-security-report-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".md",
                OverwritePrompt = true,
                AddExtension = true
            })
            {
                if (save.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    DateTimeOffset now = DateTimeOffset.Now;
                    File.WriteAllText(save.FileName, save.FilterIndex == 2 ? SecurityReport.ToJson(_view, now) : SecurityReport.ToMarkdown(_view, now));
                    _status.Text = "Security report saved to " + save.FileName;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    LoggingService.Instance.Error("Could not save the security report", ex);
                    MessageBox.Show(this, ex.Message, "Cannot save report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
