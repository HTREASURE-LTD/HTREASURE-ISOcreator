using System.IO;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class SettingsServiceTests
    {
        internal static readonly LoggingService TestLog =
            new LoggingService(Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests", "logs"));

        [TestMethod]
        public void Settings_RoundTripAllValues()
        {
            using (var folder = new TestFolder())
            {
                var service = new SettingsService(Path.Combine(folder.Root, "nested", "settings.xml"), TestLog);
                var saved = new AppSettings
                {
                    DefaultOutputFolder = folder.Output,
                    DefaultVolumeLabel = "MY_DISC",
                    DefaultFormat = IsoFileSystemFormat.Iso9660,
                    RememberLastSource = false,
                    RememberLastOutput = false,
                    LastSourceFolder = folder.Source,
                    LastOutputFolder = folder.Output,
                    ShowCompletionDialog = false,
                    Theme = AppTheme.Dark
                };

                Assert.IsTrue(service.Save(saved));
                AppSettings loaded = new SettingsService(service.SettingsFile, TestLog).Load();

                Assert.AreEqual(saved.DefaultOutputFolder, loaded.DefaultOutputFolder);
                Assert.AreEqual("MY_DISC", loaded.DefaultVolumeLabel);
                Assert.AreEqual(IsoFileSystemFormat.Iso9660, loaded.DefaultFormat);
                Assert.IsFalse(loaded.RememberLastSource);
                Assert.IsFalse(loaded.RememberLastOutput);
                Assert.AreEqual(folder.Source, loaded.LastSourceFolder);
                Assert.AreEqual(folder.Output, loaded.LastOutputFolder);
                Assert.IsFalse(loaded.ShowCompletionDialog);
                Assert.AreEqual(AppTheme.Dark, loaded.Theme);
            }
        }

        [TestMethod]
        public void Settings_SaveTwiceReplacesFileAndLeavesNoTemporaryFile()
        {
            using (var folder = new TestFolder())
            {
                var service = new SettingsService(Path.Combine(folder.Root, "settings.xml"), TestLog);
                Assert.IsTrue(service.Save(new AppSettings { Theme = AppTheme.Light }));
                Assert.IsTrue(service.Save(new AppSettings { Theme = AppTheme.Dark }));
                Assert.AreEqual(AppTheme.Dark, service.Load().Theme);
                Assert.IsFalse(File.Exists(service.SettingsFile + ".tmp"));
            }
        }

        [TestMethod]
        public void Settings_MissingFileGivesDefaults()
        {
            using (var folder = new TestFolder())
            {
                AppSettings loaded = new SettingsService(Path.Combine(folder.Root, "none.xml"), TestLog).Load();
                Assert.AreEqual(IsoFileSystemFormat.Iso9660Joliet, loaded.DefaultFormat);
                Assert.IsTrue(loaded.RememberLastSource);
                Assert.IsTrue(loaded.ShowCompletionDialog);
                Assert.AreEqual(AppTheme.System, loaded.Theme);
            }
        }

        [TestMethod]
        public void Settings_CorruptFileGivesDefaults()
        {
            using (var folder = new TestFolder())
            {
                string path = Path.Combine(folder.Root, "settings.xml");
                File.WriteAllText(path, "<AppSettings><Theme>Dark</The");
                AppSettings loaded = new SettingsService(path, TestLog).Load();
                Assert.AreEqual(AppTheme.System, loaded.Theme);
            }
        }

        [TestMethod]
        public void Settings_InvalidStoredLabelIsDiscarded()
        {
            using (var folder = new TestFolder())
            {
                string path = Path.Combine(folder.Root, "settings.xml");
                File.WriteAllText(path, "<?xml version=\"1.0\"?><AppSettings><DefaultVolumeLabel>bad label!</DefaultVolumeLabel><Theme>Dark</Theme></AppSettings>");
                AppSettings loaded = new SettingsService(path, TestLog).Load();
                Assert.AreEqual(string.Empty, loaded.DefaultVolumeLabel);
                Assert.AreEqual(AppTheme.Dark, loaded.Theme);
            }
        }
    }

    [TestClass]
    public class DropValidatorTests
    {
        [TestMethod]
        public void SingleFolder_IsAccepted()
        {
            using (var folder = new TestFolder())
            {
                string error;
                Assert.AreEqual(folder.Source, DropValidator.GetSourceFolder(new[] { folder.Source }, out error));
                Assert.IsNull(error);
            }
        }

        [TestMethod]
        public void File_IsRejectedWithExplanation()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile("a.txt", "x");
                string error;
                Assert.IsNull(DropValidator.GetSourceFolder(new[] { file }, out error));
                StringAssert.Contains(error, "is a file");
            }
        }

        [TestMethod]
        public void MultipleItems_AreRejected()
        {
            using (var folder = new TestFolder())
            {
                string error;
                Assert.IsNull(DropValidator.GetSourceFolder(new[] { folder.Source, folder.Output }, out error));
                StringAssert.Contains(error, "single folder");
            }
        }

        [TestMethod]
        public void NothingOrMissing_IsRejected()
        {
            string error;
            Assert.IsNull(DropValidator.GetSourceFolder(null, out error));
            Assert.IsNotNull(error);
            Assert.IsNull(DropValidator.GetSourceFolder(new string[0], out error));
            Assert.IsNotNull(error);
            Assert.IsNull(DropValidator.GetSourceFolder(new[] { @"C:\definitely\not\here\12345" }, out error));
            StringAssert.Contains(error, "could not be found");
        }
    }
}
