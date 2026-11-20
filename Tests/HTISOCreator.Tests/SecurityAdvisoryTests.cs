using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Security;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// The .NET Framework port of the HTREASURE Security Advisory History component (ht-software-standards 1.0.0):
    /// conformance with the standards' fixtures, interoperability with snapshots signed by the standards' own tool,
    /// strict parsing, offline behaviour, reports and the windows that show them.
    /// </summary>
    [TestClass]
    public class SecurityAdvisoryTests
    {
        private static readonly string Fixtures = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StandardsFixtures");
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public void ConformanceCases_FromTheStandards_AllPass()
        {
            Dictionary<string, SecurityAdvisory> advisories = Directory.GetFiles(Path.Combine(Fixtures, "advisories"), "*.json")
                .Select(f => StrictJson.ReadAdvisory(File.ReadAllBytes(f)))
                .ToDictionary(a => a.Id);
            Assert.AreEqual(7, advisories.Count);
            foreach (SecurityAdvisory a in advisories.Values)
            {
                Assert.AreEqual(0, AdvisoryValidator.Validate(a).Count, a.Id + ": " + string.Join(" ", AdvisoryValidator.Validate(a)));
            }

            var doc = (IDictionary<string, object>)StrictJson.Parse(File.ReadAllBytes(Path.Combine(Fixtures, "expected-statuses.json")));
            var cases = ((IEnumerable<object>)doc["cases"]).Cast<IDictionary<string, object>>().ToList();
            Assert.AreEqual(21, cases.Count, "The standards define 21 conformance cases");
            var failures = new List<string>();
            foreach (IDictionary<string, object> c in cases)
            {
                var i = (IDictionary<string, object>)c["installed"];
                object buildId;
                i.TryGetValue("buildId", out buildId);
                var app = new InstalledApplication((string)i["appId"], (string)i["name"], (string)i["version"], (string)i["channel"], (string)i["platform"], buildId as string);
                AdvisoryAssessment result = AdvisoryStatusEngine.Assess(advisories[(string)c["advisory"]], app);
                if (result.Status.ToString() != (string)c["expected"])
                {
                    failures.Add(c["name"] + ": expected " + c["expected"] + ", got " + result.Status + " (" + result.Reason + ")");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        }

        [TestMethod]
        public void InvalidFixtures_AreRejected()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(Fixtures, "invalid"), "*.json"))
            {
                bool rejected;
                try
                {
                    rejected = AdvisoryValidator.Validate(StrictJson.ReadAdvisory(File.ReadAllBytes(file))).Count > 0;
                }
                catch (AdvisoryFormatException)
                {
                    rejected = true;
                }

                Assert.IsTrue(rejected, Path.GetFileName(file) + " must be rejected");
            }

            // HTSA-0000-0903 smuggles in an unknown "runScript" member: strict reading refuses it outright.
            var ex = Assert.ThrowsException<AdvisoryFormatException>(() => StrictJson.ReadAdvisory(File.ReadAllBytes(Path.Combine(Fixtures, "invalid", "HTSA-0000-0903.json"))));
            StringAssert.Contains(ex.Message, "runScript");
        }

        [TestMethod]
        public void SnapshotSignedByTheStandardsTool_IsVerified_AndTamperingIsDetected()
        {
            byte[] content = File.ReadAllBytes(Path.Combine(Fixtures, "interop", "advisory-snapshot.json"));
            byte[] signature = File.ReadAllBytes(Path.Combine(Fixtures, "interop", "advisory-snapshot.json.sig.json"));
            TrustedKeySet keys = TrustedKeySet.Load(File.ReadAllBytes(Path.Combine(Fixtures, "interop", "test-trusted-keys.json")));
            string[] kinds = { FeedKinds.TestFixture };

            FeedReadResult ok = FeedReader.Read(content, signature, keys, kinds, Now);
            Assert.IsTrue(ok.Ok, ok.Message);
            Assert.AreEqual(7, ok.Feed.Advisories.Count);
            Assert.AreEqual(7L, ok.Feed.Sequence);
            Assert.AreEqual("ht-fixture-app", ok.Feed.Scope);

            byte[] tampered = (byte[])content.Clone();
            int at = Encoding.UTF8.GetString(content).IndexOf("\"high\"", StringComparison.Ordinal) + 1;
            tampered[at] = (byte)'l';
            Assert.AreEqual(FeedProblem.SignatureInvalid, FeedReader.Read(tampered, signature, keys, kinds, Now).Problem);
            Assert.AreEqual(FeedProblem.UnknownSigningKey, FeedReader.Read(content, signature, new TrustedKeySet(new KeyValuePair<string, string>[0]), kinds, Now).Problem);
            Assert.AreEqual(FeedProblem.SignatureMissing, FeedReader.Read(content, null, keys, kinds, Now).Problem);
            Assert.AreEqual(FeedProblem.WrongKind, FeedReader.Read(content, signature, keys, new[] { FeedKinds.BundledSnapshot }, Now).Problem);
        }

        [TestMethod]
        public void CngSignatures_RoundTrip_AndMalformedSignaturesAreRejected()
        {
            byte[] content = Encoding.UTF8.GetBytes("{\"x\":1}");
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport }))
            using (var ecdsa = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha256 })
            {
                var keys = new TrustedKeySet(new[] { new KeyValuePair<string, string>("k1", FeedSignature.SpkiFromCngKey(key)) });
                Assert.AreEqual(SignatureCheck.Valid, FeedSignature.Verify(content, SignatureJson("k1", ecdsa.SignData(content)), keys));
                Assert.AreEqual(SignatureCheck.Invalid, FeedSignature.Verify(Encoding.UTF8.GetBytes("{\"x\":2}"), SignatureJson("k1", ecdsa.SignData(content)), keys));
                Assert.AreEqual(SignatureCheck.Malformed, FeedSignature.Verify(content, SignatureJson("k1", new byte[10]), keys));
                Assert.AreEqual(SignatureCheck.Malformed, FeedSignature.Verify(content, Encoding.UTF8.GetBytes("{\"algorithm\":\"RSA\",\"keyId\":\"k1\",\"signature\":\"AA==\"}"), keys));
                Assert.AreEqual(SignatureCheck.UnknownKey, FeedSignature.Verify(content, SignatureJson("other", ecdsa.SignData(content)), keys));
                Assert.AreEqual(SignatureCheck.Missing, FeedSignature.Verify(content, null, keys));
            }

            Assert.ThrowsException<CryptographicException>(() => new TrustedKeySet(new[] { new KeyValuePair<string, string>("bad", Convert.ToBase64String(new byte[91])) }));
        }

        [TestMethod]
        public void StrictJson_RejectsWhatTheStandardsReaderRejects()
        {
            Func<string, bool> rejected = text =>
            {
                try
                {
                    StrictJson.ReadFeed(Encoding.UTF8.GetBytes(text));
                    return false;
                }
                catch (AdvisoryFormatException)
                {
                    return true;
                }
            };
            const string Valid = "{\"feedType\":\"htreasure-security-advisories\",\"schemaVersion\":\"1.0.0\",\"kind\":\"bundled-snapshot\",\"sequence\":1,\"generatedAt\":\"2026-11-20T15:57:46.0026288+00:00\",\"scope\":\"ht-iso-creator\",\"advisories\":[]}";
            Assert.IsFalse(rejected(Valid));
            Assert.IsTrue(rejected(Valid.Replace("\"scope\"", "\"extra\":1,\"scope\"")), "unknown member");
            Assert.IsTrue(rejected(Valid.Replace("[]}", "[],}")), "trailing comma");
            Assert.IsTrue(rejected("/* c */" + Valid), "comment");
            Assert.IsTrue(rejected(Valid.Replace("\"scope\":\"ht-iso-creator\"", "\"scope\":'ht-iso-creator'")), "single quotes");
            Assert.IsTrue(rejected(Valid.Replace("\"sequence\":1,", string.Empty)), "missing required member");
            Assert.IsTrue(rejected(Valid.Replace("\"sequence\":1", "\"sequence\":\"1\"")), "wrong type");
            Assert.IsTrue(rejected("[1,2]"), "not an object");
            Assert.IsFalse(rejected(Valid.Replace("\"advisories\":[]", "\"advisories\":[],\"expiresAt\":null")), "null optional member");

            // Strings may contain characters that would be syntax outside them.
            Assert.IsNull(StrictJson.ForbiddenSyntax("{\"a\":\"/* not a comment, ']' \\\" , }\"}"));
        }

        [TestMethod]
        public void SemanticVersions_FollowSemVer2Precedence()
        {
            string[] ordered = { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "1.0.2", "1.10.0" };
            for (int i = 1; i < ordered.Length; i++)
            {
                Assert.IsTrue(SemVersion.Parse(ordered[i - 1]) < SemVersion.Parse(ordered[i]), ordered[i - 1] + " < " + ordered[i]);
            }

            Assert.AreEqual(SemVersion.Parse("1.1.5+backport.1"), SemVersion.Parse("1.1.5"), "Build metadata is ignored for precedence");
            SemVersion ignored;
            Assert.IsFalse(SemVersion.TryParse("1.0", out ignored));
            Assert.IsFalse(SemVersion.TryParse("01.0.0", out ignored));
        }

        [TestMethod]
        public void OfflineCentre_HandlesMissingUnsignedForeignAndFixtureSnapshots()
        {
            string dir = NewFolder();
            try
            {
                var app = new InstalledApplication("ht-iso-creator", "HT ISO Creator", "1.0.2", "stable", "windows-x64");
                TrustedKeySet none = new TrustedKeySet(new KeyValuePair<string, string>[0]);

                // Nothing bundled: status Unknown, and the window says so.
                SecurityAdvisoryView view = Centre(app, dir, none, false, false).LoadOffline();
                Assert.AreEqual(AdvisorySource.None, view.Source);
                Assert.AreEqual("Unknown", view.OverallAssessment);
                Assert.IsTrue(view.Warnings.Any(w => w.Contains("No advisory snapshot is bundled")));

                // An unsigned snapshot (as the standards tool makes without a key) is accepted only by builds that allow it.
                File.WriteAllText(Path.Combine(dir, "advisory-snapshot.json"),
                    "{\"feedType\":\"htreasure-security-advisories\",\"schemaVersion\":\"1.0.0\",\"kind\":\"bundled-snapshot\",\"sequence\":1,\"generatedAt\":\"2026-11-20T15:57:46+00:00\",\"scope\":\"ht-iso-creator\",\"advisories\":[]}");
                view = Centre(app, dir, none, false, false).LoadOffline();
                Assert.AreEqual(AdvisorySource.None, view.Source, "A release build refuses an unsigned snapshot");
                Assert.IsTrue(view.Warnings.Any(w => w.Contains("not signed")));
                view = Centre(app, dir, none, true, false).LoadOffline();
                Assert.AreEqual(AdvisorySource.BundledSnapshot, view.Source);
                Assert.IsTrue(view.IsHistorical);
                Assert.AreEqual("No known applicable advisories", view.OverallAssessment, "Never 'secure' or 'no vulnerabilities'");
                Assert.IsTrue(view.Warnings.Any(w => w.StartsWith("Development build", StringComparison.Ordinal)));

                // A signed snapshot for another application is not shown.
                CopyInterop(dir);
                TrustedKeySet keys = TrustedKeySet.Load(File.ReadAllBytes(Path.Combine(Fixtures, "interop", "test-trusted-keys.json")));
                view = Centre(app, dir, keys, false, true).LoadOffline();
                Assert.AreEqual(AdvisorySource.None, view.Source);
                Assert.IsTrue(view.Warnings.Any(w => w.Contains("different application")));

                // Test fixtures are refused unless explicitly allowed (never in the program).
                var fixtureApp = new InstalledApplication("ht-fixture-app", "HT Fixture App", "1.1.5", "stable", "windows-x64");
                Assert.AreEqual(AdvisorySource.None, Centre(fixtureApp, dir, keys, false, false).LoadOffline().Source);
                view = Centre(fixtureApp, dir, keys, false, true).LoadOffline();
                Assert.AreEqual(AdvisorySource.TestFixture, view.Source);
                Assert.AreEqual(7, view.Assessments.Count);
                Assert.AreEqual("Action required", view.OverallAssessment);
                Assert.AreEqual(AdvisoryStatus.Affected, view.Assessments[0].Status, "Affected advisories are listed first");

                // No advisory service is configured: checking online says so and changes nothing.
                string message;
                SecurityAdvisoryView after = Centre(fixtureApp, dir, keys, false, true).CheckOnline(out message);
                Assert.AreEqual(SecurityAdvisoryCenter.NotConfiguredMessage, message);
                Assert.AreEqual(message, after.LastCheckError);
                Assert.AreEqual(7, after.Assessments.Count);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [TestMethod]
        public void SecurityReport_StatesThePolicy_AndContainsNoDeviceIdentifiers()
        {
            string dir = NewFolder();
            try
            {
                CopyInterop(dir);
                var app = new InstalledApplication("ht-fixture-app", "HT Fixture App", "1.1.5", "stable", "windows-x64");
                SecurityAdvisoryView view = Centre(app, dir, TrustedKeySet.Load(File.ReadAllBytes(Path.Combine(Fixtures, "interop", "test-trusted-keys.json"))), false, true).LoadOffline();

                string md = SecurityReport.ToMarkdown(view, Now);
                StringAssert.Contains(md, SecurityAdvisoryView.PolicyStatement);
                StringAssert.Contains(md, "**Historical data.**");
                StringAssert.Contains(md, "| HTREASURE standards version | 1.0.0 |");
                StringAssert.Contains(md, "HTSA-0000-0001");
                string json = SecurityReport.ToJson(view, Now);
                var parsed = (IDictionary<string, object>)StrictJson.Parse(Encoding.UTF8.GetBytes(json));
                Assert.AreEqual("htreasure-security-advisory-report", parsed["reportType"]);
                Assert.AreEqual(7, ((object[])parsed["advisories"]).Length);
                foreach (string report in new[] { md, json })
                {
                    Assert.IsFalse(report.Contains(Environment.MachineName), "No computer name");
                    Assert.IsFalse(report.Contains(Environment.UserName), "No user name");
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [TestMethod]
        public void Program_IdentifiesItselfToTheStandards_AndEmbedsTheRegistryKeys()
        {
            Assert.AreEqual("ht-iso-creator", StandardsRuntime.AppId);
            Assert.AreEqual("stable", StandardsRuntime.Channel);
            Assert.IsNull(StandardsRuntime.AdvisoryServiceUrl, "No advisory service is configured");
            CollectionAssert.Contains(new[] { "windows-x86", "windows-x64", "windows-arm64" }, StandardsRuntime.Platform);
            Assert.AreEqual(StandardsRuntime.Version, StandardsRuntime.Application.Version);
            Assert.AreEqual(0, StandardsRuntime.LoadTrustedKeys().Count, "The registry has no production key yet; the program trusts exactly its keys");

            string copyright = typeof(StandardsRuntime).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            StringAssert.Matches(copyright, new System.Text.RegularExpressions.Regex(@"^Copyright © 20\d\d HTREASURE LTD\. All rights reserved\.$"));
        }

        [TestMethod]
        public void HelpMenu_HasSecurityAdvisories_AndAboutSubmenu_AndTheWindowShowsHonestEmptyState()
        {
            using (var folder = new TestFolder())
            {
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder))
                    {
                        var help = (ToolStripMenuItem)form.MainMenuStrip.Items.Cast<ToolStripItem>().Single(i => i.Name == "helpMenu");
                        var items = help.DropDownItems.OfType<ToolStripMenuItem>().ToList();
                        Assert.AreEqual("Security &Advisories", items.Single(i => i.Name == "securityAdvisoriesMenuItem").Text);
                        ToolStripMenuItem about = items.Single(i => i.Name == "aboutMenuItem");
                        Assert.AreEqual("&About", about.Text);
                        CollectionAssert.AreEqual(new[] { "aboutAppMenuItem", "securityHistoryMenuItem" },
                            about.DropDownItems.OfType<ToolStripMenuItem>().Select(i => i.Name).ToArray());
                        Assert.AreEqual("Security Advisory &History", about.DropDownItems.OfType<ToolStripMenuItem>().Last().Text);
                    }

                    string dir = NewFolder();
                    try
                    {
                        var app = new InstalledApplication("ht-iso-creator", "HT ISO Creator", "1.0.2", "stable", "windows-x64");
                        using (var history = new SecurityAdvisoryHistoryForm(Centre(app, dir, new TrustedKeySet(new KeyValuePair<string, string>[0]), false, false))
                        { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) })
                        {
                            history.Show();
                            Application.DoEvents();
                            StringAssert.Contains(history.BannerText, "security status of this installation is Unknown");
                            Assert.IsTrue(UiStateTests.Find<Panel>(history, "emptyStatePanel").Visible);
                            Assert.AreEqual(SecurityAdvisoryView.PolicyStatement, UiStateTests.Find<Label>(history, "policyLabel").Text);
                            Assert.IsFalse(UiStateTests.Find<Button>(history, "checkUpdatesButton").Enabled, "No update service is configured");
                            Assert.IsTrue(UiStateTests.Find<Button>(history, "exportReportButton").Enabled);
                            UiStateTests.Find<Button>(history, "checkAdvisoriesButton").PerformClick();
                            Assert.AreEqual(SecurityAdvisoryCenter.NotConfiguredMessage, UiStateTests.Find<Label>(history, "statusLabel").Text);
                            SaveScreenshot(history, "security-advisory-history-empty.png");
                        }

                        CopyInterop(dir);
                        var fixtureApp = new InstalledApplication("ht-fixture-app", "HT Fixture App", "1.1.5", "stable", "windows-x64");
                        TrustedKeySet keys = TrustedKeySet.Load(File.ReadAllBytes(Path.Combine(Fixtures, "interop", "test-trusted-keys.json")));
                        using (var history = new SecurityAdvisoryHistoryForm(Centre(fixtureApp, dir, keys, false, true))
                        { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) })
                        {
                            history.Show();
                            Application.DoEvents();
                            StringAssert.StartsWith(history.BannerText, "TEST FIXTURE DATA");
                            Assert.AreEqual(7, UiStateTests.Find<ListView>(history, "advisoryListView").Items.Count);
                            Assert.IsFalse(UiStateTests.Find<Panel>(history, "emptyStatePanel").Visible);
                            SaveScreenshot(history, "security-advisory-history-fixtures.png");

                            AdvisoryAssessment first = history.CurrentView.Assessments[0];
                            string details = AdvisoryDetailsForm.BuildText(first, history.CurrentView);
                            StringAssert.Contains(details, "## Why this status");
                            StringAssert.Contains(details, first.Reason);
                            using (var dlg = new AdvisoryDetailsForm(first, history.CurrentView) { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) })
                            {
                                dlg.Show();
                                Application.DoEvents();
                                Assert.IsFalse(UiStateTests.Find<RichTextBox>(dlg, "detailsTextBox").DetectUrls, "Links in advisory text are never live");
                                SaveScreenshot(dlg, "security-advisory-details.png");
                            }
                        }
                    }
                    finally
                    {
                        Directory.Delete(dir, true);
                    }

                    await Task.CompletedTask;
                });
            }
        }

        [TestMethod]
        public void Branding_AboutWindowStatusBarAndHeader_FollowTheHtreasureKit()
        {
            using (var folder = new TestFolder())
            {
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder))
                    {
                        StatusStrip strip = form.Controls.OfType<StatusStrip>().Single();
                        ToolStripItem brand = strip.Items.Cast<ToolStripItem>().Single(i => i.Name == "brandStripLabel");
                        StringAssert.Matches(brand.Text, new System.Text.RegularExpressions.Regex(@"^© 20\d\d HTREASURE LTD · An HTREASURE LTD product$"));
                        Assert.AreEqual(BrandColors.DeepNavy.ToArgb(), UiStateTests.Find<Panel>(form, "headerPanel").BackColor.ToArgb(), "Deep Navy banner");
                    }

                    using (var about = new AboutForm { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false })
                    {
                        about.Show();
                        Application.DoEvents();
                        Assert.AreEqual("An HTREASURE LTD product", UiStateTests.Find<Label>(about, "attributionLabel").Text);
                        StringAssert.Contains(UiStateTests.Find<Label>(about, "companyLabel").Text, "company no. 17178941");
                        StringAssert.Contains(UiStateTests.Find<Label>(about, "companyLabel").Text, "England and Wales");
                        Assert.IsFalse(UiStateTests.Find<Label>(about, "companyLabel").Text.Contains("Registered office"), "No office address is shown");
                        Assert.AreEqual("support@htreasure.com", UiStateTests.Find<Label>(about, "supportValueLabel").Text);
                        Assert.AreEqual(AboutForm.CopyrightText(), UiStateTests.Find<Label>(about, "copyrightLabel").Text);
                        Assert.AreEqual("Security advisory history", UiStateTests.Find<LinkLabel>(about, "securityLinkLabel").Text);
                        var problems = new List<string>();
                        UiStateTests.CheckLayout(about, problems);

                        // The layout check measures text on one line; these labels wrap by design, so check the wrapped height instead.
                        foreach (Label wrapped in about.Controls.OfType<Label>().Where(l => !l.AutoSize))
                        {
                            Size needed = TextRenderer.MeasureText(wrapped.Text, wrapped.Font, new Size(wrapped.Width, int.MaxValue), TextFormatFlags.WordBreak);
                            Assert.IsTrue(needed.Height <= wrapped.Height, wrapped.Name + " needs " + needed.Height + " px but has " + wrapped.Height);
                            problems.RemoveAll(p => p.StartsWith("AboutForm/" + wrapped.Name + " text ", StringComparison.Ordinal));
                        }

                        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
                        foreach (Control c in about.Controls)
                        {
                            Assert.IsFalse(c.Text.Contains("®") || c.Text.Contains("™"), "No trade mark symbols before clearance");
                        }

                        SaveScreenshot(about, "about-branded.png");
                    }

                    await Task.CompletedTask;
                });
            }
        }

        [TestMethod]
        public void Windows7And81_AreSupported_AndTheMountCheckIsSkippedOnWindows7()
        {
            Assert.IsFalse(WindowsMountChecker.IsAvailableOn(new Version(6, 1, 7601)), "Windows 7 can't mount ISO images");
            Assert.IsTrue(WindowsMountChecker.IsAvailableOn(new Version(6, 2, 9200)), "Windows 8");
            Assert.IsTrue(WindowsMountChecker.IsAvailableOn(new Version(6, 3, 9600)), "Windows 8.1");
            Assert.IsTrue(WindowsMountChecker.IsAvailableOn(new Version(10, 0, 26300)));

            string root = Path.GetDirectoryName(typeof(SecurityAdvisoryTests).Assembly.Location);
            while (root != null && !File.Exists(Path.Combine(root, "HTISOCreator.sln")))
            {
                root = Path.GetDirectoryName(root);
            }

            Assert.IsNotNull(root);
            string setup = File.ReadAllText(Path.Combine(root, "installer", "HTISOCreator.iss"));
            StringAssert.Contains(setup, "MinVersion=6.1sp1");
            StringAssert.Contains(setup, @"DefaultDirName={autopf}\HTREASURE\{#AppName}");
            StringAssert.Contains(setup, "AppComments=An HTREASURE LTD product");
            StringAssert.Contains(setup, @"Source: ""{#SourceDir}\security\*""");
            string manifest = File.ReadAllText(Path.Combine(root, "HTISOCreator", "app.manifest"));
            StringAssert.Contains(manifest, "{35138b9a-5d96-4fbd-8e2d-a2440225f93a}", "Windows 7 declared");
            StringAssert.Contains(manifest, "{1f676c76-80e1-4239-95bb-83d0f6d0da78}", "Windows 8.1 declared");
        }

        internal static void SaveScreenshot(Form form, string name)
        {
            string folder = Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests");
            Directory.CreateDirectory(folder);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(Path.Combine(folder, name));
            }
        }

        private static SecurityAdvisoryCenter Centre(InstalledApplication app, string dir, TrustedKeySet keys, bool allowUnsigned, bool allowFixtures)
        {
            return new SecurityAdvisoryCenter(app, new SecurityAdvisoryOptions
            {
                BundledDirectory = dir,
                TrustedKeys = keys,
                AllowUnsignedBundledSnapshot = allowUnsigned,
                AllowTestFixtures = allowFixtures,
                Clock = () => Now
            });
        }

        private static void CopyInterop(string dir)
        {
            foreach (string f in new[] { "advisory-snapshot.json", "advisory-snapshot.json.sig.json" })
            {
                File.Copy(Path.Combine(Fixtures, "interop", f), Path.Combine(dir, f), true);
            }
        }

        private static string NewFolder()
        {
            string dir = Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests", "advisories-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static byte[] SignatureJson(string keyId, byte[] signature)
        {
            return Encoding.UTF8.GetBytes("{\"algorithm\":\"ECDSA-P256-SHA256\",\"keyId\":\"" + keyId + "\",\"signature\":\"" + Convert.ToBase64String(signature) + "\"}");
        }
    }
}
