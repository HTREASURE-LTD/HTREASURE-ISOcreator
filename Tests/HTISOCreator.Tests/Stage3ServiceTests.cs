using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class CapacityTests
    {
        [DataTestMethod]
        [DataRow("cd650", 681984000L)]
        [DataRow("cd700", 737280000L)]
        [DataRow("dvd5", 4700372992L)]
        [DataRow("dvd9", 8547991552L)]
        [DataRow("bd25", 25025314816L)]
        public void Profiles_UseStandardSectorCapacities(string id, long bytes)
        {
            Assert.AreEqual(bytes, CapacityProfile.Find(id).GetCapacityBytes(0));
        }

        [TestMethod]
        public void NoLimitAndCustomProfiles()
        {
            Assert.IsNull(CapacityProfile.Find(CapacityProfile.NoLimitId).GetCapacityBytes(100));
            Assert.AreEqual(100L * 1024 * 1024, CapacityProfile.Find(CapacityProfile.CustomId).GetCapacityBytes(100));
            Assert.AreEqual(CapacityProfile.NoLimitId, CapacityProfile.Find("not-a-profile").Id);
        }

        [TestMethod]
        public void Evaluate_FitsExceedsUnknownAndNoLimit()
        {
            CapacityEvaluation fits = CapacityEvaluation.Evaluate(500, 1000, "Test disc");
            Assert.AreEqual(CapacityState.Fits, fits.State);
            Assert.AreEqual(50.0, fits.PercentUsed, 0.001);
            StringAssert.Contains(fits.Describe(), "50%");

            CapacityEvaluation exact = CapacityEvaluation.Evaluate(1000, 1000, "Test disc");
            Assert.AreEqual(CapacityState.Fits, exact.State, "An image exactly the capacity fits");

            CapacityEvaluation exceeds = CapacityEvaluation.Evaluate(1500, 1000, "Test disc");
            Assert.AreEqual(CapacityState.Exceeds, exceeds.State);
            Assert.AreEqual(500, exceeds.OverflowBytes);
            StringAssert.Contains(exceeds.Describe(), "Too large for Test disc");

            Assert.AreEqual(CapacityState.SizeUnknown, CapacityEvaluation.Evaluate(null, 1000, "x").State);
            Assert.AreEqual(CapacityState.NoLimit, CapacityEvaluation.Evaluate(5000, null, "x").State);
        }

        [TestMethod]
        public void ExceedingCapacity_IsAWarningNotAnError()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.bin", new byte[200000]);
                IsoBuildPlan plan = new IsoCreationService(SettingsServiceTests.TestLog).Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = folder.OutputFile(),
                    VolumeLabel = "CAP"
                }, null, CancellationToken.None);

                CompatibilityReport report = CompatibilityService.Build(plan, CapacityEvaluation.Evaluate(plan.ImageSize, 100000, "Tiny"));
                Assert.IsFalse(report.HasErrors);
                Assert.IsTrue(report.NeedsReview);
                Assert.IsTrue(report.Findings.Any(f => f.Title == "Larger than the selected disc" && f.Severity == FindingSeverity.Warning));
            }
        }
    }

    [TestClass]
    public class CompatibilityTests
    {
        private static IsoBuildPlan Plan(TestFolder folder, IsoFileSystemFormat format)
        {
            return new IsoCreationService(SettingsServiceTests.TestLog).Prepare(new IsoCreationOptions
            {
                SourceFolder = folder.Source,
                OutputPath = folder.OutputFile(),
                VolumeLabel = "COMPAT",
                Format = format
            }, null, CancellationToken.None);
        }

        [TestMethod]
        public void CleanFolder_NeedsNoReview()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("readme.txt", "x");
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660Joliet), null);
                Assert.IsFalse(report.NeedsReview);
                Assert.AreEqual("No compatibility problems found", report.Findings.Single().Title);
            }
        }

        [TestMethod]
        public void Joliet_UnsupportedCharactersAndLongNames_AreExplained()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("semi;colon.txt", "x");
                folder.AddFile(new string('L', 70) + ".txt", "x");
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660Joliet), null);

                Assert.IsTrue(report.NeedsReview);
                Assert.AreEqual(2, report.Renames.Count);
                StringAssert.Contains(report.Renames.Single(r => r.SourcePath.StartsWith("semi")).Reason, "unsupported characters");
                StringAssert.Contains(report.Renames.Single(r => r.SourcePath.StartsWith("LLL")).Reason, "longer than 64");
            }
        }

        [TestMethod]
        public void Iso9660_DuplicatesAfterConversion_AreFlagged()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("Report final.txt", "1");
                folder.AddFile("report-final.txt", "2");
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660), null);

                CompatibilityFinding duplicates = report.Findings.Single(f => f.Title == "Duplicate names after conversion");
                Assert.AreEqual(FindingSeverity.Warning, duplicates.Severity);
                Assert.AreEqual(1, duplicates.Items.Count);
                Assert.IsTrue(report.Renames.Any(r => r.Reason.Contains("duplicate name after conversion")));
                Assert.IsTrue(report.Renames.Any(r => r.Reason.Contains("converted to upper case")));
            }
        }

        [TestMethod]
        public void DeepFolders_AreWarningForIso9660AndInfoForJoliet()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"1\2\3\4\5\6\7\8\9\deep.txt", "x");
                Assert.AreEqual(FindingSeverity.Warning,
                    CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660), null).Findings.Single(f => f.Title.StartsWith("Folders nested")).Severity);
                Assert.AreEqual(FindingSeverity.Info,
                    CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660Joliet), null).Findings.Single(f => f.Title.StartsWith("Folders nested")).Severity);
            }
        }

        [TestMethod]
        public void LongIso9660Paths_AreWarned()
        {
            using (var folder = new TestFolder())
            {
                string path = string.Join("\\", Enumerable.Range(0, 9).Select(i => new string((char)('A' + i), 29))) + "\\f.txt";
                folder.AddFile(path, "x");
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660), null);
                Assert.IsTrue(report.Findings.Any(f => f.Title == "Paths longer than 255 characters"));
            }
        }

        [TestMethod]
        public void FilesOver4GB_AreErrorsAndOver2GB_AreWarnings()
        {
            using (var folder = new TestFolder())
            {
                folder.AddSparseFile("huge.bin", 5L * 1024 * 1024 * 1024);
                folder.AddSparseFile("large.bin", 3L * 1024 * 1024 * 1024);
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660Joliet), null);
                Assert.IsTrue(report.HasErrors);
                Assert.IsTrue(report.Findings.Single(f => f.Severity == FindingSeverity.Error).Items.Any(i => i.Contains("huge.bin")));
                Assert.IsTrue(report.Findings.Single(f => f.Title == "Files larger than 2 GB").Items.Single().StartsWith("large.bin"));
            }
        }

        [TestMethod]
        public void EmptyFoldersAndFiles_AreInformational()
        {
            using (var folder = new TestFolder())
            {
                folder.AddDirectory(@"empty\deeper");
                folder.AddFile("zero.txt", new byte[0]);
                CompatibilityReport report = CompatibilityService.Build(Plan(folder, IsoFileSystemFormat.Iso9660Joliet), null);
                StringAssert.StartsWith(report.Findings.Single(f => f.Title == "Empty folders").Detail, "1 empty folder");
                Assert.IsTrue(report.Findings.Any(f => f.Title == "Zero-byte files"));
                Assert.IsFalse(report.NeedsReview);
            }
        }

        [TestMethod]
        public void PlanReportsExactImageSize()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.bin", new byte[123456]);
                var service = new IsoCreationService(SettingsServiceTests.TestLog);
                IsoBuildPlan plan = Plan(folder, IsoFileSystemFormat.Iso9660Joliet);
                Assert.AreEqual(service.Create(plan, null, CancellationToken.None).ImageSize, plan.ImageSize);
            }
        }
    }

    [TestClass]
    public class HistoryTests
    {
        private static HistoryService NewService(TestFolder folder)
        {
            return new HistoryService(Path.Combine(folder.Root, "history.xml"), SettingsServiceTests.TestLog);
        }

        [TestMethod]
        public void Entries_RoundTripNewestFirst()
        {
            using (var folder = new TestFolder())
            {
                HistoryService history = NewService(folder);
                var older = new HistoryEntry
                {
                    Timestamp = new DateTime(2026, 1, 1, 10, 0, 0),
                    Outcome = CreationOutcome.Failed,
                    SourceFolder = @"C:\src",
                    OutputPath = @"D:\out\old.iso",
                    VolumeLabel = "OLD",
                    Format = IsoFileSystemFormat.Iso9660,
                    Message = "Disk full"
                };
                var newer = new HistoryEntry
                {
                    Timestamp = new DateTime(2026, 2, 1, 10, 0, 0),
                    Outcome = CreationOutcome.Succeeded,
                    SourceFolder = @"C:\src",
                    OutputPath = @"D:\out\new.iso",
                    VolumeLabel = "NEW",
                    Format = IsoFileSystemFormat.Iso9660Joliet,
                    ImageSize = 123456,
                    FileCount = 7,
                    DirectoryCount = 3,
                    DurationSeconds = 12.5,
                    Verification = HistoryVerificationStatus.Passed,
                    VerificationDetail = "Passed.",
                    LogReference = "abcd1234"
                };
                Assert.IsTrue(history.Add(older));
                Assert.IsTrue(history.Add(newer));

                List<HistoryEntry> loaded = NewService(folder).Load();
                Assert.AreEqual(2, loaded.Count);
                HistoryEntry first = loaded[0];
                Assert.AreEqual("NEW", first.VolumeLabel);
                Assert.AreEqual(CreationOutcome.Succeeded, first.Outcome);
                Assert.AreEqual(IsoFileSystemFormat.Iso9660Joliet, first.Format);
                Assert.AreEqual(123456, first.ImageSize);
                Assert.AreEqual(7, first.FileCount);
                Assert.AreEqual(3, first.DirectoryCount);
                Assert.AreEqual(12.5, first.DurationSeconds);
                Assert.AreEqual(HistoryVerificationStatus.Passed, first.Verification);
                Assert.AreEqual("abcd1234", first.LogReference);
                Assert.AreEqual(new DateTime(2026, 2, 1, 10, 0, 0), first.Timestamp);
                Assert.AreEqual("Disk full", loaded[1].Message);
            }
        }

        [TestMethod]
        public void Update_Remove_Clear()
        {
            using (var folder = new TestFolder())
            {
                HistoryService history = NewService(folder);
                var a = new HistoryEntry { OutputPath = "a.iso" };
                var b = new HistoryEntry { OutputPath = "b.iso" };
                history.Add(a);
                history.Add(b);

                a.Verification = HistoryVerificationStatus.Failed;
                history.Update(a);
                Assert.AreEqual(HistoryVerificationStatus.Failed, history.Load().Single(e => e.Id == a.Id).Verification);

                history.Remove(new[] { a.Id });
                Assert.AreEqual(b.Id, history.Load().Single().Id);

                history.Clear();
                Assert.AreEqual(0, history.Load().Count);
            }
        }

        [TestMethod]
        public void RecordVerification_UpdatesNewestSuccessfulEntryForThatIso()
        {
            using (var folder = new TestFolder())
            {
                HistoryService history = NewService(folder);
                history.Add(new HistoryEntry { Timestamp = DateTime.Now.AddHours(-2), Outcome = CreationOutcome.Succeeded, OutputPath = @"D:\x.iso" });
                history.Add(new HistoryEntry { Timestamp = DateTime.Now.AddHours(-1), Outcome = CreationOutcome.Succeeded, OutputPath = @"D:\X.ISO" });

                var result = new VerificationResult { IsoPath = @"D:\x.iso" };
                result.Checks.Add(new VerificationCheck("ISO structure", VerificationCheckStatus.Failed, "bad"));
                history.RecordVerification(@"D:\x.iso", result);

                List<HistoryEntry> loaded = history.Load();
                Assert.AreEqual(HistoryVerificationStatus.Failed, loaded[0].Verification);
                Assert.AreEqual(HistoryVerificationStatus.NotVerified, loaded[1].Verification);
                StringAssert.StartsWith(loaded[0].VerificationDetail, "FAILED");
            }
        }

        [TestMethod]
        public void History_IsCappedAtMaximumEntries()
        {
            using (var folder = new TestFolder())
            {
                string file = Path.Combine(folder.Root, "history.xml");
                var many = Enumerable.Range(0, HistoryService.MaxEntries).Select(i => new HistoryEntry { Timestamp = DateTime.Now.AddMinutes(-i - 1) }).ToList();
                using (FileStream stream = File.Create(file))
                {
                    new XmlSerializer(typeof(List<HistoryEntry>), new XmlRootAttribute("History")).Serialize(stream, many);
                }

                HistoryService history = NewService(folder);
                var newest = new HistoryEntry { OutputPath = "newest.iso" };
                history.Add(newest);
                List<HistoryEntry> loaded = history.Load();
                Assert.AreEqual(HistoryService.MaxEntries, loaded.Count);
                Assert.AreEqual(newest.Id, loaded[0].Id);
            }
        }

        [TestMethod]
        public void DamagedHistoryFile_IsKeptAsideAndHistoryRecovers()
        {
            using (var folder = new TestFolder())
            {
                string file = Path.Combine(folder.Root, "history.xml");
                File.WriteAllText(file, "<History><HistoryEntry><Id>broken");
                HistoryService history = NewService(folder);

                Assert.AreEqual(0, history.Load().Count);
                Assert.AreEqual(1, Directory.GetFiles(folder.Root, "history.xml.damaged-*").Length, "The damaged file must be kept, not deleted");
                Assert.IsTrue(history.Add(new HistoryEntry { OutputPath = "after.iso" }));
                Assert.AreEqual("after.iso", history.Load().Single().OutputPath);
            }
        }

        [TestMethod]
        public void CsvExport_EscapesSpecialCharacters()
        {
            string csv = HistoryService.ExportCsv(new[]
            {
                new HistoryEntry { OutputPath = @"D:\a, b.iso", Message = "said \"no\"", Timestamp = new DateTime(2026, 3, 4, 5, 6, 7) }
            });
            StringAssert.Contains(csv, "\"D:\\a, b.iso\"");
            StringAssert.Contains(csv, "\"said \"\"no\"\"\"");
            StringAssert.Contains(csv, "2026-03-04 05:06:07");
        }
    }

    [TestClass]
    public class RecoveryTests
    {
        private static PendingOperationsRegistry NewRegistry(TestFolder folder)
        {
            return new PendingOperationsRegistry(Path.Combine(folder.Root, "pending.txt"), SettingsServiceTests.TestLog);
        }

        [TestMethod]
        public void InterruptedTemporaryFile_IsDeletedOnRecovery()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("image.iso.1234.partial");
                File.WriteAllText(temp, "partial");
                NewRegistry(folder).RegisterTemporaryFile(temp);

                List<string> messages = NewRegistry(folder).Recover();
                Assert.IsFalse(File.Exists(temp));
                Assert.AreEqual(1, messages.Count);
                Assert.IsFalse(File.Exists(Path.Combine(folder.Root, "pending.txt")));
            }
        }

        [TestMethod]
        public void BackupWithoutDestination_IsRestored()
        {
            using (var folder = new TestFolder())
            {
                string destination = folder.OutputFile("image.iso");
                string backup = destination + ".abcd.backup";
                File.WriteAllText(backup, "original");
                NewRegistry(folder).RegisterBackup(backup, destination);

                List<string> messages = NewRegistry(folder).Recover();
                Assert.AreEqual("original", File.ReadAllText(destination));
                Assert.IsFalse(File.Exists(backup));
                StringAssert.Contains(messages.Single(), "Restored");
            }
        }

        [TestMethod]
        public void BackupWithNewImageInPlace_IsRemoved()
        {
            using (var folder = new TestFolder())
            {
                string destination = folder.OutputFile("image.iso");
                string backup = destination + ".abcd.backup";
                File.WriteAllText(destination, "new image");
                File.WriteAllText(backup, "old image");
                NewRegistry(folder).RegisterBackup(backup, destination);

                NewRegistry(folder).Recover();
                Assert.AreEqual("new image", File.ReadAllText(destination));
                Assert.IsFalse(File.Exists(backup));
            }
        }

        [TestMethod]
        public void NothingPending_RecoversNothing()
        {
            using (var folder = new TestFolder())
            {
                PendingOperationsRegistry registry = NewRegistry(folder);
                registry.RegisterTemporaryFile(folder.OutputFile("x.partial"));
                registry.Unregister(folder.OutputFile("x.partial"));
                Assert.AreEqual(0, registry.Recover().Count);
            }
        }

        [TestMethod]
        public void SuccessfulAndCancelledCreations_LeaveNothingPending()
        {
            using (var folder = new TestFolder())
            {
                var data = new byte[12 * 1024 * 1024];
                new Random(4).NextBytes(data);
                folder.AddFile("data.bin", data);
                File.WriteAllText(folder.OutputFile(), "existing");
                string registryFile = Path.Combine(folder.Root, "pending.txt");
                var service = new IsoCreationService(SettingsServiceTests.TestLog, FileSystemOperations.Instance, NewRegistry(folder));
                var options = new IsoCreationOptions { SourceFolder = folder.Source, OutputPath = folder.OutputFile(), VolumeLabel = "REC", OverwriteExisting = true };

                using (var cts = new CancellationTokenSource())
                {
                    IsoBuildPlan plan = service.Prepare(options, null, CancellationToken.None);
                    var progress = new VerificationTests.CallbackProgress(p =>
                    {
                        if (p.Stage == IsoOperationStage.Writing && p.BytesWritten > 0)
                        {
                            Assert.IsTrue(File.Exists(registryFile), "The temporary file is recorded while writing");
                            cts.Cancel();
                        }
                    });
                    Assert.ThrowsException<OperationCanceledException>(() => service.Create(plan, progress, cts.Token));
                }

                Assert.IsFalse(File.Exists(registryFile));
                Assert.AreEqual("existing", File.ReadAllText(folder.OutputFile()));

                service.Create(service.Prepare(options, null, CancellationToken.None), null, CancellationToken.None);
                Assert.IsFalse(File.Exists(registryFile));
                CollectionAssert.AreEqual(new[] { folder.OutputFile() }, Directory.GetFiles(folder.Output));
            }
        }

        [TestMethod]
        public void InsufficientSpace_IsReportedBeforeWriting()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.bin", new byte[500000]);
                var service = new IsoCreationService(SettingsServiceTests.TestLog) { FreeSpaceProvider = path => 100000 };
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = folder.OutputFile(),
                    VolumeLabel = "SPACE"
                }, null, CancellationToken.None);

                Assert.IsFalse(plan.CanCreate);
                Assert.IsTrue(plan.Errors.Any(e => e.Contains("not enough free space")), string.Join("; ", plan.Errors));
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length, "Nothing is written when space is short");
            }
        }

        [TestMethod]
        public void SpaceRunningOutBeforeWriting_FailsCleanly()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.bin", new byte[500000]);
                var service = new IsoCreationService(SettingsServiceTests.TestLog);
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = folder.OutputFile(),
                    VolumeLabel = "SPACE"
                }, null, CancellationToken.None);
                Assert.IsTrue(plan.CanCreate);

                // Space is used up by something else between preparing and writing.
                service.FreeSpaceProvider = path => 1000;
                var ex = Assert.ThrowsException<IsoCreationException>(() => service.Create(plan, null, CancellationToken.None));
                StringAssert.Contains(ex.Message, "not enough free space");
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }
    }
}
