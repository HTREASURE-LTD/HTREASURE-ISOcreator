using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DiscUtils.Iso9660;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class VerificationTests
    {
        private static readonly LoggingService Log = SettingsServiceTests.TestLog;

        internal static string CreateIso(TestFolder folder, IsoFileSystemFormat format = IsoFileSystemFormat.Iso9660Joliet, string label = "VERIFY_ME", string name = "test.iso")
        {
            var service = new IsoCreationService(Log);
            IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
            {
                SourceFolder = folder.Source,
                OutputPath = folder.OutputFile(name),
                VolumeLabel = label,
                Format = format,

                // Some callers deliberately use names that need conversion; Strict Mode is covered by FilenameModeTests.
                FilenameMode = FilenameMode.Compatibility,
                ConversionsApproved = true
            }, null, CancellationToken.None);
            Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
            return service.Create(plan, null, CancellationToken.None).OutputPath;
        }

        private static void AddSampleContent(TestFolder folder)
        {
            folder.AddFile("readme.txt", "hello");
            folder.AddFile(@"docs\guide.txt", "guide text");
            folder.AddFile(@"docs\deep\data.bin", Enumerable.Range(0, 70000).Select(i => (byte)(i * 7)).ToArray());
            folder.AddFile("Café ñ.txt", "unicode");
            folder.AddDirectory(@"empty\inner");
        }

        private static VerificationResult Verify(string iso, string source, bool hashes, string label = null, IsoFileSystemFormat? format = null)
        {
            return new IsoVerificationService(Log, new WindowsMountChecker(Log)).Verify(new VerificationOptions
            {
                IsoPath = iso,
                SourceFolder = source,
                CompareHashes = hashes,
                ExpectedLabel = label,
                ExpectedFormat = format
            }, null, null, CancellationToken.None);
        }

        private static VerificationCheck Check(VerificationResult result, string name)
        {
            return result.Checks.Single(c => c.Name == name);
        }

        [TestMethod]
        public void ValidIso_FullContentVerificationPasses()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);

                VerificationResult result = Verify(iso, folder.Source, true, "VERIFY_ME", IsoFileSystemFormat.Iso9660Joliet);

                Assert.IsTrue(result.Passed, result.BuildReport());
                Assert.AreEqual(VerificationLevel.SourceContent, result.Level);
                Assert.AreEqual(4, result.FilesCompared);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, "File contents (SHA-256)").Status);
                using (SHA256 sha = SHA256.Create())
                {
                    string expected = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(iso))).Replace("-", string.Empty);
                    Assert.AreEqual(expected, result.ImageSha256, "Whole-image hash must match an independent calculation");
                }

                StringAssert.Contains(result.BuildReport(), "Result:        PASSED");
            }
        }

        [TestMethod]
        public void ValidIso_WithoutSource_IsOnlyStructural()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);

                VerificationResult result = Verify(iso, null, false);

                Assert.IsTrue(result.Passed);
                Assert.AreEqual(VerificationLevel.Structural, result.Level);
                Assert.AreEqual(VerificationCheckStatus.Skipped, Check(result, "Source comparison").Status);
                StringAssert.Contains(result.Summary, "Structural verification only");
            }
        }

        [TestMethod]
        public void ValidIso_PathsAndSizesWithoutHashes_IsReportedAsSuch()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                VerificationResult result = Verify(iso, folder.Source, false);
                Assert.IsTrue(result.Passed);
                Assert.AreEqual(VerificationLevel.SourcePathsAndSizes, result.Level);
                Assert.AreEqual(VerificationCheckStatus.Skipped, Check(result, "File contents (SHA-256)").Status);
            }
        }

        [TestMethod]
        public void Iso9660OnlyImage_VerifiesAgainstSourceUsingNameMapping()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder, IsoFileSystemFormat.Iso9660, "PLAIN");
                VerificationResult result = Verify(iso, folder.Source, true, "PLAIN", IsoFileSystemFormat.Iso9660);
                Assert.IsTrue(result.Passed, result.BuildReport());
            }
        }

        [TestMethod]
        public void NonIsoFile_IsRejected()
        {
            using (var folder = new TestFolder())
            {
                string fake = folder.OutputFile("text.iso");
                File.WriteAllText(fake, new string('x', 100000));
                VerificationResult result = Verify(fake, null, false);
                Assert.IsFalse(result.Passed);
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(result, "ISO structure").Status);
            }
        }

        [TestMethod]
        public void ZipRenamedToIso_IsRejected()
        {
            using (var folder = new TestFolder())
            {
                string zip = folder.OutputFile("archive.iso");
                var bytes = new byte[64 * 1024];
                bytes[0] = (byte)'P';
                bytes[1] = (byte)'K';
                bytes[2] = 3;
                bytes[3] = 4;
                File.WriteAllBytes(zip, bytes);

                VerificationResult result = Verify(zip, null, false);
                Assert.IsFalse(result.Passed);
                StringAssert.Contains(Check(result, "ISO structure").Detail, "not an ISO 9660 image");
            }
        }

        [TestMethod]
        public void TruncatedIso_IsRejected()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("big.bin", new byte[300000]);
                string iso = CreateIso(folder);
                using (var stream = new FileStream(iso, FileMode.Open))
                {
                    stream.SetLength(stream.Length / 2 / 2048 * 2048);
                }

                VerificationResult result = Verify(iso, folder.Source, false);
                Assert.IsFalse(result.Passed);
                Assert.IsTrue(result.Image.Problems.Any(p => p.Contains("truncated")), string.Join("; ", result.Image.Problems));
            }
        }

        [TestMethod]
        public void CorruptedDirectoryRecords_AreRejected()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                IsoImageEntry docs = info.Entries.Single(e => e.Path == "docs");

                // Overwrite the docs directory's records with garbage.
                using (var stream = new FileStream(iso, FileMode.Open))
                {
                    stream.Position = docs.ExtentSector * 2048;
                    var junk = Enumerable.Repeat((byte)0xFF, 2048).ToArray();
                    stream.Write(junk, 0, junk.Length);
                }

                VerificationResult result = Verify(iso, folder.Source, false);
                Assert.IsFalse(result.Passed);
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(result, "ISO structure").Status);
            }
        }

        [TestMethod]
        public void MissingAndExtraFiles_AreReported()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                folder.AddFile("added later.txt", "new");
                File.Delete(Path.Combine(folder.Source, "readme.txt"));

                VerificationResult result = Verify(iso, folder.Source, false);

                Assert.IsFalse(result.Passed);
                CollectionAssert.Contains(result.MissingFromImage, "added later.txt");
                CollectionAssert.Contains(result.ExtraInImage, "readme.txt");
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(result, "Files and folders").Status);
            }
        }

        [TestMethod]
        public void MissingSourceFolder_FailsComparison()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                VerificationResult result = Verify(iso, Path.Combine(folder.Root, "gone"), true);
                Assert.IsFalse(result.Passed);
                StringAssert.Contains(Check(result, "Source comparison").Detail, "does not exist");
            }
        }

        [TestMethod]
        public void ModifiedSize_IsReported()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                File.AppendAllText(Path.Combine(folder.Source, "readme.txt"), " more");

                VerificationResult result = Verify(iso, folder.Source, false);
                Assert.IsFalse(result.Passed);
                Assert.AreEqual(1, result.SizeMismatches.Count);
                StringAssert.StartsWith(result.SizeMismatches[0], "readme.txt");
            }
        }

        [TestMethod]
        public void ModifiedContentWithSameSize_IsOnlyCaughtBySha256()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                File.WriteAllText(Path.Combine(folder.Source, @"docs\guide.txt"), "GUIDE TEXT");

                VerificationResult sizesOnly = Verify(iso, folder.Source, false);
                Assert.IsTrue(sizesOnly.Passed, "Same size and path: a size check cannot see the change");
                Assert.AreEqual(VerificationLevel.SourcePathsAndSizes, sizesOnly.Level);

                VerificationResult hashed = Verify(iso, folder.Source, true);
                Assert.IsFalse(hashed.Passed);
                CollectionAssert.AreEqual(new[] { @"docs\guide.txt" }, hashed.ContentMismatches);
                StringAssert.Contains(hashed.BuildReport(), "Different content (SHA-256) (1)");
            }
        }

        [TestMethod]
        public void WrongLabelOrFormat_FailsWhenExpected()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                string iso = CreateIso(folder);
                Assert.IsFalse(Verify(iso, null, false, "OTHER").Passed);
                Assert.IsFalse(Verify(iso, null, false, null, IsoFileSystemFormat.Iso9660).Passed);
            }
        }

        [TestMethod]
        public void MissingIsoFile_FailsWithoutThrowing()
        {
            using (var folder = new TestFolder())
            {
                VerificationResult result = Verify(folder.OutputFile("absent.iso"), null, false);
                Assert.IsFalse(result.Passed);
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(result, "Image file").Status);
            }
        }

        [TestMethod]
        public void VerificationAgainstPlan_PassesRightAfterCreation()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                var service = new IsoCreationService(Log);
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = folder.OutputFile(),
                    VolumeLabel = "PLAN_CHECK"
                }, null, CancellationToken.None);
                IsoCreationResult created = service.Create(plan, null, CancellationToken.None);

                VerificationResult result = new IsoVerificationService(Log, new WindowsMountChecker(Log)).Verify(new VerificationOptions
                {
                    IsoPath = created.OutputPath,
                    CompareHashes = true,
                    ExpectedLabel = "PLAN_CHECK",
                    ExpectedFormat = IsoFileSystemFormat.Iso9660Joliet
                }, plan, null, CancellationToken.None);

                Assert.IsTrue(result.Passed, result.BuildReport());
                Assert.AreEqual(folder.Source, result.SourceFolder);
            }
        }

        [TestMethod]
        public void Verification_CanBeCancelled()
        {
            using (var folder = new TestFolder())
            {
                var data = new byte[20 * 1024 * 1024];
                new Random(9).NextBytes(data);
                folder.AddFile("big.bin", data);
                string iso = CreateIso(folder);

                using (var cts = new CancellationTokenSource())
                {
                    var progress = new CallbackProgress(p =>
                    {
                        if (p.Stage == IsoOperationStage.Verifying && p.BytesWritten > 0)
                        {
                            cts.Cancel();
                        }
                    });

                    Assert.ThrowsException<OperationCanceledException>(() =>
                        new IsoVerificationService(Log, new WindowsMountChecker(Log)).Verify(new VerificationOptions
                        {
                            IsoPath = iso,
                            SourceFolder = folder.Source,
                            CompareHashes = true
                        }, null, progress, cts.Token));
                }

                // The image and source must not be held open after cancelling.
                File.Delete(iso);
                File.Delete(Path.Combine(folder.Source, "big.bin"));
            }
        }

        [TestMethod]
        public void IndependentReader_AgreesWithDiscUtilsReader()
        {
            using (var folder = new TestFolder())
            {
                AddSampleContent(folder);
                folder.AddFile(new string('n', 80) + ".txt", "long");
                string iso = CreateIso(folder);

                IsoImageInfo ours = IsoImageReader.Read(iso, CancellationToken.None);
                List<string> dirs;
                string label;
                Dictionary<string, byte[]> theirs = TestFolder.ReadImageFiles(iso, true, out dirs, out label);

                Assert.AreEqual(label, ours.JolietVolumeLabel);
                CollectionAssert.AreEquivalent(theirs.Keys.ToList(), ours.Entries.Where(e => !e.IsDirectory).Select(e => e.Path).ToList());
                CollectionAssert.AreEquivalent(dirs, ours.Entries.Where(e => e.IsDirectory).Select(e => e.Path).ToList());
                using (FileStream stream = File.OpenRead(iso))
                {
                    foreach (IsoImageEntry entry in ours.Entries.Where(e => !e.IsDirectory))
                    {
                        using (Stream extent = IsoImageReader.OpenExtent(stream, entry))
                        using (var memory = new MemoryStream())
                        {
                            extent.CopyTo(memory);
                            CollectionAssert.AreEqual(theirs[entry.Path], memory.ToArray(), entry.Path);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void SourceTimestamps_ArePreservedInTheImage()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile(@"dated\file.txt", "x");
                var fileTime = new DateTime(2015, 6, 1, 12, 34, 56, DateTimeKind.Utc);
                var dirTime = new DateTime(2016, 7, 2, 8, 9, 10, DateTimeKind.Utc);
                File.SetLastWriteTimeUtc(file, fileTime);
                Directory.SetLastWriteTimeUtc(Path.Combine(folder.Source, "dated"), dirTime);

                string iso = CreateIso(folder);
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);

                Assert.AreEqual(fileTime, info.Entries.Single(e => e.Path == @"dated\file.txt").RecordedTime);
                Assert.AreEqual(dirTime, info.Entries.Single(e => e.Path == "dated").RecordedTime);
                Assert.AreEqual(fileTime, info.PrimaryEntries.Single(e => !e.IsDirectory).RecordedTime, "The ISO 9660 tree carries the same date");
            }
        }

        internal sealed class CallbackProgress : IProgress<IsoProgressInfo>
        {
            private readonly Action<IsoProgressInfo> _callback;

            public CallbackProgress(Action<IsoProgressInfo> callback)
            {
                _callback = callback;
            }

            public void Report(IsoProgressInfo value)
            {
                _callback(value);
            }
        }
    }
}
