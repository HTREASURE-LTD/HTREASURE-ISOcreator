using System;
using System.Collections.Generic;
using System.Linq;

// .NET Framework 4.6.2 port of the offline part of HTREASURE.Standards.Security
// (HTREASURE-LTD/ht-software-standards, standards version 1.0.0). The data model, status rules,
// validation and signature format are the same; only the language level and JSON library differ.
namespace HTISOCreator.Security
{
    public enum AdvisorySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Applicability of one advisory to one installation. See <see cref="AdvisoryStatusEngine"/>.</summary>
    public enum AdvisoryStatus
    {
        /// <summary>Verified as corrected in the installed version.</summary>
        Fixed,

        /// <summary>The installed version falls inside a verified affected range.</summary>
        Affected,

        /// <summary>May be affected; the assessment is not complete.</summary>
        UnderReview,

        /// <summary>Not enough verified information to decide.</summary>
        Unknown,

        /// <summary>A verified assessment confirms the installation is not affected.</summary>
        NotAffected
    }

    public sealed class AdvisoryAlias
    {
        public string Type { get; set; }

        public string Id { get; set; }

        public bool Verified { get; set; }
    }

    public sealed class CvssInfo
    {
        public string Version { get; set; }

        public string Vector { get; set; }

        public double BaseScore { get; set; }
    }

    public sealed class AdvisoryReference
    {
        public string Type { get; set; }

        public string Url { get; set; }
    }

    public sealed class VersionRange
    {
        public string Introduced { get; set; }

        public string Fixed { get; set; }

        public string LastAffected { get; set; }
    }

    public sealed class AffectedEntry
    {
        public IList<string> Platforms { get; set; }

        public IList<string> Channels { get; set; }

        public string Status { get; set; }

        public IList<VersionRange> Ranges { get; set; }

        public IList<string> FixedBuilds { get; set; }

        public string AssessedThrough { get; set; }
    }

    /// <summary>One advisory, as defined by security/schemas/advisory.schema.json.</summary>
    public sealed class SecurityAdvisory
    {
        public SecurityAdvisory()
        {
            SchemaVersion = "1.0.0";
        }

        public string SchemaVersion { get; set; }

        public string Id { get; set; }

        public string AppId { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public string Severity { get; set; }

        public CvssInfo Cvss { get; set; }

        public IList<AdvisoryAlias> Aliases { get; set; }

        public string Published { get; set; }

        public string Updated { get; set; }

        public string VerificationStatus { get; set; }

        public IList<AffectedEntry> Affected { get; set; }

        public IList<string> FixedVersions { get; set; }

        public string Remediation { get; set; }

        public string Mitigation { get; set; }

        public IList<AdvisoryReference> References { get; set; }

        public string ReleaseNotes { get; set; }

        public AdvisorySeverity SeverityLevel
        {
            get
            {
                switch (Severity)
                {
                    case "critical":
                        return AdvisorySeverity.Critical;
                    case "high":
                        return AdvisorySeverity.High;
                    case "medium":
                        return AdvisorySeverity.Medium;
                    default:
                        return AdvisorySeverity.Low;
                }
            }
        }

        /// <summary>Readable summary of the affected ranges, for tables.</summary>
        public string AffectedVersionsText
        {
            get
            {
                var parts = new List<string>();
                foreach (AffectedEntry entry in (Affected ?? new List<AffectedEntry>()).Where(a => a.Status == "affected"))
                {
                    foreach (VersionRange r in entry.Ranges ?? new List<VersionRange>())
                    {
                        parts.Add(r.Fixed != null ? ">= " + r.Introduced + ", < " + r.Fixed
                            : r.LastAffected != null ? r.Introduced + " to " + r.LastAffected
                            : ">= " + r.Introduced);
                    }
                }

                return parts.Count == 0 ? "Not specified" : string.Join("; ", parts.Distinct());
            }
        }

        public string FixedVersionText
        {
            get { return FixedVersions != null && FixedVersions.Count > 0 ? string.Join(", ", FixedVersions) : "No verified fix"; }
        }
    }

    public static class FeedKinds
    {
        public const string Live = "live";
        public const string BundledSnapshot = "bundled-snapshot";
        public const string TestFixture = "test-fixture";
    }

    /// <summary>A signed collection of advisories (security/schemas/advisory-feed.schema.json).</summary>
    public sealed class AdvisoryFeed
    {
        public AdvisoryFeed()
        {
            FeedType = "htreasure-security-advisories";
            SchemaVersion = "1.0.0";
        }

        public string FeedType { get; set; }

        public string SchemaVersion { get; set; }

        public string Kind { get; set; }

        public long Sequence { get; set; }

        public DateTimeOffset GeneratedAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public string Scope { get; set; }

        public IList<SecurityAdvisory> Advisories { get; set; }
    }

    /// <summary>Detached signature (security/schemas/signature.schema.json).</summary>
    public sealed class DetachedSignature
    {
        public string Algorithm { get; set; }

        public string KeyId { get; set; }

        public string Signature { get; set; }
    }

    /// <summary>Identity of the running installation. Collected locally only; never transmitted.</summary>
    public sealed class InstalledApplication
    {
        public InstalledApplication(string appId, string name, string version, string channel, string platform, string buildId = null)
        {
            AppId = appId;
            Name = name;
            Version = version;
            Channel = channel;
            Platform = platform;
            BuildId = string.IsNullOrEmpty(buildId) ? null : buildId;
        }

        public string AppId { get; private set; }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public string Channel { get; private set; }

        public string Platform { get; private set; }

        public string BuildId { get; private set; }
    }
}
