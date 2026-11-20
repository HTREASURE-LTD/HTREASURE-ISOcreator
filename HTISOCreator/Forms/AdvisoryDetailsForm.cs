using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Security;

namespace HTISOCreator.Forms
{
    /// <summary>
    /// Full details of one advisory. Advisory text is shown as read-only text and is never executed; an external
    /// reference opens only if it is HTTPS and only after the user confirms.
    /// </summary>
    public sealed class AdvisoryDetailsForm : Form
    {
        private readonly RichTextBox _body;

        public AdvisoryDetailsForm(AdvisoryAssessment assessment, SecurityAdvisoryView view)
        {
            SecurityAdvisory a = assessment.Advisory;
            Text = a.Id + " - Advisory details";
            Name = "AdvisoryDetailsForm";
            Icon = AppIcon.Icon;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(760, 600);
            MinimumSize = new Size(600, 440);
            BackColor = BrandColors.PageGrey;
            ShowInTaskbar = false;
            MinimizeBox = false;

            var header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BrandColors.DeepNavy };
            header.Controls.Add(new Label { Text = a.Id, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold), Location = new Point(18, 6) });
            header.Controls.Add(new Label { Text = a.Title, AutoSize = true, ForeColor = BrandColors.SilverGrey, Location = new Point(20, 34) });

            var badges = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(18, 8, 18, 0), BackColor = Color.White, WrapContents = false };
            badges.Controls.Add(Badge("Severity: " + SecurityReport.SeverityText(a.SeverityLevel), BrandColors.For(a.SeverityLevel)));
            badges.Controls.Add(Badge("Status: " + SecurityReport.StatusText(assessment.Status), BrandColors.For(assessment.Status)));
            if (view.Source == AdvisorySource.TestFixture)
            {
                badges.Controls.Add(Badge("Test fixture data", BrandColors.Slate));
            }
            else if (view.IsHistorical)
            {
                badges.Controls.Add(Badge("Historical data", BrandColors.Amber));
            }

            _body = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White,
                DetectUrls = false, Name = "detailsTextBox", AccessibleName = "Advisory details"
            };
            Render(BuildText(assessment, view));
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 8, 10, 8), BackColor = Color.White };
            body.Controls.Add(_body);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(14, 10, 14, 8), BackColor = BrandColors.PageGrey };
            var close = new Button { Text = "Close", AutoSize = true, MinimumSize = new Size(90, 30), Name = "closeButton", UseVisualStyleBackColor = true };
            close.Click += (s, e) => Close();
            buttons.Controls.Add(close);
            CancelButton = close;
            if (a.References != null && a.References.Count > 0)
            {
                var open = new Button { Text = "Open reference...", AutoSize = true, MinimumSize = new Size(90, 30), Name = "openReferenceButton", UseVisualStyleBackColor = true };
                var menu = new ContextMenuStrip();
                foreach (AdvisoryReference r in a.References)
                {
                    string url = r.Url;
                    menu.Items.Add(r.Type + ": " + url, null, (s, e) => OpenExternalReference(this, url));
                }

                open.Click += (s, e) => menu.Show(open, new Point(0, open.Height));
                buttons.Controls.Add(open);
            }

            Controls.Add(body);
            Controls.Add(buttons);
            Controls.Add(badges);
            Controls.Add(header);
        }

        /// <summary>The advisory as text with "## " headings, the same content as the standards' details view.</summary>
        internal static string BuildText(AdvisoryAssessment assessment, SecurityAdvisoryView view)
        {
            SecurityAdvisory a = assessment.Advisory;
            var sb = new StringBuilder();
            sb.AppendLine("Advisory ID: " + a.Id);
            if (a.Aliases != null && a.Aliases.Count > 0)
            {
                sb.AppendLine("External references: " + string.Join(", ", a.Aliases.Select(x => x.Id)));
            }

            sb.AppendLine("Severity: " + SecurityReport.SeverityText(a.SeverityLevel) +
                          (a.Cvss != null ? " (CVSS " + a.Cvss.Version + " " + a.Cvss.BaseScore.ToString("0.0", CultureInfo.InvariantCulture) + ")" : string.Empty));
            sb.AppendLine("Affected versions: " + a.AffectedVersionsText);
            sb.AppendLine("Fixed version: " + a.FixedVersionText);
            sb.AppendLine("Published: " + a.Published);
            sb.AppendLine("Last updated: " + a.Updated);
            sb.AppendLine("Status: " + SecurityReport.StatusText(assessment.Status));
            sb.AppendLine("Installed: " + view.Application.Version + " (" + view.Application.Channel + ", " + view.Application.Platform + ")");
            sb.AppendLine();
            sb.AppendLine("## Why this status");
            sb.AppendLine(assessment.Reason);
            if (view.IsHistorical)
            {
                sb.AppendLine();
                sb.AppendLine("This status is based on historical advisory data and is not a current security assessment.");
            }

            sb.AppendLine();
            sb.AppendLine("## Details");
            sb.AppendLine(a.Summary);
            sb.AppendLine();
            sb.AppendLine("## Remediation");
            sb.AppendLine(a.Remediation);
            if (!string.IsNullOrWhiteSpace(a.Mitigation))
            {
                sb.AppendLine();
                sb.AppendLine("## Mitigation");
                sb.AppendLine(a.Mitigation);
            }

            if (!string.IsNullOrWhiteSpace(a.ReleaseNotes))
            {
                sb.AppendLine();
                sb.AppendLine("## Release notes");
                sb.AppendLine(a.ReleaseNotes);
            }

            if (a.References != null && a.References.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## References");
                foreach (AdvisoryReference r in a.References)
                {
                    sb.AppendLine("- " + r.Type + ": " + r.Url);
                }
            }

            return sb.ToString();
        }

        private static void OpenExternalReference(IWin32Window owner, string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                MessageBox.Show(owner, "Only HTTPS references can be opened.", "Reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(owner, "Open this external reference in your web browser?\n\n" + uri.AbsoluteUri, "Open reference",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }))
                {
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                MessageBox.Show(owner, ex.Message, "Cannot open reference", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label Badge(string text, Color colour)
        {
            return new Label
            {
                Text = text.ToUpperInvariant(), AutoSize = true, ForeColor = Color.White, BackColor = colour,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), Padding = new Padding(6, 3, 6, 3), Margin = new Padding(0, 0, 8, 0)
            };
        }

        // Headings ("## ") in bold navy, Markdown emphasis markers removed; everything else as plain text.
        private void Render(string text)
        {
            using (var heading = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            {
                foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
                {
                    string line = raw.Replace("**", string.Empty);
                    bool isHeading = line.StartsWith("#", StringComparison.Ordinal);
                    _body.SelectionStart = _body.TextLength;
                    _body.SelectionFont = isHeading ? heading : _body.Font;
                    _body.SelectionColor = isHeading ? BrandColors.DeepNavy : Color.Black;
                    _body.AppendText((isHeading ? line.TrimStart('#', ' ') : line) + "\n");
                }
            }

            _body.SelectionStart = 0;
        }
    }
}
