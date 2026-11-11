using System;
using System.IO;
using System.Linq;
using System.Threading;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// Stage 3.2: Strict Mode, Compatibility Mode, filename verification and the conversion report.
    /// </summary>
    [TestClass]
    public class FilenameModeTests
    {
        /// <summary>The 70-character name from the Stage 3 regression run that Joliet cannot store.</summary>
        internal const string MsTestDll = "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll";

        internal const string MsTestDllInImage = "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformSe.dll";

        private static readonly LoggingService Log = SettingsServiceTests.TestLog;

        private static IsoCreationOptions Options(TestFolder folder, FilenameMode mode, bool approved = false,
            IsoFileSystemFormat format = IsoFileSystemFormat.Iso9660Joliet, string name = "names.iso")
        {
            return new IsoCreationOptions
            {
                SourceFolder = folder.Source,
                OutputPath = folder.OutputFile(name),
                VolumeLabel = "NAMES",
                Format = format,
                FilenameMode = mode,
                ConversionsApproved = approved,
                OverwriteExisting = File.Exists(folder.OutputFile(name))
            };
        }

        private static IsoBuildPlan Prepare(IsoCreationOptions options)
        {
            return new IsoCreationService(Log).Prepare(options, null, CancellationToken.None);
        }

        private static VerificationResult Verify(string iso, string source, FilenameMode? mode, IsoBuildPlan plan = null)
        {
            return new IsoVerificationService(Log, new WindowsMountChecker(Log)).Verify(new VerificationOptions
            {
                IsoPath = iso,
                SourceFolder = source,
                CompareHashes = true,
                FilenameMode = mode
            }, plan, null, CancellationToken.None);
        }

        private static VerificationCheck Check(VerificationResult result, string name)
        {
            return result.Checks.Single(c => c.Name == name);
        }

        private static void AddMsTestLayout(TestFolder folder)
        {
            folder.AddFile(@"Tests\bin\Debug\net462\" + MsTestDll, "debug build");
            folder.AddFile(@"Tests\bin\Release\net462\" + MsTestDll, "release build");
            folder.AddFile(@"Tests\bin\Release\net462\HTISOCreator.Tests.dll", "tests");
        }

        // ---------------------------------------------------------------- Strict Mode

        [TestMethod]
        public void Defaults_AreStrictMode()
        {
            Assert.AreEqual(FilenameMode.Strict, new AppSettings().FilenameMode, "New installations use Strict Mode");
            Assert.AreEqual(FilenameMode.Strict, new IsoCreationOptions().FilenameMode);
            Assert.IsFalse(new IsoCreationOptions().ConversionsApproved);
        }

        [TestMethod]
        public void NormalNames_StrictModeCreatesAndVerifiesExactNames()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("ReadMe.txt", "hello");
                folder.AddFile(@"Mixed Case Folder\Setup.EXE", "exe");
                folder.AddFile(@"Mixed Case Folder\lib\Some.Library.Name.dll", "dll");
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Strict));

                Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
                Assert.AreEqual(0, plan.RenamedItems.Count);
                Assert.IsNull(plan.StrictModeError);
                string iso = new IsoCreationService(Log).Create(plan, null, CancellationToken.None).OutputPath;

                VerificationResult result = Verify(iso, folder.Source, FilenameMode.Strict);
                Assert.IsTrue(result.Passed, result.BuildReport());
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "File names").Status);
                StringAssert.Contains(Check(result, "File names").Detail, "identical to the originals");
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "ISO 9660 directory tree").Status);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "Joliet directory tree").Status);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "Duplicate entries").Status);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "File contents (SHA-256)").Status);

                // Exact Joliet names, letter case included.
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                CollectionAssert.IsSubsetOf(new[] { "ReadMe.txt", @"Mixed Case Folder\Setup.EXE", @"Mixed Case Folder\lib\Some.Library.Name.dll" },
                    info.Entries.Select(e => e.Path).ToList());
            }
        }

        [TestMethod]
        public void MsTestDllRegression_StrictModeRejectsBeforeWritingAnything()
        {
            using (var folder = new TestFolder())
            {
                AddMsTestLayout(folder);
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Strict));

                Assert.IsFalse(plan.CanCreate);
                Assert.IsNotNull(plan.StrictModeError);
                StringAssert.Contains(plan.StrictModeError, "Strict Mode: 2 file or folder name(s) cannot be stored exactly");
                Assert.AreEqual(2, plan.RenamedItems.Count, "The Debug and Release copies are both reported");
                foreach (IsoNameChange change in plan.RenamedItems)
                {
                    StringAssert.EndsWith(change.SourcePath, MsTestDll);
                    StringAssert.EndsWith(change.ImagePath, MsTestDllInImage);
                    StringAssert.Contains(change.Reason, "longer than 64 characters");
                }

                CompatibilityReport report = CompatibilityService.Build(plan, null);
                CompatibilityFinding strict = report.Findings.Single(f => f.Title == "Names that cannot be preserved (Strict Mode)");
                Assert.AreEqual(FindingSeverity.Error, strict.Severity);
                Assert.AreEqual(2, strict.Items.Count);
                StringAssert.Contains(strict.Items[0], MsTestDll + "  →  ");
                Assert.IsFalse(report.Findings.Any(f => f.Title == "Problems that prevent creation"), "The Strict Mode error is not listed twice");

                Assert.ThrowsException<InvalidOperationException>(() => new IsoCreationService(Log).Create(plan, null, CancellationToken.None));
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length, "Nothing is written when Strict Mode rejects the plan");
            }
        }

        [TestMethod]
        public void StrictModeRejection_KeepsExistingIsoUnchanged()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("good.txt", "fine");
                string iso = new IsoCreationService(Log).Create(Prepare(Options(folder, FilenameMode.Strict)), null, CancellationToken.None).OutputPath;
                byte[] before = File.ReadAllBytes(iso);
                DateTime writtenBefore = File.GetLastWriteTimeUtc(iso);

                AddMsTestLayout(folder);
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Strict));
                Assert.IsFalse(plan.CanCreate);
                Assert.ThrowsException<InvalidOperationException>(() => new IsoCreationService(Log).Create(plan, null, CancellationToken.None));

                CollectionAssert.AreEqual(before, File.ReadAllBytes(iso), "The existing ISO must not change");
                Assert.AreEqual(writtenBefore, File.GetLastWriteTimeUtc(iso));
                Assert.AreEqual(1, Directory.GetFiles(folder.Output).Length, "No temporary or backup files are left");
            }
        }

        [TestMethod]
        public void UnsupportedCharacters_StrictModeRejectsWithReason()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("semi;colon.txt", "x");
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Strict));
                Assert.IsFalse(plan.CanCreate);
                IsoNameChange change = plan.RenamedItems.Single();
                Assert.AreEqual("semi;colon.txt", change.SourcePath);
                Assert.AreEqual("semi_colon.txt", change.ImagePath);
                StringAssert.Contains(change.Reason, "unsupported characters");
            }
        }

        [TestMethod]
        public void UnicodeNames_JolietPreservesExactly_Iso9660OnlyRejectsInStrictMode()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("Café ñ 日本語.txt", "unicode");
                folder.AddFile(@"Ελληνικά\файл.txt", "more");

                IsoBuildPlan joliet = Prepare(Options(folder, FilenameMode.Strict));
                Assert.IsTrue(joliet.CanCreate, string.Join("; ", joliet.Errors));
                string iso = new IsoCreationService(Log).Create(joliet, null, CancellationToken.None).OutputPath;
                VerificationResult result = Verify(iso, folder.Source, FilenameMode.Strict);
                Assert.IsTrue(result.Passed, result.BuildReport());
                Assert.IsTrue(IsoImageReader.Read(iso, CancellationToken.None).Entries.Any(e => e.Path == "Café ñ 日本語.txt"));

                IsoBuildPlan isoOnly = Prepare(Options(folder, FilenameMode.Strict, format: IsoFileSystemFormat.Iso9660, name: "plain.iso"));
                Assert.IsFalse(isoOnly.CanCreate);
                StringAssert.Contains(isoOnly.StrictModeError, "Choose a file system with long names (ISO 9660 + Joliet or UDF)");
            }
        }

        [TestMethod]
        public void EmptyFoldersZeroByteFilesAndDeepFolders_StrictModeCreatesAndVerifies()
        {
            using (var folder = new TestFolder())
            {
                string deep = string.Join("\\", Enumerable.Range(1, 12).Select(i => "Level" + i));
                folder.AddFile(deep + @"\bottom.txt", "deep");
                folder.AddDirectory(@"Empty Folder\Also Empty");
                folder.AddFile(@"zero\empty.dat", new byte[0]);
                folder.AddFile("zero-length.log", new byte[0]);

                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Strict));
                Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
                string iso = new IsoCreationService(Log).Create(plan, null, CancellationToken.None).OutputPath;

                VerificationResult result = Verify(iso, folder.Source, FilenameMode.Strict, plan);
                Assert.IsTrue(result.Passed, result.BuildReport());
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                Assert.IsTrue(info.Entries.Any(e => e.IsDirectory && e.Path == @"Empty Folder\Also Empty"));
                Assert.AreEqual(0, info.Entries.Single(e => e.Path == @"zero\empty.dat").Length);
                Assert.IsTrue(info.Entries.Any(e => e.Path == deep + @"\bottom.txt"));
            }
        }

        // ---------------------------------------------------------------- Compatibility Mode

        [TestMethod]
        public void CompatibilityMode_RefusesToWriteUnapprovedConversions()
        {
            using (var folder = new TestFolder())
            {
                AddMsTestLayout(folder);
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Compatibility, approved: false));

                Assert.IsTrue(plan.CanCreate, "Compatibility Mode plans can be created once approved");
                Assert.IsTrue(CompatibilityService.Build(plan, null).NeedsReview, "Every conversion is shown before creation");
                var ex = Assert.ThrowsException<InvalidOperationException>(() => new IsoCreationService(Log).Create(plan, null, CancellationToken.None));
                StringAssert.Contains(ex.Message, "not been approved");
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void MsTestDllRegression_CompatibilityModeCreatesApprovedPlanAndVerifiesIt()
        {
            using (var folder = new TestFolder())
            {
                AddMsTestLayout(folder);
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Compatibility, approved: true));
                CompatibilityReport report = CompatibilityService.Build(plan, null);
                CompatibilityFinding programs = report.Findings.Single(f => f.Title == "Program files that will be renamed");
                Assert.AreEqual(FindingSeverity.Warning, programs.Severity);
                StringAssert.Contains(programs.Detail, "may stop working");

                string iso = new IsoCreationService(Log).Create(plan, null, CancellationToken.None).OutputPath;
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                Assert.IsTrue(info.Entries.Any(e => e.Path == @"Tests\bin\Debug\net462\" + MsTestDllInImage));
                Assert.IsTrue(info.Entries.Any(e => e.Path == @"Tests\bin\Release\net462\" + MsTestDllInImage));
                Assert.AreEqual(0, info.Problems.Count, string.Join("; ", info.Problems));

                // Against the approved plan: names, sizes and SHA-256 all pass.
                VerificationResult approved = Verify(iso, folder.Source, null, plan);
                Assert.IsTrue(approved.Passed, approved.BuildReport());
                StringAssert.Contains(Check(approved, "File names").Detail, "2 converted as approved");

                // A passing SHA-256 comparison must not hide the renamed DLLs under Strict rules.
                VerificationResult strict = Verify(iso, folder.Source, FilenameMode.Strict);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(strict, "File contents (SHA-256)").Status);
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(strict, "File names").Status);
                Assert.IsFalse(strict.Passed);
                Assert.AreEqual(2, strict.NameDifferences.Count);
                StringAssert.Contains(strict.BuildReport(), "Names that differ from the original (2)");

                // Without a rule the difference is still reported, as a warning.
                VerificationResult reportOnly = Verify(iso, folder.Source, null);
                Assert.AreEqual(VerificationCheckStatus.Warning, Check(reportOnly, "File names").Status);
            }
        }

        [TestMethod]
        public void DifferentLongNamesWithSamePrefix_GetUniqueCollisionSafeNames()
        {
            using (var folder = new TestFolder())
            {
                string prefix = new string('A', 62);
                folder.AddFile(prefix + "_first_variant.dll", "1");
                folder.AddFile(prefix + "_second_variant.dll", "2");
                folder.AddFile(prefix + "_third_variant.dll", "3");

                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Compatibility, approved: true));
                Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
                Assert.AreEqual(3, plan.Files.Select(f => f.ImagePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(), "No two files share a destination");
                Assert.IsTrue(plan.Files.All(f => f.ImagePath.Length <= IsoNameMapper.JolietMaxNameLength && f.ImagePath.EndsWith(".dll", StringComparison.Ordinal)),
                    "Extensions are kept: " + string.Join(", ", plan.Files.Select(f => f.ImagePath)));
                Assert.AreEqual(2, CompatibilityService.Build(plan, null).CollisionCount);

                string iso = new IsoCreationService(Log).Create(plan, null, CancellationToken.None).OutputPath;
                VerificationResult result = Verify(iso, folder.Source, FilenameMode.Compatibility, plan);
                Assert.IsTrue(result.Passed, result.BuildReport());

                Assert.IsFalse(Prepare(Options(folder, FilenameMode.Strict, name: "strict.iso")).CanCreate);
            }
        }

        [TestMethod]
        public void CaseOnlyDifferences_AreTreatedAsCollisions()
        {
            // NTFS folders are normally case-insensitive, so this is checked on the mapper directly
            // (a case-sensitive folder can contain both names).
            IsoNameMapper.MappingResult mapping = IsoNameMapper.Map(new string[0], new[] { "Readme.txt", "README.TXT" }, IsoFileSystemFormat.Iso9660Joliet);
            Assert.AreEqual(2, mapping.Files.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count(), string.Join(", ", mapping.Files.Values));
            IsoNameChange change = mapping.Changes.Single();
            StringAssert.Contains(change.Reason, IsoNameMapper.DuplicateReason);
        }

        // ---------------------------------------------------------------- Report

        [TestMethod]
        public void ConversionReport_TextAndCsvContainEveryRequiredField()
        {
            using (var folder = new TestFolder())
            {
                AddMsTestLayout(folder);
                folder.AddFile(new string('B', 62) + "_x.txt", "1");
                folder.AddFile(new string('B', 62) + "_y.txt", "2");
                IsoBuildPlan plan = Prepare(Options(folder, FilenameMode.Compatibility, approved: true));

                FilenameConversionReport report = FilenameConversionReport.FromPlan(plan, true);
                Assert.AreEqual(ConversionStatus.AppliedInImage, report.Status);
                Assert.AreEqual(4, report.FilesAffected);
                Assert.AreEqual(1, report.Collisions.Count);

                string text = report.ToText();
                foreach (string expected in new[]
                         {
                             "Date:", "Source folder:    " + folder.Source, "Destination ISO:  " + folder.OutputFile("names.iso"),
                             "Compatibility Mode - Allow Filename Conversion", "Applied in image", "Affected:         4 file(s), 0 folder(s)",
                             "Collision warnings (1)", @"Tests\bin\Debug\net462\" + MsTestDll, "-> " + @"Tests\bin\Debug\net462\" + MsTestDllInImage,
                             "Reason: longer than 64 characters"
                         })
                {
                    StringAssert.Contains(text, expected);
                }

                string csvPath = Path.Combine(folder.Root, "report.csv");
                report.Save(csvPath);
                string[] lines = File.ReadAllLines(csvPath);
                CollectionAssert.Contains(lines, "Filename mode,Compatibility Mode - Allow Filename Conversion");
                CollectionAssert.Contains(lines, "Files affected,4");
                CollectionAssert.Contains(lines, "Type,Original relative path,Destination relative path,Reason,Status,Collision");
                Assert.IsTrue(lines.Any(l => l.StartsWith("File," + @"Tests\bin\Debug\net462\" + MsTestDll + "," + @"Tests\bin\Debug\net462\" + MsTestDllInImage +
                                                          ",longer than 64 characters,Applied in image,No", StringComparison.Ordinal)), string.Join("\n", lines));
                Assert.IsTrue(lines.Any(l => l.EndsWith(",Yes", StringComparison.Ordinal)), "Collision column");
                Assert.AreEqual(0xEF, File.ReadAllBytes(csvPath)[0], "UTF-8 BOM so Excel shows Unicode names");

                string txtPath = Path.Combine(folder.Root, "report.txt");
                report.Save(txtPath);
                StringAssert.StartsWith(File.ReadAllText(txtPath), "HT ISO Creator - Filename Conversion Report");
            }
        }

        [TestMethod]
        public void ConversionReport_StrictModeStatusAndNoChanges()
        {
            using (var folder = new TestFolder())
            {
                AddMsTestLayout(folder);
                FilenameConversionReport strict = FilenameConversionReport.FromPlan(Prepare(Options(folder, FilenameMode.Strict)), false);
                Assert.AreEqual(ConversionStatus.BlockedByStrictMode, strict.Status);
                StringAssert.Contains(strict.ToCsv(), "Blocked (Strict Mode)");

                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                FilenameConversionReport fromScan = FilenameConversionReport.FromAnalysis(analysis, "x.iso", IsoFileSystemFormat.Iso9660Joliet, FilenameMode.Compatibility);
                Assert.AreEqual(2, fromScan.Changes.Count);
                Assert.AreEqual(ConversionStatus.AwaitingApproval, fromScan.Status);
            }

            using (var clean = new TestFolder())
            {
                clean.AddFile("ok.txt", "x");
                string text = FilenameConversionReport.FromPlan(Prepare(Options(clean, FilenameMode.Strict)), false).ToText();
                StringAssert.Contains(text, "All names preserved exactly");
                StringAssert.Contains(text, "No names change.");
            }
        }

        [TestMethod]
        public void CsvEscaping_QuotesAndNeutralisesFormulas()
        {
            Assert.AreEqual("plain", FilenameConversionReport.Escape("plain"));
            Assert.AreEqual("\"a,b\"", FilenameConversionReport.Escape("a,b"));
            Assert.AreEqual("\"say \"\"hi\"\"\"", FilenameConversionReport.Escape("say \"hi\""));
            Assert.AreEqual("'=cmd()", FilenameConversionReport.Escape("=cmd()"));
            Assert.AreEqual("'-x", FilenameConversionReport.Escape("-x"));
        }

        // ---------------------------------------------------------------- Settings and history

        [TestMethod]
        public void FilenameMode_IsSavedAndOldSettingsFilesDefaultToStrict()
        {
            using (var folder = new TestFolder())
            {
                string file = Path.Combine(folder.Root, "settings.xml");
                var service = new SettingsService(file, Log);
                Assert.IsTrue(service.Save(new AppSettings { FilenameMode = FilenameMode.Compatibility, DefaultVolumeLabel = "KEEP_ME" }));
                Assert.AreEqual(FilenameMode.Compatibility, service.Load().FilenameMode);

                // A Stage 3 settings file has no FilenameMode element: other values are kept, the mode is Strict.
                string old = File.ReadAllText(file).Replace("<FilenameMode>Compatibility</FilenameMode>", string.Empty);
                Assert.IsFalse(old.Contains("FilenameMode"));
                File.WriteAllText(file, old);
                AppSettings loaded = service.Load();
                Assert.AreEqual(FilenameMode.Strict, loaded.FilenameMode);
                Assert.AreEqual("KEEP_ME", loaded.DefaultVolumeLabel);
            }
        }

        [TestMethod]
        public void History_RecordsFilenameModeAndReadsOlderEntries()
        {
            using (var folder = new TestFolder())
            {
                string file = Path.Combine(folder.Root, "history.xml");
                var history = new HistoryService(file, Log);
                Assert.IsTrue(history.Add(new HistoryEntry { OutputPath = "a.iso", FilenameMode = FilenameMode.Compatibility }));
                Assert.IsTrue(history.Add(new HistoryEntry { OutputPath = "b.iso" }));

                var loaded = history.Load();
                Assert.AreEqual(FilenameMode.Compatibility, loaded.Single(e => e.OutputPath == "a.iso").FilenameMode);
                Assert.IsNull(loaded.Single(e => e.OutputPath == "b.iso").FilenameMode, "Entries without a mode (Stage 3) stay unknown");
            }
        }
    }
}
