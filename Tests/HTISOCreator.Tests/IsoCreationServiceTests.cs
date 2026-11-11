using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class IsoCreationServiceTests
    {
        private static readonly LoggingService TestLog =
            new LoggingService(Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests", "logs"));

        private static IsoCreationOptions Options(TestFolder folder, string label = "TEST_DISC", IsoFileSystemFormat format = IsoFileSystemFormat.Iso9660Joliet)
        {
            return new IsoCreationOptions
            {
                SourceFolder = folder.Source,
                OutputPath = folder.OutputFile(),
                VolumeLabel = label,
                Format = format,

                // These tests exercise the name conversion engine; Strict Mode is covered by FilenameModeTests.
                FilenameMode = FilenameMode.Compatibility,
                ConversionsApproved = true
            };
        }

        private static IsoCreationResult PrepareAndCreate(IsoCreationOptions options, out IsoBuildPlan plan)
        {
            var service = new IsoCreationService(TestLog);
            plan = service.Prepare(options, null, CancellationToken.None);
            Assert.IsTrue(plan.CanCreate, "Plan errors: " + string.Join("; ", plan.Errors));
            return service.Create(plan, null, CancellationToken.None);
        }

        [TestMethod]
        public void SmallFolder_ImageContainsOriginalFiles()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("readme.txt", "Hello ISO");
                folder.AddFile("notes.txt", "Second file");
                folder.AddFile("data.bin", Enumerable.Range(0, 5000).Select(i => (byte)i).ToArray());

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder), out plan);

                Assert.IsTrue(File.Exists(result.OutputPath));
                Assert.AreEqual(new FileInfo(result.OutputPath).Length, result.ImageSize);
                Assert.AreEqual(0, result.ImageSize % 2048, "ISO images are a whole number of 2048-byte sectors");

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                Assert.AreEqual("TEST_DISC", label);
                Assert.AreEqual(3, files.Count);
                CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello ISO"), files["readme.txt"]);
                CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(folder.Source, "data.bin")), files["data.bin"]);
                Assert.AreEqual(0, Directory.GetFiles(folder.Output, "*.partial").Length, "No temporary files may remain");
            }
        }

        [TestMethod]
        public void NestedDirectories_StructurePreservedIncludingEmptyFolders()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"a\b\c\deep.txt", "deep");
                folder.AddFile(@"a\top.txt", "top");
                folder.AddFile(@"folder with spaces\file with spaces.txt", "spaces");
                folder.AddDirectory(@"empty\also empty");

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder), out plan);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                CollectionAssert.AreEquivalent(
                    new[] { "a", @"a\b", @"a\b\c", "empty", @"empty\also empty", "folder with spaces" },
                    dirs);
                Assert.AreEqual("deep", Encoding.UTF8.GetString(files[@"a\b\c\deep.txt"]));
                Assert.AreEqual("spaces", Encoding.UTF8.GetString(files[@"folder with spaces\file with spaces.txt"]));
                Assert.AreEqual(6, result.DirectoryCount);
                Assert.AreEqual(3, result.FileCount);
            }
        }

        [TestMethod]
        public void EmptyFolder_CreatesValidEmptyImage()
        {
            using (var folder = new TestFolder())
            {
                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder, "EMPTY"), out plan);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                Assert.AreEqual(0, files.Count);
                Assert.AreEqual(0, dirs.Count);
                Assert.AreEqual("EMPTY", label);
            }
        }

        [TestMethod]
        public void ZeroLengthFiles_AreIncluded()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("empty.txt", new byte[0]);
                folder.AddFile("full.txt", "x");

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder), out plan);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                Assert.AreEqual(0, files["empty.txt"].Length);
                Assert.AreEqual("x", Encoding.UTF8.GetString(files["full.txt"]));
            }
        }

        [TestMethod]
        public void UnicodeNames_PreservedWithJoliet()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("Café crème.txt", "accents");
                folder.AddFile(@"日本語\ファイル.txt", "japanese");
                folder.AddFile("Ελληνικά ñ ü.txt", "greek");

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder), out plan);
                Assert.AreEqual(0, plan.RenamedItems.Count);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                Assert.IsTrue(files.ContainsKey("Café crème.txt"));
                Assert.IsTrue(files.ContainsKey(@"日本語\ファイル.txt"));
                Assert.IsTrue(files.ContainsKey("Ελληνικά ñ ü.txt"));
            }
        }

        [TestMethod]
        public void UnicodeNames_MappedToIso9660NamesWithoutJoliet()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("Café.txt", "1");
                folder.AddFile("cafe.txt", "2");
                folder.AddFile("a very long file name that exceeds thirty characters.document", "3");
                folder.AddFile(@"sub.dir\x.txt", "4");

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder, "PLAIN_ISO", IsoFileSystemFormat.Iso9660), out plan);
                Assert.IsTrue(plan.RenamedItems.Count >= 4);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, false, out dirs, out label);
                Assert.AreEqual(4, files.Count, "Colliding names must not overwrite each other");
                foreach (string path in files.Keys)
                {
                    foreach (string part in path.Split('\\'))
                    {
                        Assert.IsTrue(part.Length <= 30, part);
                        Assert.IsTrue(part.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '.'), part);
                    }
                }

                CollectionAssert.AreEquivalent(new[] { "1", "2", "3", "4" }, files.Values.Select(v => Encoding.UTF8.GetString(v)).ToArray());
            }
        }

        [TestMethod]
        public void LongJolietNames_AreShortenedTo64AndImageIsReadable()
        {
            using (var folder = new TestFolder())
            {
                string longName = new string('n', 80) + ".txt";
                string longDir = new string('d', 70);
                folder.AddFile(longName, "long");
                folder.AddFile(Path.Combine(longDir, "inner.txt"), "inner");

                IsoBuildPlan plan;
                IsoCreationResult result = PrepareAndCreate(Options(folder), out plan);
                Assert.AreEqual(2, plan.RenamedItems.Count);

                List<string> dirs;
                string label;
                Dictionary<string, byte[]> files = TestFolder.ReadImageFiles(result.OutputPath, true, out dirs, out label);
                string shortened = new string('n', 60) + ".txt";
                Assert.AreEqual("long", Encoding.UTF8.GetString(files[shortened]));
                Assert.AreEqual("inner", Encoding.UTF8.GetString(files[new string('d', 64) + @"\inner.txt"]));
            }
        }

        [TestMethod]
        public void Cancellation_RemovesIncompleteOutput()
        {
            using (var folder = new TestFolder())
            {
                var random = new Random(1);
                var big = new byte[24 * 1024 * 1024];
                random.NextBytes(big);
                folder.AddFile("big.bin", big);

                var service = new IsoCreationService(TestLog);
                IsoBuildPlan plan = service.Prepare(Options(folder), null, CancellationToken.None);
                using (var cts = new CancellationTokenSource())
                {
                    var progress = new SynchronousProgress(info =>
                    {
                        if (info.Stage == IsoOperationStage.Writing && info.BytesWritten > 0)
                        {
                            cts.Cancel();
                        }
                    });

                    try
                    {
                        service.Create(plan, progress, cts.Token);
                        Assert.Fail("Expected cancellation");
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length, "Output folder must be empty after cancellation");

                // The source file handle must have been released.
                File.Delete(Path.Combine(folder.Source, "big.bin"));
            }
        }

        [TestMethod]
        public void Progress_IsMeasuredAndReaches100Percent()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.bin", new byte[3 * 1024 * 1024]);
                folder.AddFile("b.bin", new byte[2 * 1024 * 1024]);
                var reports = new List<IsoProgressInfo>();
                var service = new IsoCreationService(TestLog);
                IsoBuildPlan plan = service.Prepare(Options(folder), null, CancellationToken.None);
                IsoCreationResult result = service.Create(plan, new SynchronousProgress(reports.Add), CancellationToken.None);

                IsoProgressInfo lastWrite = reports.Last(r => r.Stage == IsoOperationStage.Writing);
                Assert.AreEqual(result.ImageSize, lastWrite.TotalBytes);
                Assert.AreEqual(lastWrite.TotalBytes, lastWrite.BytesWritten);
                Assert.AreEqual(100.0, lastWrite.Percent.Value, 0.0001);
                Assert.AreEqual(2, lastWrite.FilesProcessed);
                long previous = 0;
                foreach (IsoProgressInfo r in reports.Where(r => r.Stage == IsoOperationStage.Writing))
                {
                    Assert.IsTrue(r.BytesWritten >= previous, "Progress must never go backwards");
                    previous = r.BytesWritten;
                }
            }
        }

        [TestMethod]
        public void ExistingOutput_IsNotReplacedWithoutConfirmation()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "new");
                File.WriteAllText(folder.OutputFile(), "old content");

                var service = new IsoCreationService(TestLog);
                IsoCreationOptions options = Options(folder);
                IsoBuildPlan plan = service.Prepare(options, null, CancellationToken.None);
                Assert.ThrowsException<IsoCreationException>(() => service.Create(plan, null, CancellationToken.None));
                Assert.AreEqual("old content", File.ReadAllText(folder.OutputFile()));

                options.OverwriteExisting = true;
                IsoCreationResult result = service.Create(plan, null, CancellationToken.None);
                Assert.IsTrue(result.ImageSize > 11);
                Assert.AreEqual(1, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void InvalidOutputFolder_IsReportedAsPlanError()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                IsoCreationOptions options = Options(folder);
                options.OutputPath = Path.Combine(folder.Root, "does not exist", "out.iso");

                IsoBuildPlan plan = new IsoCreationService(TestLog).Prepare(options, null, CancellationToken.None);
                Assert.IsFalse(plan.CanCreate);
                StringAssert.Contains(plan.Errors[0], "output folder does not exist");
            }
        }

        [TestMethod]
        public void OutputInsideSource_IsRejected()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                IsoCreationOptions options = Options(folder);
                options.OutputPath = Path.Combine(folder.Source, "self.iso");

                IsoBuildPlan plan = new IsoCreationService(TestLog).Prepare(options, null, CancellationToken.None);
                Assert.IsFalse(plan.CanCreate);
                Assert.IsTrue(plan.Errors.Any(e => e.Contains("inside the source folder")));
            }
        }

        [TestMethod]
        public void MissingSourceFolder_Throws()
        {
            using (var folder = new TestFolder())
            {
                IsoCreationOptions options = Options(folder);
                options.SourceFolder = Path.Combine(folder.Root, "missing");
                Assert.ThrowsException<IsoCreationException>(() => new IsoCreationService(TestLog).Prepare(options, null, CancellationToken.None));
            }
        }

        [TestMethod]
        public void LockedFile_IsReportedBeforeWriting()
        {
            using (var folder = new TestFolder())
            {
                string locked = folder.AddFile("locked.txt", "x");
                using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    IsoBuildPlan plan = new IsoCreationService(TestLog).Prepare(Options(folder), null, CancellationToken.None);
                    Assert.IsFalse(plan.CanCreate);
                    Assert.IsTrue(plan.Errors.Any(e => e.Contains("locked.txt") && e.Contains("in use")), string.Join("; ", plan.Errors));
                }
            }
        }

        [TestMethod]
        public void FileChangedAfterScan_FailsAndRemovesOutput()
        {
            using (var folder = new TestFolder())
            {
                string path = folder.AddFile("changing.txt", "original");
                var service = new IsoCreationService(TestLog);
                IsoBuildPlan plan = service.Prepare(Options(folder), null, CancellationToken.None);

                File.AppendAllText(path, " and more");

                var ex = Assert.ThrowsException<IsoCreationException>(() => service.Create(plan, null, CancellationToken.None));
                StringAssert.Contains(ex.Message, "changing.txt");
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void FileModifiedWithSameSize_IsDetected()
        {
            using (var folder = new TestFolder())
            {
                string path = folder.AddFile("same-size.txt", "aaaa");
                var service = new IsoCreationService(TestLog);
                IsoBuildPlan plan = service.Prepare(Options(folder), null, CancellationToken.None);

                File.WriteAllText(path, "bbbb");
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

                var ex = Assert.ThrowsException<IsoCreationException>(() => service.Create(plan, null, CancellationToken.None));
                StringAssert.Contains(ex.Message, "changed");
                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void FilesOf4GBOrMore_AreRejected_AndOver2GBWarned()
        {
            using (var folder = new TestFolder())
            {
                folder.AddSparseFile("huge.bin", 5L * 1024 * 1024 * 1024);
                folder.AddSparseFile("large.bin", 3L * 1024 * 1024 * 1024);

                IsoBuildPlan plan = new IsoCreationService(TestLog).Prepare(Options(folder), null, CancellationToken.None);
                Assert.IsFalse(plan.CanCreate);
                Assert.AreEqual(1, plan.Errors.Count, string.Join("; ", plan.Errors));
                StringAssert.Contains(plan.Errors[0], "huge.bin");
                Assert.IsTrue(plan.Warnings.Any(w => w.Contains("larger than 2 GB")));
            }
        }

        [TestMethod]
        public void InvalidVolumeLabel_IsPlanError()
        {
            using (var folder = new TestFolder())
            {
                IsoBuildPlan plan = new IsoCreationService(TestLog).Prepare(Options(folder, "TOO_LONG_FOR_JOLIET"), null, CancellationToken.None);
                Assert.IsFalse(plan.CanCreate);
            }
        }

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
