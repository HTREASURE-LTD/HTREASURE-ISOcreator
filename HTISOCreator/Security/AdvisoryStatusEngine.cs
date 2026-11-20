using System;
using System.Collections.Generic;
using System.Linq;

namespace HTISOCreator.Security
{
    /// <summary>Result of assessing one advisory against one installation, with the reason shown to users.</summary>
    public sealed class AdvisoryAssessment
    {
        public AdvisoryAssessment(SecurityAdvisory advisory, AdvisoryStatus status, string reason)
        {
            Advisory = advisory;
            Status = status;
            Reason = reason;
        }

        public SecurityAdvisory Advisory { get; private set; }

        public AdvisoryStatus Status { get; private set; }

        public string Reason { get; private set; }
    }

    /// <summary>
    /// Decides whether an advisory applies to an installation (the HTREASURE status rules). It is deliberately
    /// conservative: only rules for the installation's own platform and channel count; a version is Fixed only when it
    /// is a listed fixed build, equals the fixed version, or lies between the fixed version and <c>assessedThrough</c>;
    /// Not Affected needs an explicit verified rule; missing data gives Unknown.
    /// </summary>
    public static class AdvisoryStatusEngine
    {
        private static readonly AdvisoryStatus[] OverallOrder =
            { AdvisoryStatus.Affected, AdvisoryStatus.UnderReview, AdvisoryStatus.Unknown, AdvisoryStatus.Fixed, AdvisoryStatus.NotAffected };

        private static readonly AdvisoryStatus[] EntryOrder =
            { AdvisoryStatus.Affected, AdvisoryStatus.Unknown, AdvisoryStatus.Fixed, AdvisoryStatus.NotAffected };

        public static AdvisoryAssessment Assess(SecurityAdvisory advisory, InstalledApplication app)
        {
            if (!string.Equals(advisory.AppId, app.AppId, StringComparison.Ordinal))
            {
                return new AdvisoryAssessment(advisory, AdvisoryStatus.NotAffected, "This advisory concerns a different application.");
            }

            List<AffectedEntry> matching = (advisory.Affected ?? new List<AffectedEntry>())
                .Where(e => Matches(e.Platforms, app.Platform) && Matches(e.Channels, app.Channel))
                .ToList();

            // An explicit, verified fixed build wins over everything else: this is how backports are recorded.
            if (matching.Any(e => IsListedFixedBuild(e, app)))
            {
                return advisory.VerificationStatus == "verified"
                    ? new AdvisoryAssessment(advisory, AdvisoryStatus.Fixed, "Build " + Describe(app) + " is verified to contain the fix.")
                    : new AdvisoryAssessment(advisory, AdvisoryStatus.UnderReview, "A fix is recorded for this build, but the advisory assessment is not complete.");
            }

            if (advisory.VerificationStatus == "under-review")
            {
                return new AdvisoryAssessment(advisory, AdvisoryStatus.UnderReview, "HTREASURE is still assessing whether this installation is affected.");
            }

            if (advisory.VerificationStatus != "verified")
            {
                return new AdvisoryAssessment(advisory, AdvisoryStatus.Unknown, "There is not enough verified information to decide whether this installation is affected.");
            }

            if (matching.Count == 0)
            {
                return new AdvisoryAssessment(advisory, AdvisoryStatus.Unknown,
                    "No verified assessment covers platform '" + app.Platform + "' on the '" + app.Channel + "' channel.");
            }

            SemVersion installed;
            if (!SemVersion.TryParse(app.Version, out installed))
            {
                return new AdvisoryAssessment(advisory, AdvisoryStatus.Unknown, "Installed version '" + app.Version + "' could not be read.");
            }

            List<KeyValuePair<AdvisoryStatus, string>> results = matching.Select(e => AssessEntry(e, installed)).ToList();

            // Worst outcome wins when several rules apply.
            foreach (AdvisoryStatus status in OverallOrder)
            {
                foreach (KeyValuePair<AdvisoryStatus, string> r in results)
                {
                    if (r.Key == status)
                    {
                        return new AdvisoryAssessment(advisory, status, r.Value);
                    }
                }
            }

            return new AdvisoryAssessment(advisory, AdvisoryStatus.Unknown, "The advisory rules could not be evaluated.");
        }

        public static IList<AdvisoryAssessment> AssessAll(IEnumerable<SecurityAdvisory> advisories, InstalledApplication app)
        {
            return advisories.Where(a => a.AppId == app.AppId)
                .Select(a => Assess(a, app))
                .OrderBy(r => Rank(r.Status))
                .ThenByDescending(r => r.Advisory.SeverityLevel)
                .ThenByDescending(r => r.Advisory.Published, StringComparer.Ordinal)
                .ToList();
        }

