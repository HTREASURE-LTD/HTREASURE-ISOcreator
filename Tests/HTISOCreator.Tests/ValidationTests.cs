using System.IO;
using System.Linq;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    [TestClass]
    public class ValidationTests
    {
        [DataTestMethod]
        [DataRow("LINUX_TOOLS", IsoFileSystemFormat.Iso9660Joliet, true)]
        [DataRow("ABCDEFGHIJKLMNOP", IsoFileSystemFormat.Iso9660Joliet, true)]
        [DataRow("ABCDEFGHIJKLMNOPQ", IsoFileSystemFormat.Iso9660Joliet, false)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345", IsoFileSystemFormat.Iso9660, true)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456", IsoFileSystemFormat.Iso9660, false)]
        [DataRow("MY DISC", IsoFileSystemFormat.Iso9660Joliet, false)]
        [DataRow("lower", IsoFileSystemFormat.Iso9660Joliet, false)]
        [DataRow("", IsoFileSystemFormat.Iso9660Joliet, false)]
        public void VolumeLabel_Validation(string label, IsoFileSystemFormat format, bool valid)
        {
            Assert.AreEqual(valid, VolumeLabelValidator.Validate(label, format) == null);
        }

        [DataTestMethod]
        [DataRow("Linux Tools", "LINUX_TOOLS")]
        [DataRow("Café (v2)", "CAFE_V2")]
        [DataRow("日本語", "ISO_IMAGE")]
        [DataRow("a-very-long-folder-name-indeed", "A_VERY_LONG_FOLD")]
        public void VolumeLabel_SuggestionIsValid(string folder, string expected)
        {
            string suggestion = VolumeLabelValidator.Suggest(folder, IsoFileSystemFormat.Iso9660Joliet);
            Assert.AreEqual(expected, suggestion);
            Assert.IsNull(VolumeLabelValidator.Validate(suggestion, IsoFileSystemFormat.Iso9660Joliet));
        }

        [TestMethod]
        public void OutputPath_RequiresIsoExtensionAndExistingFolder()
        {
            string temp = Path.GetTempPath();
            Assert.IsNull(PathValidator.ValidateOutputFile(Path.Combine(temp, "x.iso"), null));
            Assert.IsNotNull(PathValidator.ValidateOutputFile(Path.Combine(temp, "x.zip"), null));
            Assert.IsNotNull(PathValidator.ValidateOutputFile(Path.Combine(temp, "no such folder 123", "x.iso"), null));
            Assert.IsNotNull(PathValidator.ValidateOutputFile("relative.iso", null));
            Assert.AreEqual(@"C:\a\b.iso", PathValidator.EnsureIsoExtension(@"C:\a\b"));
            Assert.AreEqual(@"C:\a\b.ISO", PathValidator.EnsureIsoExtension(@"C:\a\b.ISO"));
        }

        [TestMethod]
        public void OutputPath_SiblingFolderWithSamePrefixIsNotInsideSource()
        {
            Assert.IsFalse(PathValidator.IsSameOrInside(@"C:\data2\x.iso", @"C:\data"));
            Assert.IsTrue(PathValidator.IsSameOrInside(@"C:\data\sub\x.iso", @"C:\data"));
        }

        [TestMethod]
        public void JolietMapper_ReplacesInvalidCharactersAndShortens()
        {
            Assert.AreEqual("a_b.txt", IsoNameMapper.MapComponent("a;b.txt", false, IsoFileSystemFormat.Iso9660Joliet));
            string mapped = IsoNameMapper.MapComponent(new string('x', 100) + ".jpeg", false, IsoFileSystemFormat.Iso9660Joliet);
            Assert.AreEqual(64, mapped.Length);
            Assert.IsTrue(mapped.EndsWith(".jpeg"));
        }

        [TestMethod]
        public void JolietMapper_DoesNotSplitSurrogatePairs()
        {
            string emoji = "\U0001F600";
            string name = new string('a', 63) + emoji + ".txt";
            string mapped = IsoNameMapper.MapComponent(name, false, IsoFileSystemFormat.Iso9660Joliet);
            Assert.IsTrue(mapped.Length <= 64);
            Assert.IsFalse(char.IsHighSurrogate(mapped[mapped.Length - 5]));
        }

        [TestMethod]
        public void Iso9660Mapper_ProducesUniqueCompliantNames()
        {
            IsoNameMapper.MappingResult result = IsoNameMapper.Map(
                new[] { "Dir.One", "dir one" },
                new[] { "Report final.docx", "report-final.docx", @"Dir.One\x.txt" },
                IsoFileSystemFormat.Iso9660);

            Assert.AreEqual(2, result.Directories.Values.Distinct().Count());
            Assert.AreEqual(3, result.Files.Values.Distinct().Count());
            Assert.AreEqual("REPORT_FINAL.DOCX", result.Files["Report final.docx"]);
            Assert.AreEqual("REPORT_FINAL_1.DOCX", result.Files["report-final.docx"]);
            Assert.AreEqual(result.Directories["Dir.One"] + @"\X.TXT", result.Files[@"Dir.One\x.txt"]);
        }

        [TestMethod]
        public void FileSizeFormatter_Formats()
        {
            Assert.AreEqual("0 bytes", FileSizeFormatter.Format(0));
            Assert.AreEqual("1.00 KB", FileSizeFormatter.Format(1024));
            Assert.AreEqual("4.38 GB", FileSizeFormatter.Format(4700000000));
        }
    }
}
