using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// The plain ISO 9660 tree must have valid, unique names in every folder, even when
    /// DiscUtils's own name generator would produce duplicates.
    /// </summary>
    [TestClass]
    public class ShortNameTests
    {
        private static readonly Regex ValidFile = new Regex(@"^[A-Z0-9_]+(\.[A-Z0-9_]+)?$");
        private static readonly Regex ValidDirectory = new Regex(@"^[A-Z0-9_]+$");

        /// <summary>Names that DiscUtils 0.16.13 turns into duplicates or loses extensions for.</summary>
        private static void AddCollidingContent(TestFolder folder)
        {
            folder.AddFile(@"obj\HTISOCreator.csproj.nuget.g.props", "props");
            folder.AddFile(@"obj\HTISOCreator.csproj.nuget.g.targets", "targets");
            folder.AddFile(@"obj\HTISOCreator.csproj.nuget.dgspec.json", "json");
            folder.AddFile(@"bin\Microsoft.TestPlatform.CoreUtilities.dll", "a");
            folder.AddFile(@"bin\Microsoft.TestPlatform.PlatformAbstractions.dll", "b");
            folder.AddFile(@"bin\Microsoft.TestPlatform.CoreUtilities.xml", "c");
            folder.AddFile(@"bin\Microsoft.VisualStudio.TestPlatform.Common.dll", "d");
            folder.AddFile(@"Shared Documents For The Project 2025\a.txt", "1");
            folder.AddFile(@"Shared Documents For The Project 2026\b.txt", "2");
            folder.AddFile(@"Some.Folder.With.Dots\c.txt", "3");
            folder.AddFile("README.TXT", "already valid");
            folder.AddFile("noextension", "x");
            folder.AddDirectory("NOEXTENSION_DIR");
        }

        [TestMethod]
        public void DiscUtilsShortNameField_IsAvailable()
        {
            Assert.IsTrue(IsoShortNames.IsSupported,
                "DiscUtils changed: the ShortName backing field is missing, so ISO 9660 names cannot be made unique.");
        }

        [TestMethod]
        public void CollidingNames_AreUniqueValidAndStructurallyClean()
        {
            using (var folder = new TestFolder())
            {
                AddCollidingContent(folder);
                string iso = VerificationTests.CreateIso(folder);
                IsoImageInfo info = IsoImageReader.Read(iso, CancellationToken.None);

                Assert.AreEqual(0, info.Problems.Count, string.Join(Environment.NewLine, info.Problems));
                Assert.AreEqual(info.Entries.Count, info.PrimaryEntries.Count);

                foreach (IGrouping<string, IsoImageEntry> group in info.PrimaryEntries.GroupBy(e => Path.GetDirectoryName(e.Path)))
                {
                    var names = group.Select(e => Path.GetFileName(e.Path)).ToList();
                    Assert.AreEqual(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                        "Duplicate names in '" + group.Key + "': " + string.Join(", ", names));
                }

                foreach (IsoImageEntry entry in info.PrimaryEntries)
                {
                    string name = Path.GetFileName(entry.Path);
                    Assert.IsTrue(name.Length <= IsoShortNames.MaxNameLength + (entry.IsDirectory ? 0 : 1), "Too long: " + name);
                    Assert.IsTrue((entry.IsDirectory ? ValidDirectory : ValidFile).IsMatch(name), "Not a valid ISO 9660 name: " + name);
                }

                List<string> binFiles = info.PrimaryEntries.Where(e => e.Path.StartsWith(@"BIN\", StringComparison.Ordinal))
                    .Select(e => Path.GetFileName(e.Path)).ToList();
                Assert.AreEqual(3, binFiles.Count(n => n.EndsWith(".DLL", StringComparison.Ordinal)), "Real extensions must be kept: " + string.Join(", ", binFiles));
                Assert.AreEqual(1, binFiles.Count(n => n.EndsWith(".XML", StringComparison.Ordinal)));
                Assert.IsTrue(info.PrimaryEntries.Any(e => e.Path == "README.TXT"), "An already-valid name must not change");

                // The Joliet names Windows shows are unchanged.
                Assert.IsTrue(info.Entries.Any(e => e.Path == @"obj\HTISOCreator.csproj.nuget.g.targets"));
                Assert.IsTrue(info.Entries.Any(e => e.Path == @"Some.Folder.With.Dots\c.txt"));

                VerificationResult result = new IsoVerificationService(SettingsServiceTests.TestLog, new WindowsMountChecker(SettingsServiceTests.TestLog))
                    .Verify(new VerificationOptions { IsoPath = iso, SourceFolder = folder.Source, CompareHashes = true }, null, null, CancellationToken.None);
                Assert.IsTrue(result.Passed, result.BuildReport());
            }
        }

        [TestMethod]
        public void SizeCalculation_MatchesImageWithCorrectedNames()
        {
            using (var folder = new TestFolder())
            {
                AddCollidingContent(folder);
                FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(folder.Source, CancellationToken.None);
                long? predicted = IsoSizeCalculator.Calculate(analysis, IsoFileSystemFormat.Iso9660Joliet);
                string iso = VerificationTests.CreateIso(folder);
                Assert.AreEqual(new FileInfo(iso).Length, predicted);
            }
        }

        [TestMethod]
        public void Iso9660OnlyImage_HasNoProblems()
        {
            using (var folder = new TestFolder())
            {
                AddCollidingContent(folder);
                IsoImageInfo info = IsoImageReader.Read(VerificationTests.CreateIso(folder, IsoFileSystemFormat.Iso9660), CancellationToken.None);
                Assert.AreEqual(0, info.Problems.Count, string.Join(Environment.NewLine, info.Problems));
                Assert.IsFalse(info.HasJoliet);
            }
        }

        [TestMethod]
        public void MakeFileName_AddsCounterWithinLengthLimit()
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            string longName = new string('a', 60) + ".extension";
            string first = IsoShortNames.MakeFileName(longName, used);
            string second = IsoShortNames.MakeFileName(longName, used);
            string third = IsoShortNames.MakeFileName(longName, used);

            Assert.AreEqual(new string('A', 22) + ".EXTENSIO;1", first);
            Assert.AreEqual(new string('A', 20) + "_1.EXTENSIO;1", second);
            Assert.AreEqual(new string('A', 20) + "_2.EXTENSIO;1", third);
        }

        [TestMethod]
        public void MakeDirectoryName_RemovesDotsAndShortens()
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            Assert.AreEqual("SOME_FOLDER", IsoShortNames.MakeDirectoryName("Some.Folder", used));
            Assert.AreEqual("SOME_FOLDER_1", IsoShortNames.MakeDirectoryName("some folder", used));
            Assert.AreEqual(30, IsoShortNames.MakeDirectoryName(new string('x', 50), used).Length);
        }
    }
}
