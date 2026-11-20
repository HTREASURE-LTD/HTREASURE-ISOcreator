using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HTISOCreator.Security
{
    public enum FeedProblem
    {
        None,
        SignatureMissing,
        SignatureInvalid,
        UnknownSigningKey,
        Malformed,
        SchemaInvalid,
        Expired,
        WrongKind,
        TooLarge
    }

    public sealed class FeedReadResult
    {
        public FeedReadResult(AdvisoryFeed feed, FeedProblem problem, string message)
        {
            Feed = feed;
            Problem = problem;
            Message = message;
        }

        public AdvisoryFeed Feed { get; private set; }

        public FeedProblem Problem { get; private set; }

        public string Message { get; private set; }

        public bool Ok
        {
            get { return Problem == FeedProblem.None && Feed != null; }
        }
    }

    /// <summary>
    /// Turns raw feed bytes into a trusted <see cref="AdvisoryFeed"/>: authenticate first, then parse, then validate.
    /// Advisory metadata is data only. Nothing in it is ever executed, opened or installed.
    /// </summary>
    public static class FeedReader
    {
        public static FeedReadResult Read(byte[] content, byte[] signatureJson, TrustedKeySet trusted,
            ICollection<string> allowedKinds, DateTimeOffset now, bool requireSignature = true, bool enforceExpiry = true)
        {
            if (content.Length > StrictJson.MaxBytes)
            {
                return Fail(FeedProblem.TooLarge, "The advisory data is larger than allowed.");
            }

            if (requireSignature || signatureJson != null)
            {
                switch (FeedSignature.Verify(content, signatureJson, trusted))
                {
                    case SignatureCheck.Valid:
                        break;
                    case SignatureCheck.Missing:
                        return Fail(FeedProblem.SignatureMissing, "The advisory data is not signed.");
                    case SignatureCheck.UnknownKey:
                        return Fail(FeedProblem.UnknownSigningKey, "The advisory data is signed by a key this application does not trust.");
                    case SignatureCheck.Malformed:
                        return Fail(FeedProblem.SignatureInvalid, "The advisory signature is malformed.");
                    default:
                        return Fail(FeedProblem.SignatureInvalid, "The advisory signature does not match; the data may have been altered.");
                }
            }

            AdvisoryFeed feed;
            try
            {
                feed = StrictJson.ReadFeed(content);
            }
            catch (AdvisoryFormatException ex)
            {
                return Fail(FeedProblem.Malformed, "The advisory data is not valid: " + ex.Message);
            }

            IList<string> errors = AdvisoryValidator.Validate(feed);
            if (errors.Count > 0)
            {
                return Fail(FeedProblem.SchemaInvalid, "The advisory data failed validation: " + string.Join(" ", errors.Take(5)));
            }

            if (!allowedKinds.Contains(feed.Kind))
            {
                return Fail(FeedProblem.WrongKind, "Unexpected advisory data kind '" + feed.Kind + "'.");
            }

            if (enforceExpiry && feed.ExpiresAt.HasValue && feed.ExpiresAt.Value <= now)
            {
                return Fail(FeedProblem.Expired, "The advisory data expired on " + feed.ExpiresAt.Value.ToString("u") + "; it may be a replay of old data.");
            }

            return new FeedReadResult(feed, FeedProblem.None, "Verified.");
        }

        private static FeedReadResult Fail(FeedProblem problem, string message)
        {
            return new FeedReadResult(null, problem, message);
        }
    }

    public enum AdvisorySource
    {
        /// <summary>No verified advisory data could be loaded.</summary>
        None,

        /// <summary>Snapshot bundled with the installed release. Historical, never a current assessment.</summary>
        BundledSnapshot,

        /// <summary>Live feed verified recently and not expired (not available: no advisory service is configured).</summary>
        LiveVerified,

        /// <summary>Live feed verified earlier, now past its validity window.</summary>
        LiveCachedHistorical,

        /// <summary>Test fixtures. Shown with a prominent test-data label.</summary>
        TestFixture
    }

    public sealed class SecurityAdvisoryOptions
    {
        public SecurityAdvisoryOptions()
        {
            Clock = () => DateTimeOffset.UtcNow;
        }

        /// <summary>Folder that holds the bundled advisory-snapshot.json (normally &lt;program folder&gt;\security).</summary>
        public string BundledDirectory { get; set; }

        public TrustedKeySet TrustedKeys { get; set; }

        /// <summary>HTTPS URL of the signed live advisory feed. Empty: the advisory service is not configured for this product.</summary>
        public string AdvisoryServiceUrl { get; set; }

        /// <summary>Development builds only: accept a bundled snapshot without a signature. Release builds leave this false.</summary>
        public bool AllowUnsignedBundledSnapshot { get; set; }

        /// <summary>Accept test-fixture feeds (tests and screenshots). Never enabled in the program.</summary>
        public bool AllowTestFixtures { get; set; }

        public Func<DateTimeOffset> Clock { get; set; }
    }

    /// <summary>Everything the Security Advisory History window needs.</summary>
    public sealed class SecurityAdvisoryView
    {
        public const string PolicyStatement = "A lack of known advisories is not proof that the software is free from vulnerabilities.";

        public SecurityAdvisoryView()
        {
            Assessments = new List<AdvisoryAssessment>();
            Warnings = new List<string>();
        }

        public InstalledApplication Application { get; set; }

        public AdvisorySource Source { get; set; }

        public string SourceDescription { get; set; }

        public DateTimeOffset? DataGeneratedAt { get; set; }

        public DateTimeOffset? BundledSnapshotDate { get; set; }

        public DateTimeOffset? LastSuccessfulCheck { get; set; }

        public string LastCheckError { get; set; }

        public bool AdvisoryServiceConfigured { get; set; }

        public IList<AdvisoryAssessment> Assessments { get; set; }

        public IList<string> Warnings { get; set; }

        /// <summary>True when the data must not be read as a current security assessment.</summary>
        public bool IsHistorical
        {
            get { return Source != AdvisorySource.LiveVerified; }
        }

        public IEnumerable<AdvisoryAssessment> Outstanding
        {
            get { return Assessments.Where(a => a.Status == AdvisoryStatus.Affected || a.Status == AdvisoryStatus.UnderReview || a.Status == AdvisoryStatus.Unknown); }
        }

        public IEnumerable<AdvisoryAssessment> FixesIncluded
        {
            get { return Assessments.Where(a => a.Status == AdvisoryStatus.Fixed); }
        }

        /// <summary>Headline assessment. Never claims the software is secure or free of vulnerabilities.</summary>
        public string OverallAssessment
        {
            get
            {
                if (Source == AdvisorySource.None)
                {
                    return "Unknown";
                }

                if (Assessments.Any(a => a.Status == AdvisoryStatus.Affected))
                {
                    return "Action required";
                }

                return Assessments.Any(a => a.Status == AdvisoryStatus.UnderReview || a.Status == AdvisoryStatus.Unknown)
                    ? "Review pending"
                    : "No known applicable advisories";
            }
        }
    }

    /// <summary>
    /// Entry point for the Security Advisory History window. Works offline from the snapshot bundled with the release.
    /// The online part of the standard (signed live feed with rollback protection) needs the HTREASURE advisory
    /// service, which has not been provisioned; until it is, checking online reports that it is not configured,
    /// and the program never connects to the internet for advisories.
    /// </summary>
    public sealed class SecurityAdvisoryCenter
    {
        public const string SnapshotFile = "advisory-snapshot.json";
        public const string NotConfiguredMessage = "The advisory service is not configured for this product.";

        private readonly InstalledApplication _app;
        private readonly SecurityAdvisoryOptions _options;
        private string _lastCheckError;

        public SecurityAdvisoryCenter(InstalledApplication app, SecurityAdvisoryOptions options)
        {
            _app = app;
            _options = options;
        }

        public InstalledApplication Application
        {
            get { return _app; }
        }

        public bool AdvisoryServiceConfigured
        {
            get { return !string.IsNullOrEmpty(_options.AdvisoryServiceUrl); }
        }

        public static string SignatureFileFor(string file)
        {
            return file + ".sig.json";
        }

        /// <summary>Builds the view from local data only. Never touches the network.</summary>
        public SecurityAdvisoryView LoadOffline()
        {
            DateTimeOffset now = _options.Clock();
            var warnings = new List<string>();
            FeedReadResult bundled = ReadBundled(now, warnings);
            if (bundled != null)
            {
                bool fixture = bundled.Feed.Kind == FeedKinds.TestFixture;
                return new SecurityAdvisoryView
                {
                    Application = _app,
                    Source = fixture ? AdvisorySource.TestFixture : AdvisorySource.BundledSnapshot,
                    SourceDescription = fixture
                        ? "TEST FIXTURE DATA - not real security information"
                        : "Advisory snapshot bundled with this release (" + bundled.Feed.GeneratedAt.ToLocalTime().ToString("d MMM yyyy") + ")",
                    DataGeneratedAt = bundled.Feed.GeneratedAt,
                    BundledSnapshotDate = bundled.Feed.GeneratedAt,
                    LastCheckError = _lastCheckError,
                    AdvisoryServiceConfigured = AdvisoryServiceConfigured,
                    Assessments = AdvisoryStatusEngine.AssessAll(bundled.Feed.Advisories, _app),
                    Warnings = warnings
                };
            }

            return new SecurityAdvisoryView
            {
                Application = _app,
                Source = AdvisorySource.None,
                SourceDescription = "No verified advisory data is available.",
                LastCheckError = _lastCheckError,
                AdvisoryServiceConfigured = AdvisoryServiceConfigured,
                Warnings = warnings
            };
        }

        /// <summary>
        /// Checks the HTREASURE advisory service. No service has been provisioned for HT ISO Creator, so this reports
        /// that and keeps the bundled data. It never installs anything.
        /// </summary>
        public SecurityAdvisoryView CheckOnline(out string message)
        {
            message = AdvisoryServiceConfigured
                ? "Online advisory checks are not available in this version of HT ISO Creator. Showing the bundled advisory history."
                : NotConfiguredMessage;
            _lastCheckError = message;
            return LoadOffline();
        }

        private FeedReadResult ReadBundled(DateTimeOffset now, List<string> warnings)
        {
            string path = Path.Combine(_options.BundledDirectory ?? string.Empty, SnapshotFile);
            byte[] content;
            byte[] signature = null;
            try
            {
                if (!File.Exists(path))
                {
                    warnings.Add("No advisory snapshot is bundled with this installation.");
                    return null;
                }

                content = File.ReadAllBytes(path);
                string sigPath = SignatureFileFor(path);
                if (File.Exists(sigPath))
                {
                    signature = File.ReadAllBytes(sigPath);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                warnings.Add("The bundled advisory snapshot could not be read: " + ex.Message);
                return null;
            }

            string[] kinds = _options.AllowTestFixtures
                ? new[] { FeedKinds.BundledSnapshot, FeedKinds.TestFixture }
                : new[] { FeedKinds.BundledSnapshot };
            FeedReadResult r;
            try
            {
                r = FeedReader.Read(content, signature, _options.TrustedKeys, kinds, now,
                    requireSignature: !_options.AllowUnsignedBundledSnapshot, enforceExpiry: false);
            }
            catch (CryptographicException ex)
            {
                warnings.Add("The bundled advisory snapshot could not be verified: " + ex.Message);
                return null;
            }

            if (!r.Ok)
            {
                warnings.Add("The bundled advisory snapshot could not be verified: " + r.Message);
                return null;
            }

            if (signature == null)
            {
                warnings.Add("Development build: the bundled advisory snapshot is not signed.");
            }

            if (r.Feed.Scope != _app.AppId && r.Feed.Scope != "all")
            {
                warnings.Add("The bundled advisory snapshot belongs to a different application.");
                return null;
            }

            return r;
        }
    }
}
