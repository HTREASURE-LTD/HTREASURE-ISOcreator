using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HTISOCreator.Security
{
    /// <summary>
    /// Validates advisories and feeds against advisory.schema.json, plus the semantic rules a JSON schema cannot
    /// express (date order, range order, unique identifiers). Every advisory passes through here before it is shown.
    /// </summary>
    public static class AdvisoryValidator
    {
        private static readonly Regex IdPattern = new Regex(@"^HTSA-\d{4}-\d{4}$", RegexOptions.CultureInvariant);
        private static readonly Regex KebabPattern = new Regex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
        private static readonly Regex SelectorPattern = new Regex(@"^(\*|[a-z0-9]+(-[a-z0-9]+)*)$", RegexOptions.CultureInvariant);
        private static readonly Regex CvePattern = new Regex(@"^CVE-\d{4}-\d{4,}$", RegexOptions.CultureInvariant);
        private static readonly Regex GhsaPattern = new Regex(@"^GHSA(-[23456789cfghjmpqrvwx]{4}){3}$", RegexOptions.CultureInvariant);
        private static readonly Regex CvssPattern = new Regex(@"^CVSS:(3\.1|4\.0)/[A-Za-z:/]+$", RegexOptions.CultureInvariant);

        private static readonly HashSet<string> Severities = new HashSet<string> { "critical", "high", "medium", "low" };
        private static readonly HashSet<string> Verifications = new HashSet<string> { "verified", "under-review", "unverified" };
        private static readonly HashSet<string> ReferenceTypes = new HashSet<string> { "advisory", "release-notes", "cve", "ghsa", "web" };

        public static IList<string> Validate(SecurityAdvisory a)
        {
            var e = new List<string>();
            Action<bool, string> req = (ok, msg) =>
            {
                if (!ok)
                {
                    e.Add(a.Id + ": " + msg);
                }
            };

            req(a.SchemaVersion == "1.0.0", "schemaVersion must be 1.0.0.");
            req(a.Id != null && IdPattern.IsMatch(a.Id), "id must match HTSA-YYYY-NNNN.");
            req(a.AppId != null && a.AppId.Length <= 64 && KebabPattern.IsMatch(a.AppId), "appId must be lower-case kebab case.");
            req(a.Title != null && a.Title.Length >= 4 && a.Title.Length <= 160, "title must be 4-160 characters.");
            req(a.Summary != null && a.Summary.Length >= 10 && a.Summary.Length <= 20000, "summary must be 10-20000 characters.");
            req(a.Severity != null && Severities.Contains(a.Severity), "severity must be critical, high, medium or low.");
            req(a.VerificationStatus != null && Verifications.Contains(a.VerificationStatus), "verificationStatus is invalid.");
            req(a.Remediation != null && a.Remediation.Length >= 4 && a.Remediation.Length <= 10000, "remediation is required.");
            req(a.Mitigation == null || a.Mitigation.Length <= 10000, "mitigation is too long.");
            req(a.ReleaseNotes == null || a.ReleaseNotes.Length <= 20000, "releaseNotes is too long.");

            DateTime pub, upd;
            bool pubOk = DateTime.TryParseExact(a.Published, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out pub);
            bool updOk = DateTime.TryParseExact(a.Updated, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out upd);
            req(pubOk, "published must be a yyyy-MM-dd date.");
            req(updOk, "updated must be a yyyy-MM-dd date.");
            if (pubOk && updOk)
            {
                req(upd >= pub, "updated must not be earlier than published.");
            }

            if (pubOk && a.Id != null && IdPattern.IsMatch(a.Id))
            {
                req(int.Parse(a.Id.Substring(5, 4), CultureInfo.InvariantCulture) <= pub.Year + 1, "id year is later than the publication year.");
            }

            if (a.Cvss != null)
            {
                CvssInfo c = a.Cvss;
                req(c.Version == "3.1" || c.Version == "4.0", "cvss.version must be 3.1 or 4.0.");
                req(c.Vector != null && c.Vector.Length <= 200 && CvssPattern.IsMatch(c.Vector) && c.Vector.StartsWith("CVSS:" + c.Version + "/", StringComparison.Ordinal),
                    "cvss.vector is invalid or does not match cvss.version.");
                req(c.BaseScore >= 0 && c.BaseScore <= 10, "cvss.baseScore must be 0-10.");
            }

            foreach (AdvisoryAlias al in a.Aliases ?? new List<AdvisoryAlias>())
            {
                req(al.Verified, "alias " + al.Id + " is not verified; only verified external identifiers may be listed.");
                bool valid = al.Id != null && ((al.Type == "CVE" && CvePattern.IsMatch(al.Id)) || (al.Type == "GHSA" && GhsaPattern.IsMatch(al.Id)));
                req(valid, "alias " + al.Id + " is not a valid " + al.Type + " identifier.");
            }

            req((a.Aliases == null ? 0 : a.Aliases.Count) <= 20, "too many aliases.");

            foreach (AdvisoryReference r in a.References ?? new List<AdvisoryReference>())
            {
                Uri u;
                req(ReferenceTypes.Contains(r.Type ?? string.Empty), "reference type '" + r.Type + "' is invalid.");
                req(r.Url != null && Uri.TryCreate(r.Url, UriKind.Absolute, out u) && u.Scheme == Uri.UriSchemeHttps && r.Url.Length <= 500,
                    "reference '" + r.Url + "' must be an absolute HTTPS URL.");
            }

            req(a.Affected != null && a.Affected.Count >= 1 && a.Affected.Count <= 50, "affected must contain 1-50 entries.");
            foreach (AffectedEntry entry in a.Affected ?? new List<AffectedEntry>())
            {
                req(entry.Platforms != null && entry.Platforms.Count > 0 && entry.Platforms.All(p => p != null && SelectorPattern.IsMatch(p)), "affected.platforms is invalid.");
                req(entry.Channels != null && entry.Channels.Count > 0 && entry.Channels.All(p => p != null && SelectorPattern.IsMatch(p)), "affected.channels is invalid.");
                req(entry.Status == "affected" || entry.Status == "not-affected", "affected.status must be affected or not-affected.");
                if (entry.Status == "affected")
                {
                    req(entry.Ranges != null && entry.Ranges.Count > 0, "an affected entry needs at least one version range.");
                }

                SemVersion ignored;
                if (entry.AssessedThrough != null)
                {
                    req(SemVersion.TryParse(entry.AssessedThrough, out ignored), "assessedThrough is not a semantic version.");
                }

                foreach (VersionRange r in entry.Ranges ?? new List<VersionRange>())
                {
                    SemVersion intro;
                    bool introOk = SemVersion.TryParse(r.Introduced, out intro);
                    req(introOk, "range introduced '" + r.Introduced + "' is not a semantic version.");
                    req(!(r.Fixed != null && r.LastAffected != null), "a range may set fixed or lastAffected, not both.");
                    if (r.Fixed != null)
                    {
                        SemVersion fx;
                        req(SemVersion.TryParse(r.Fixed, out fx) && intro != null && fx > intro, "range fixed must be a version later than introduced.");
                    }

                    if (r.LastAffected != null)
                    {
                        SemVersion la;
                        req(SemVersion.TryParse(r.LastAffected, out la) && intro != null && la >= intro, "range lastAffected must not be earlier than introduced.");
                    }
                }
            }

            return e;
        }

        public static IList<string> Validate(AdvisoryFeed feed)
        {
            var e = new List<string>();
            if (feed.FeedType != "htreasure-security-advisories")
            {
                e.Add("feedType is invalid.");
            }

            if (feed.SchemaVersion != "1.0.0")
            {
                e.Add("feed schemaVersion must be 1.0.0.");
            }

            if (feed.Kind != FeedKinds.Live && feed.Kind != FeedKinds.BundledSnapshot && feed.Kind != FeedKinds.TestFixture)
            {
                e.Add("feed kind is invalid.");
            }

            if (feed.Sequence < 1)
            {
                e.Add("feed sequence must be positive.");
            }

            if (feed.Kind == FeedKinds.Live && feed.ExpiresAt == null)
            {
                e.Add("a live feed must have expiresAt.");
            }

            if (feed.ExpiresAt.HasValue && feed.ExpiresAt.Value <= feed.GeneratedAt)
            {
                e.Add("expiresAt must be later than generatedAt.");
            }

            if (string.IsNullOrWhiteSpace(feed.Scope))
            {
                e.Add("scope is required.");
            }

            if (feed.Advisories == null)
            {
                e.Add("advisories is required.");
                return e;
            }

            foreach (IGrouping<string, SecurityAdvisory> dup in feed.Advisories.GroupBy(a => a.Id).Where(g => g.Count() > 1))
            {
                e.Add("duplicate advisory id " + dup.Key + ".");
            }

            if (feed.Scope != "all")
            {
                foreach (SecurityAdvisory a in feed.Advisories.Where(a => a.AppId != feed.Scope))
                {
                    e.Add(a.Id + ": appId " + a.AppId + " is outside the feed scope " + feed.Scope + ".");
                }
            }

            foreach (SecurityAdvisory a in feed.Advisories)
            {
                e.AddRange(Validate(a));
            }

            return e;
        }
    }
}
