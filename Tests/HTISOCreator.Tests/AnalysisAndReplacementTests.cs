using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class SourceAnalysisTests
    {
        [TestMethod]
        public void Analysis_CountsFilesFoldersAndBytes()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", new byte[100]);
                folder.AddFile(@"sub\b.txt", new byte[250]);
                folder.AddFile(@"sub\deeper\c.txt", new byte[0]);
                folder.AddDirectory(@"empty\inner");

                FolderAnalysisResult result = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                Assert.AreEqual(3, result.FileCount);
                Assert.AreEqual(4, result.DirectoryCount);
                Assert.AreEqual(350, result.TotalBytes);
                Assert.AreEqual(0, result.Errors.Count);
                CollectionAssert.AreEquivalent(new[] { "empty", @"empty\inner", "sub", @"sub\deeper" }, result.Directories);
            }
        }

        [TestMethod]
        public void Analysis_CanBeCancelled()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                using (var cts = new CancellationTokenSource())
                {
                    cts.Cancel();
                    Assert.ThrowsException<OperationCanceledException>(() => new FolderAnalysisService().Analyze(folder.Source, cts.Token));
                }
            }
        }

        [TestMethod]
        public void Analysis_ReportsUnreadableFolder()
        {
            using (var folder = new TestFolder())
            {
                string locked = folder.AddDirectory("locked");
                folder.AddFile(@"locked\secret.txt", "x");
                var rule = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.ListDirectory, AccessControlType.Deny);
                var info = new DirectoryInfo(locked);
                DirectorySecurity security = info.GetAccessControl();
                security.AddAccessRule(rule);
                info.SetAccessControl(security);
                try
                {
                    FolderAnalysisResult result = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                    Assert.AreEqual(1, result.Errors.Count, string.Join("; ", result.Errors));
                    StringAssert.Contains(result.Errors[0], "locked");
                    StringAssert.Contains(result.Errors[0], "access denied");
                }
                finally
                {
                    security.RemoveAccessRule(rule);
                    info.SetAccessControl(security);
                }
            }
        }

        [TestMethod]
        public void Analysis_SkipsJunctionsWithWarning()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"real\a.txt", "x");
                string link = Path.Combine(folder.Source, "link");
                using (Process process = Process.Start(new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + folder.Output + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                }))
                {
                    process.WaitForExit();
                    Assert.AreEqual(0, process.ExitCode, "mklink /J failed");
                }

                FolderAnalysisResult result = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                Assert.AreEqual(1, result.Warnings.Count);
                StringAssert.Contains(result.Warnings[0], "link");
                CollectionAssert.AreEqual(new[] { "real" }, result.Directories);
                Directory.Delete(link);
            }
        }

        [DataTestMethod]
        [DataRow(IsoFileSystemFormat.Iso9660Joliet)]
        [DataRow(IsoFileSystemFormat.Iso9660)]
        public void CalculatedIsoSize_EqualsActualImageSize(IsoFileSystemFormat format)
        {
            using (var folder = new TestFolder())
            {
                var random = new Random(3);
                for (int i = 0; i < 40; i++)
                {
                    var data = new byte[random.Next(0, 9000)];
                    random.NextBytes(data);
                    folder.AddFile(Path.Combine("dir" + (i % 5), "sub" + (i % 3), "file number " + i + ".bin"), data);
                }

                folder.AddFile(new string('L', 80) + ".txt", "long name");
                folder.AddFile("Café.txt", "accent");
                folder.AddDirectory(@"empty\one\two");

                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                long? calculated = IsoSizeCalculator.Calculate(analysis, format);

                var service = new IsoCreationService(SettingsServiceTests.TestLog);
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = folder.OutputFile(),
                    VolumeLabel = "SIZE_TEST",
                    Format = format,
                    FilenameMode = FilenameMode.Compatibility,
                    ConversionsApproved = true
                }, null, CancellationToken.None);
                IsoCreationResult result = service.Create(plan, null, CancellationToken.None);

                Assert.AreEqual(result.ImageSize, calculated);
            }
        }

        [TestMethod]
        public void CalculatedIsoSize_EmptyFolder()
        {
            using (var folder = new TestFolder())
            {
                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                long? size = IsoSizeCalculator.Calculate(analysis, IsoFileSystemFormat.Iso9660Joliet);
                Assert.IsTrue(size > 0 && size % 2048 == 0);
            }
        }

        [TestMethod]
        public void CalculatedIsoSize_IsNullWhenAFileIsTooLarge()
        {
            using (var folder = new TestFolder())
            {
                folder.AddSparseFile("huge.bin", 5L * 1024 * 1024 * 1024);
                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                Assert.IsNull(IsoSizeCalculator.Calculate(analysis, IsoFileSystemFormat.Iso9660Joliet));
            }
        }
    }

    [TestClass]
    public class OutputReplacementTests
    {
        /// <summary>Real file operations that throw on chosen calls, to simulate failures part-way through.</summary>
        private sealed class FaultyFileOperations : IFileOperations
        {
            public Func<string, string, bool> FailMove { get; set; } = (s, d) => false;

            public bool FailDelete { get; set; }

            public bool Exists(string path)
            {
                return File.Exists(path);
            }

            public void Move(string source, string destination)
            {
                if (FailMove(source, destination))
                {
                    throw new IOException("Simulated move failure: " + source + " -> " + destination);
                }

                File.Move(source, destination);
            }

            public void Delete(string path)
            {
                if (FailDelete)
                {
                    throw new IOException("Simulated delete failure");
                }

                File.Delete(path);
            }
        }

        private static SafeFileReplacer Replacer(IFileOperations ops)
        {
            return new SafeFileReplacer(ops, SettingsServiceTests.TestLog);
        }

        [TestMethod]
        public void NoExistingFile_IsMovedIntoPlace()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                Replacer(new FaultyFileOperations()).Publish(temp, target);
                Assert.AreEqual("new", File.ReadAllText(target));
                Assert.IsFalse(File.Exists(temp));
            }
        }

        [TestMethod]
        public void ExistingFile_IsReplacedAndBackupRemoved()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                File.WriteAllText(target, "old");
                Replacer(new FaultyFileOperations()).Publish(temp, target);
                Assert.AreEqual("new", File.ReadAllText(target));
                CollectionAssert.AreEqual(new[] { target }, Directory.GetFiles(folder.Output));
            }
        }

        [TestMethod]
        public void FailureMovingNewFile_RestoresOriginal()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                File.WriteAllText(target, "old");
                var ops = new FaultyFileOperations { FailMove = (s, d) => s == temp };

                var ex = Assert.ThrowsException<IsoCreationException>(() => Replacer(ops).Publish(temp, target));
                StringAssert.Contains(ex.Message, "original file was left unchanged");
                Assert.AreEqual("old", File.ReadAllText(target));
                Assert.AreEqual(0, Directory.GetFiles(folder.Output, "*.backup").Length);
            }
        }

        [TestMethod]
        public void FailureMovingNewFileAndRestoring_KeepsOriginalAsNamedBackup()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                File.WriteAllText(target, "old");
                var ops = new FaultyFileOperations { FailMove = (s, d) => s == temp || s.EndsWith(".backup") };

                var ex = Assert.ThrowsException<IsoCreationException>(() => Replacer(ops).Publish(temp, target));
                string backup = Directory.GetFiles(folder.Output, "*.backup").Single();
                Assert.AreEqual("old", File.ReadAllText(backup), "The original data must survive");
                StringAssert.Contains(ex.Message, backup);
            }
        }

        [TestMethod]
        public void ExistingFileInUse_IsNotTouched()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                File.WriteAllText(target, "old");

                using (new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var ex = Assert.ThrowsException<IsoCreationException>(() => Replacer(FileSystemOperations.Instance).Publish(temp, target));
                    StringAssert.Contains(ex.Message, "in use");
                }

                Assert.AreEqual("old", File.ReadAllText(target));
            }
        }

        [TestMethod]
        public void BackupThatCannotBeDeleted_DoesNotFailThePublish()
        {
            using (var folder = new TestFolder())
            {
                string temp = folder.OutputFile("new.partial");
                string target = folder.OutputFile("out.iso");
                File.WriteAllText(temp, "new");
                File.WriteAllText(target, "old");
                Replacer(new FaultyFileOperations { FailDelete = true }).Publish(temp, target);
                Assert.AreEqual("new", File.ReadAllText(target));
                Assert.AreEqual(1, Directory.GetFiles(folder.Output, "*.backup").Length);
            }
        }

        [TestMethod]
        public void CreateIso_FinalisationFailure_KeepsExistingIsoAndRemovesPartialFile()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "new content");
                string target = folder.OutputFile();
                File.WriteAllText(target, "previous image");

                var ops = new FaultyFileOperations { FailMove = (s, d) => s.EndsWith(".partial") };
                var service = new IsoCreationService(SettingsServiceTests.TestLog, ops);
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = target,
                    VolumeLabel = "TEST",
                    OverwriteExisting = true
                }, null, CancellationToken.None);

                Assert.ThrowsException<IsoCreationException>(() => service.Create(plan, null, CancellationToken.None));
                Assert.AreEqual("previous image", File.ReadAllText(target));
                CollectionAssert.AreEqual(new[] { target }, Directory.GetFiles(folder.Output));
            }
        }

        [TestMethod]
        public void CreateIso_CancelledWhileReplacing_KeepsExistingIso()
        {
            using (var folder = new TestFolder())
            {
                var data = new byte[16 * 1024 * 1024];
                new Random(5).NextBytes(data);
                folder.AddFile("big.bin", data);
                string target = folder.OutputFile();
                File.WriteAllText(target, "previous image");

                var service = new IsoCreationService(SettingsServiceTests.TestLog);
                IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                {
                    SourceFolder = folder.Source,
                    OutputPath = target,
                    VolumeLabel = "TEST",
                    OverwriteExisting = true
                }, null, CancellationToken.None);

                using (var cts = new CancellationTokenSource())
                {
                    var progress = new CallbackProgress(info =>
                    {
                        if (info.Stage == IsoOperationStage.Writing && info.BytesWritten > 0)
                        {
                            cts.Cancel();
                        }
                    });
                    Assert.ThrowsException<OperationCanceledException>(() => service.Create(plan, progress, cts.Token));
                }

                Assert.AreEqual("previous image", File.ReadAllText(target));
                CollectionAssert.AreEqual(new[] { target }, Directory.GetFiles(folder.Output));
            }
        }

        [TestMethod]
        public void CreateIso_CancelledBeforePrepare_Throws()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                using (var cts = new CancellationTokenSource())
                {
                    cts.Cancel();
                    Assert.ThrowsException<OperationCanceledException>(() => new IsoCreationService(SettingsServiceTests.TestLog).Prepare(new IsoCreationOptions
                    {
                        SourceFolder = folder.Source,
                        OutputPath = folder.OutputFile(),
                        VolumeLabel = "TEST"
                    }, null, cts.Token));
                }

                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }

        private sealed class CallbackProgress : IProgress<IsoProgressInfo>
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
