using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HTISOCreator.Security
{
    /// <summary>
    /// Export Security Report: the installed version, standards version, applicable advisories, verified fixes and the
    /// assessment date, for organisations keeping audit evidence. Contains no device identifiers or personal data.
    /// </summary>
    public static class SecurityReport
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm 'UTC'zzz";

        public static string ToMarkdown(SecurityAdvisoryView v, DateTimeOffset generatedAt)
        {
            var sb = new StringBuilder();
            Action<string, string> row = (k, val) => sb.AppendLine("| " + k + " | " + Escape(val) + " |");

            sb.AppendLine("# Security Advisory Report: " + v.Application.Name);
            sb.AppendLine();
            sb.AppendLine("| Item | Value |");
            sb.AppendLine("|---|---|");
            row("Application", v.Application.Name + " (" + v.Application.AppId + ")");
            row("Installed version", v.Application.Version + (v.Application.BuildId == null ? string.Empty : " (build " + v.Application.BuildId + ")"));
            row("Release channel", v.Application.Channel);
            row("Platform", v.Application.Platform);
            row("HTREASURE standards version", StandardsRuntime.StandardsVersion);
            row("Report generated", generatedAt.ToString(DateFormat, CultureInfo.InvariantCulture));
            row("Advisory data source", v.SourceDescription);
            row("Advisory data date", v.DataGeneratedAt.HasValue ? v.DataGeneratedAt.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : "None");
            row("Last successful online check", v.LastSuccessfulCheck.HasValue ? v.LastSuccessfulCheck.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : "Never");
            row("Assessment", v.OverallAssessment);
            sb.AppendLine();
            if (v.IsHistorical)
            {
                sb.AppendLine("> **Historical data.** This report is based on advisory data that is not a current, live assessment. Check for advisories online for the latest information.");
                sb.AppendLine();
            }

            Section(sb, "Applicable and outstanding advisories", v.Outstanding.ToList());
            Section(sb, "Security fixes included in this version", v.FixesIncluded.ToList());

            sb.AppendLine("## Statement");
            sb.AppendLine();
            sb.AppendLine(SecurityAdvisoryView.PolicyStatement);
            sb.AppendLine();
            sb.AppendLine("HTREASURE LTD · https://htreasure.com · support@htreasure.com");
            return sb.ToString();
        }

        public static string ToJson(SecurityAdvisoryView v, DateTimeOffset generatedAt)
        {
            var w = new JsonTextWriter();
            w.BeginObject()
                .Value("reportType", "htreasure-security-advisory-report")
                .Value("generatedAt", generatedAt)
                .Value("standardsVersion", StandardsRuntime.StandardsVersion)
                .BeginObject("application")
                    .Value("appId", v.Application.AppId)
                    .Value("name", v.Application.Name)
                    .Value("version", v.Application.Version)
                    .Value("channel", v.Application.Channel)
                    .Value("platform", v.Application.Platform)
                    .Value("buildId", v.Application.BuildId)
                .EndObject()
                .Value("source", v.Source.ToString())
                .Value("sourceDescription", v.SourceDescription)
                .Value("dataGeneratedAt", v.DataGeneratedAt)
                .Value("lastSuccessfulCheck", v.LastSuccessfulCheck)
                .Value("historical", v.IsHistorical)
                .Value("assessment", v.OverallAssessment)
                .BeginArray("advisories");
            foreach (AdvisoryAssessment a in v.Assessments)
            {
                w.BeginObject()
                    .Value("id", a.Advisory.Id)
                    .BeginArray("aliases");
                foreach (AdvisoryAlias alias in a.Advisory.Aliases ?? new List<AdvisoryAlias>())
                {
                    w.Value(null, alias.Id);
                }

                w.EndArray()
                    .Value("severity", a.Advisory.Severity)
                    .Value("title", a.Advisory.Title)
                    .Value("affectedVersions", a.Advisory.AffectedVersionsText)
                    .BeginArray("fixedVersions");
                foreach (string f in a.Advisory.FixedVersions ?? new List<string>())
                {
                    w.Value(null, f);
                }

                w.EndArray()
                    .Value("published", a.Advisory.Published)
                    .Value("status", a.Status.ToString())
                    .Value("reason", a.Reason)
                    .EndObject();
            }

            w.EndArray()
                .Value("statement", SecurityAdvisoryView.PolicyStatement)
                .EndObject();
            return w.ToString() + "\n";
        }

        public static string StatusText(AdvisoryStatus s)
        {
            switch (s)
            {
                case AdvisoryStatus.UnderReview:
                    return "Under Review";
                case AdvisoryStatus.NotAffected:
                    return "Not Affected";
                default:
                    return s.ToString();
            }
        }

        public static string SeverityText(AdvisorySeverity s)
        {
            return s.ToString();
        }

        private static void Section(StringBuilder sb, string title, List<AdvisoryAssessment> items)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (items.Count == 0)
            {
                sb.AppendLine("None recorded in the advisory data used for this report.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Advisory | Severity | Title | Affected versions | Fixed version | Published | Status |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (AdvisoryAssessment a in items)
            {
                string ids = a.Advisory.Id + string.Concat((a.Advisory.Aliases ?? new List<AdvisoryAlias>()).Select(x => ", " + x.Id));
                sb.AppendLine("| " + Escape(ids) + " | " + Cap(a.Advisory.Severity) + " | " + Escape(a.Advisory.Title) + " | " +
                              Escape(a.Advisory.AffectedVersionsText) + " | " + Escape(a.Advisory.FixedVersionText) + " | " +
                              a.Advisory.Published + " | " + StatusText(a.Status) + " |");
            }

            sb.AppendLine();
        }

        private static string Cap(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string Escape(string s)
        {
            return (s ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