        public static int Rank(AdvisoryStatus status)
        {
            switch (status)
            {
                case AdvisoryStatus.Affected:
                    return 0;
                case AdvisoryStatus.UnderReview:
                    return 1;
                case AdvisoryStatus.Unknown:
                    return 2;
                case AdvisoryStatus.Fixed:
                    return 3;
                default:
                    return 4;
            }
        }

        private static KeyValuePair<AdvisoryStatus, string> AssessEntry(AffectedEntry entry, SemVersion v)
        {
            if (entry.Status == "not-affected")
            {
                return Pair(AdvisoryStatus.NotAffected, "A verified assessment confirms this platform and channel are not affected.");
            }

            SemVersion assessedThrough = null;
            if (entry.AssessedThrough != null && !SemVersion.TryParse(entry.AssessedThrough, out assessedThrough))
            {
                return Pair(AdvisoryStatus.Unknown, "The advisory's assessment range is invalid.");
            }

            var outcomes = new List<KeyValuePair<AdvisoryStatus, string>>();
            foreach (VersionRange r in entry.Ranges ?? new List<VersionRange>())
            {
                SemVersion intro;
                if (!SemVersion.TryParse(r.Introduced, out intro))
                {
                    outcomes.Add(Pair(AdvisoryStatus.Unknown, "The advisory's version range is invalid."));
                    continue;
                }

                if (v < intro)
                {
                    outcomes.Add(Pair(AdvisoryStatus.NotAffected, "The vulnerability was introduced in " + r.Introduced + ", after the installed version."));
                    continue;
                }

                if (r.Fixed != null)
                {
                    SemVersion fixedV;
                    if (!SemVersion.TryParse(r.Fixed, out fixedV))
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Unknown, "The advisory's fixed version is invalid."));
                    }
                    else if (v < fixedV)
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Affected, "Versions from " + r.Introduced + " up to, but not including, " + r.Fixed + " are affected."));
                    }
                    else if (v == fixedV)
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Fixed, "The fix was released in " + r.Fixed + "."));
                    }
                    else if (assessedThrough != null && v <= assessedThrough)
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Fixed, "Fixed in " + r.Fixed + " and verified as still fixed through " + entry.AssessedThrough + "."));
                    }
                    else
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Unknown,
                            "Fixed in " + r.Fixed + ", but no verified assessment yet confirms the fix is carried into this newer version."));
                    }

                    continue;
                }

                if (r.LastAffected != null)
                {
                    SemVersion last;
                    if (!SemVersion.TryParse(r.LastAffected, out last))
                    {
                        outcomes.Add(Pair(AdvisoryStatus.Unknown, "The advisory's version range is invalid."));
                    }
                    else
                    {
                        outcomes.Add(v <= last
                            ? Pair(AdvisoryStatus.Affected, "Versions " + r.Introduced + " to " + r.LastAffected + " are affected.")
                            : Pair(AdvisoryStatus.Unknown, "No verified fix is recorded after " + r.LastAffected + "."));
                    }

                    continue;
                }

                outcomes.Add(Pair(AdvisoryStatus.Affected, "All versions from " + r.Introduced + " are affected and no fix is available yet."));
            }

            foreach (AdvisoryStatus status in EntryOrder)
            {
                foreach (KeyValuePair<AdvisoryStatus, string> o in outcomes)
                {
                    if (o.Key == status)
                    {
                        return o;
                    }
                }
            }

            return Pair(AdvisoryStatus.Unknown, "No version range applies.");
        }

        private static KeyValuePair<AdvisoryStatus, string> Pair(AdvisoryStatus status, string reason)
        {
            return new KeyValuePair<AdvisoryStatus, string>(status, reason);
        }

        private static bool Matches(IList<string> selectors, string value)
        {
            return selectors != null && selectors.Any(s => s == "*" || string.Equals(s, value, StringComparison.Ordinal));
        }

        private static bool IsListedFixedBuild(AffectedEntry entry, InstalledApplication app)
        {
            return entry.FixedBuilds != null && entry.FixedBuilds.Count > 0 &&
                   entry.FixedBuilds.Any(b => string.Equals(b, app.Version, StringComparison.Ordinal) ||
                                              (app.BuildId != null && string.Equals(b, app.BuildId, StringComparison.Ordinal)));
        }

        private static string Describe(InstalledApplication app)
        {
            return app.BuildId == null ? app.Version : app.Version + " (" + app.BuildId + ")";
        }
    }
}
