using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// Stage 3.3: UDF and ISO 9660 + Joliet + UDF images written with Windows IMAPI2, read back with
    /// HT ISO Creator's own UDF reader.
    /// </summary>
    [TestClass]
    public class UdfTests
    {
        private static readonly LoggingService Log = SettingsServiceTests.TestLog;

        private static readonly string Name124 = new string('M', 120) + ".dat";

        private static IsoCreationOptions Options(TestFolder folder, IsoFileSystemFormat format, FilenameMode mode = FilenameMode.Strict,
            bool approved = false, string name = "udf.iso")
        {
            return new IsoCreationOptions
            {
                SourceFolder = folder.Source,
                OutputPath = folder.OutputFile(name),
                VolumeLabel = "UDF_TEST",
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

        private static string Create(IsoBuildPlan plan)
        {
            Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
            return new IsoCreationService(Log).Create(plan, null, CancellationToken.None).OutputPath;
        }

        private static VerificationResult Verify(string iso, IsoBuildPlan plan, string source = null, FilenameMode? mode = null, bool hashes = true)
        {
            return new IsoVerificationService(Log, new WindowsMountChecker(Log)).Verify(new VerificationOptions
            {
                IsoPath = iso,
                SourceFolder = source,
                CompareHashes = hashes,
                FilenameMode = mode,
                ExpectedLabel = "UDF_TEST"
            }, plan, null, CancellationToken.None);
        }

        private static VerificationCheck Check(VerificationResult result, string name)
        {
            return result.Checks.Single(c => c.Name == name);
        }

        /// <summary>Names that ISO 9660 + Joliet cannot keep but UDF can.</summary>
        private static void AddLongNameContent(TestFolder folder)
        {
            folder.AddFile(@"Tests\bin\Debug\net462\" + FilenameModeTests.MsTestDll, "debug");
            folder.AddFile(@"Tests\bin\Release\net462\" + FilenameModeTests.MsTestDll, "release");
            folder.AddFile(@"long\" + Name124, Enumerable.Range(0, 300000).Select(i => (byte)(i * 13)).ToArray());
            folder.AddFile(@"unicode\Café ñ 日本語 Ελληνικά файл.txt", "unicode");
            folder.AddFile(@"punct\semi;colon.txt", "semicolon");
            folder.AddFile(@"punct\Mixed Case Name.TXT", "case");
            folder.AddFile(string.Join("\\", Enumerable.Range(1, 12).Select(i => "Level" + i)) + @"\deep.txt", "deep");
            folder.AddDirectory(@"empty\inner");
            folder.AddFile("zero.bin", new byte[0]);
        }

        private static readonly string[] ExpectedPaths =
        {
            @"Tests\bin\Debug\net462\" + FilenameModeTests.MsTestDll,
            @"long\" + Name124,
            @"unicode\Café ñ 日本語 Ελληνικά файл.txt",
            @"punct\semi;colon.txt",
            @"punct\Mixed Case Name.TXT",
            @"empty\inner",
            "zero.bin"
        };

        [TestMethod]
        public void ImapiIsAvailableOnThisWindows()
        {
            Assert.IsNull(ImapiImageBuilder.CheckAvailable());
        }

        [TestMethod]
        public void Udf_StrictModeKeepsMsTestDllAndLongNamesExactly()
        {
            using (var folder = new TestFolder())
            {
                AddLongNameContent(folder);
                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Udf));
                Assert.AreEqual(0, plan.RenamedItems.Count, "UDF stores every name exactly");
                string iso = Create(plan);

                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                Assert.AreEqual(0, info.Problems.Count, string.Join("; ", info.Problems));
                Assert.IsTrue(info.HasUdf);
                Assert.IsFalse(info.HasIso9660, "UDF only: no ISO 9660 view");
                Assert.AreEqual(IsoFileSystemFormat.Udf, info.Format);
                Assert.AreEqual("UDF_TEST", info.UdfVolumeLabel);
                Assert.AreEqual("1.02", info.UdfRevision);
                List<string> paths = info.Entries.Select(e => e.Path).ToList();
                foreach (string expected in ExpectedPaths)
                {
                    CollectionAssert.Contains(paths, expected, "Exact name expected (case-sensitive): " + expected);
                }

                VerificationResult afterCreation = Verify(iso, plan);
                Assert.IsTrue(afterCreation.Passed, afterCreation.BuildReport());
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(afterCreation, "UDF file system").Status);
                Assert.AreEqual(VerificationCheckStatus.Skipped, Check(afterCreation, "ISO 9660 directory tree").Status);
                Assert.AreEqual(VerificationCheckStatus.Passed, Check(afterCreation, "File contents (SHA-256)").Status);
                StringAssert.Contains(Check(afterCreation, "File names").Detail, "(UDF names)");

                VerificationResult fromSource = Verify(iso, null, folder.Source, FilenameMode.Strict);
                Assert.IsTrue(fromSource.Passed, fromSource.BuildReport());
                Assert.AreEqual(8, fromSource.FilesCompared);
            }
        }

        [TestMethod]
        public void Udf_RecordsSourceFileAndFolderDates()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile(@"dated\file.txt", "x");
                var fileTime = new DateTime(2021, 3, 4, 5, 6, 7, DateTimeKind.Utc);
                var dirTime = new DateTime(2019, 8, 9, 10, 11, 12, DateTimeKind.Utc);
                File.SetLastWriteTimeUtc(file, fileTime);
                Directory.SetLastWriteTimeUtc(Path.GetDirectoryName(file), dirTime);

                IsoImageInfo info = IsoImageReader.Read(Create(Prepare(Options(folder, IsoFileSystemFormat.Udf))), CancellationToken.None);
                Assert.AreEqual(fileTime, info.Entries.Single(e => e.Path == @"dated\file.txt").RecordedTime);
                Assert.AreEqual(dirTime, info.Entries.Single(e => e.Path == "dated").RecordedTime);
            }
        }

        [TestMethod]
        public void Hybrid_UdfNamesExact_AndAllThreeViewsAgree()
        {
            using (var folder = new TestFolder())
            {
                AddLongNameContent(folder);
                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Iso9660JolietUdf));
                Assert.AreEqual(0, plan.RenamedItems.Count);
                string iso = Create(plan);

                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                Assert.AreEqual(0, info.Problems.Count, string.Join("; ", info.Problems));
                Assert.IsTrue(info.HasUdf && info.HasIso9660 && info.HasJoliet);
                Assert.AreEqual(IsoFileSystemFormat.Iso9660JolietUdf, info.Format);
                Assert.AreSame(info.UdfEntries.Count == info.Entries.Count ? info.Entries : null, info.Entries, "Windows shows the UDF view");
                CollectionAssert.IsSubsetOf(ExpectedPaths, info.Entries.Select(e => e.Path).ToList());
                Assert.AreEqual(info.UdfEntries.Count, info.JolietEntries.Count);
                Assert.AreEqual(info.UdfEntries.Count, info.PrimaryEntries.Count);
                Assert.IsTrue(info.PrimaryEntries.All(e => Path.GetFileName(e.Path).Length <= 12), "IMAPI2 writes 8.3 ISO 9660 alias names");

                VerificationResult result = Verify(iso, plan);
                Assert.IsTrue(result.Passed, result.BuildReport());
                foreach (string check in new[] { "ISO 9660 directory tree", "Joliet directory tree", "UDF file system", "Duplicate entries", "File names", "File contents (SHA-256)" })
                {
                    Assert.AreEqual(VerificationCheckStatus.Passed, Check(result, check).Status, check);
                }
            }
        }

        [TestMethod]
        public void NamesOver127_StrictRejects_CompatibilityShortensWithoutCollisions()
        {
            using (var folder = new TestFolder())
            {
                string prefix = new string('L', 140);
                folder.AddFile(prefix + "_one.bin", "1");
                folder.AddFile(prefix + "_two.bin", "2");

                IsoBuildPlan strict = Prepare(Options(folder, IsoFileSystemFormat.Udf));
                Assert.IsFalse(strict.CanCreate);
                Assert.AreEqual(2, strict.RenamedItems.Count);
                StringAssert.Contains(strict.RenamedItems[0].Reason, "longer than 127 characters (UDF limit)");
                StringAssert.Contains(strict.StrictModeError, "cannot be stored exactly in UDF");

                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Udf, FilenameMode.Compatibility, approved: true, name: "compat.iso"));
                List<string> names = plan.Files.Select(f => f.ImagePath).ToList();
                Assert.AreEqual(2, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.IsTrue(names.All(n => n.Length <= FileSystemFormats.UdfMaxNameLength && n.EndsWith(".bin", StringComparison.Ordinal)), string.Join(", ", names));

                string iso = Create(plan);
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                CollectionAssert.AreEquivalent(names, info.Entries.Select(e => e.Path).ToList(), "IMAPI2 used exactly the planned names");
                VerificationResult result = Verify(iso, plan);
                Assert.IsTrue(result.Passed, result.BuildReport());
                StringAssert.Contains(Check(result, "File names").Detail, "2 converted as approved");
            }
        }

        [TestMethod]
        public void FilesOf4GBOrMore_OnlyUdfAccepts_AndImageReadsBack()
        {
            using (var folder = new TestFolder())
            {
                string big = Path.Combine(folder.Source, "big.bin");
                long size = 4L * 1024 * 1024 * 1024 + 4096;
                using (var stream = new FileStream(big, FileMode.CreateNew))
                {
                    stream.SetLength(size);
                    stream.Position = size - 4;
                    stream.Write(new byte[] { 9, 8, 7, 6 }, 0, 4);
                }

                foreach (IsoFileSystemFormat format in new[] { IsoFileSystemFormat.Iso9660Joliet, IsoFileSystemFormat.Iso9660JolietUdf })
                {
                    IsoBuildPlan rejected = Prepare(Options(folder, format));
                    Assert.IsFalse(rejected.CanCreate, format.ToString());
                    StringAssert.Contains(rejected.Errors[0], "Choose the UDF file system");
                }

                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Udf));
                Assert.IsTrue(plan.CanCreate, string.Join("; ", plan.Errors));
                Assert.IsTrue(plan.ImageSize > size);
                string iso = Create(plan);
                try
                {
                    Assert.AreEqual(plan.ImageSize, new FileInfo(iso).Length, "Size calculated before writing matches the image");
                    IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);
                    IsoImageEntry entry = info.Entries.Single(e => e.Path == "big.bin");
                    Assert.AreEqual(size, entry.Length);
                    Assert.IsTrue(entry.DataRanges.Count >= 4, "A file over 4 GB needs several UDF extents (each is under 1 GB)");
                    using (var image = File.OpenRead(iso))
                    using (Stream data = IsoImageReader.OpenExtent(image, entry))
                    {
                        Assert.AreEqual(size, data.Length);
                        long remaining = size - 4;
                        var buffer = new byte[1024 * 1024];
                        long read = 0;
                        while (read < remaining)
                        {
                            read += data.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining - read));
                        }

                        var tail = new byte[4];
                        Assert.AreEqual(4, data.Read(tail, 0, 4));
                        CollectionAssert.AreEqual(new byte[] { 9, 8, 7, 6 }, tail);
                    }
                }
                finally
                {
                    File.Delete(iso);
                }
            }
        }

        [TestMethod]
        public void SizeCalculation_MatchesImapiImages()
        {
            using (var folder = new TestFolder())
            {
                AddLongNameContent(folder);
                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                foreach (IsoFileSystemFormat format in new[] { IsoFileSystemFormat.Udf, IsoFileSystemFormat.Iso9660JolietUdf })
                {
                    long? predicted = IsoSizeCalculator.Calculate(analysis, format);
                    string iso = Create(Prepare(Options(folder, format, name: format + ".iso")));
                    Assert.AreEqual(new FileInfo(iso).Length, predicted, format.ToString());
                }
            }
        }

        [TestMethod]
        public void Cancellation_LeavesNoFiles()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("big.bin", new byte[40 * 1024 * 1024]);
                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Udf));
                using (var cancellation = new CancellationTokenSource())
                {
                    var progress = new SynchronousProgress(p =>
                    {
                        if (p.BytesWritten > 0)
                        {
                            cancellation.Cancel();
                        }
                    });
                    Assert.ThrowsException<OperationCanceledException>(() => new IsoCreationService(Log).Create(plan, progress, cancellation.Token));
                }

                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length, "No partial image is left");
            }
        }

        [TestMethod]
        public void SourceChangedBeforeWriting_FailsWithTheRealReason()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile("changing.txt", "original");
                IsoBuildPlan plan = Prepare(Options(folder, IsoFileSystemFormat.Udf));
                File.WriteAllText(file, "changed and longer");

                var ex = Assert.ThrowsException<IsoCreationException>(() => new IsoCreationService(Log).Create(plan, null, CancellationToken.None));
                StringAssert.Contains(ex.Message, "changing.txt");
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void DamagedUdfImage_IsRejectedOrReported()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "hello");
                string iso = Create(Prepare(Options(folder, IsoFileSystemFormat.Udf)));

                // Wrong checksum in the anchor at sector 256: no readable file system remains.
                string broken = folder.OutputFile("broken.iso");
                File.Copy(iso, broken);
                using (var stream = new FileStream(broken, FileMode.Open))
                {
                    stream.Position = 256 * 2048 + 4;
                    stream.WriteByte(0x5A);
                }

                Assert.ThrowsException<InvalidIsoImageException>(() => IsoImageReader.Read(broken, CancellationToken.None));
                VerificationResult result = Verify(broken, null, folder.Source);
                Assert.IsFalse(result.Passed);
                Assert.AreEqual(VerificationCheckStatus.Failed, Check(result, "ISO structure").Status);

                // Truncated image: the second half (file data, end anchors, reserve descriptors) is missing.
                // (Cutting only the last few sectors removes IMAPI2's padding and extra anchor copies,
                // which leaves a valid image.)
                string truncated = folder.OutputFile("truncated.iso");
                File.Copy(iso, truncated);
                using (var stream = new FileStream(truncated, FileMode.Open))
                {
                    stream.SetLength(stream.Length / 2 / 2048 * 2048);
                }

                VerificationResult truncatedResult = Verify(truncated, null, folder.Source);
                Assert.IsFalse(truncatedResult.Passed, truncatedResult.BuildReport());

                // Volume recognition sequence wiped (found in Stage 4.3 QA): files are still readable
                // through the anchor, but Windows refuses to mount it, so verification must fail.
                using (var original = File.OpenRead(iso))
                {
                    Assert.IsTrue(UdfImageReader.HasNsrDescriptor(original));
                }

                string noVrs = folder.OutputFile("no-vrs.iso");
                File.Copy(iso, noVrs);
                using (var stream = new FileStream(noVrs, FileMode.Open))
                {
                    stream.Position = 16 * 2048;
                    stream.Write(new byte[2048], 0, 2048);
                }

                VerificationResult noVrsResult = Verify(noVrs, null, folder.Source);
                Assert.IsFalse(noVrsResult.Passed, noVrsResult.BuildReport());
                StringAssert.Contains(noVrsResult.BuildReport(), "volume recognition sequence");
            }
        }

        [TestMethod]
        public void ModifiedSourceContent_IsCaughtInUdfImage()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile(@"docs\guide.txt", "guide text");
                string iso = Create(Prepare(Options(folder, IsoFileSystemFormat.Iso9660JolietUdf)));
                File.WriteAllText(file, "GUIDE TEXT");

                VerificationResult result = Verify(iso, null, folder.Source, FilenameMode.Strict);
                Assert.IsFalse(result.Passed);
                CollectionAssert.Contains(result.ContentMismatches, @"docs\guide.txt");
            }
        }

        [TestMethod]
        public void UdfFormatsReportEngineAndRules()
        {
            Assert.AreEqual("Windows IMAPI2", FileSystemFormats.EngineName(IsoFileSystemFormat.Udf));
            Assert.AreEqual("DiscUtils", FileSystemFormats.EngineName(IsoFileSystemFormat.Iso9660Joliet));
            Assert.AreEqual(long.MaxValue, FileSystemFormats.MaxFileSize(IsoFileSystemFormat.Udf));
            Assert.AreEqual((long)uint.MaxValue, FileSystemFormats.MaxFileSize(IsoFileSystemFormat.Iso9660JolietUdf));
            StringAssert.Contains(CompatibilityService.RulesFor(IsoFileSystemFormat.Udf), "127");
        }

        /// <summary>Progress reported on the calling thread (Progress&lt;T&gt; would post to the thread pool).</summary>
        private sealed class SynchronousProgress : IProgress<IsoProgressInfo>
        {
            private readonly Action<IsoProgressInfo> _handler;

            public SynchronousProgress(Action<IsoProgressInfo> handler)
            {
                _handler = handler;
            }

            public void Report(IsoProgressInfo value)
            {
                _handler(value);
            }
        }
    }
}
