using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Forms;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>Stage 3.3 user interface: choosing UDF in the main window, ISO Information and Settings.</summary>
    [TestClass]
    public class UdfUiTests
    {
        private static AppSettings Settings(IsoFileSystemFormat format)
        {
            return new AppSettings
            {
                RememberLastSource = false,
                RememberLastOutput = false,
                ShowCompletionDialog = false,
                AutoVerify = true,
                VerifyHashes = true,
                Theme = AppTheme.Light,
                FilenameMode = FilenameMode.Strict,
                DefaultFormat = format
            };
        }

        private static void Select(MainForm form, IsoFileSystemFormat format)
        {
            UiStateTests.Find<ComboBox>(form, "fileSystemComboBox").SelectedIndex = FileSystemFormats.All.ToList().IndexOf(format);
        }

        [TestMethod]
        public void MainForm_UdfKeepsMsTestDllInStrictMode_CreatesAndVerifies()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"Tests\bin\Release\net462\" + FilenameModeTests.MsTestDll, "dll");
                folder.AddFile("readme.txt", "x");
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, Settings(IsoFileSystemFormat.Iso9660Joliet)))
                    {
                        var combo = UiStateTests.Find<ComboBox>(form, "fileSystemComboBox");
                        Assert.AreEqual(4, combo.Items.Count);
                        Assert.AreEqual(IsoFileSystemFormat.Iso9660Joliet, FileSystemFormats.All[combo.SelectedIndex], "Saved default is selected");

                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        var status = UiStateTests.Find<Label>(form, "filenameStatusLabel");
                        StringAssert.Contains(status.Text, "1 name(s) cannot be preserved");

                        Select(form, IsoFileSystemFormat.Udf);
                        await UiStateTests.WaitUntil(() => UiStateTests.Find<Label>(form, "isoSizeValue").Text.Contains("bytes"));
                        StringAssert.Contains(status.Text, "will be preserved exactly", "UDF keeps the 70-character DLL name");
                        Assert.AreEqual("UDF", UiStateTests.Find<Label>(form, "formatValue").Text);

                        UiStateTests.Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile("udf-ui.iso");
                        UiStateTests.Find<Button>(form, "createButton").PerformClick();
                        await UiStateTests.WaitUntil(() => !form.IsBusy && form.History.Load().Count == 1);

                        HistoryEntry entry = form.History.Load().Single();
                        Assert.AreEqual(CreationOutcome.Succeeded, entry.Outcome, entry.Message);
                        Assert.AreEqual(IsoFileSystemFormat.Udf, entry.Format);
                        Assert.AreEqual("UDF", entry.FormatName);
                        Assert.AreEqual(HistoryVerificationStatus.Passed, entry.Verification, entry.VerificationDetail);
                        IsoImageInfo info = IsoImageReader.Read(entry.OutputPath, CancellationToken.None);
                        Assert.IsTrue(info.Entries.Any(e => e.Path == @"Tests\bin\Release\net462\" + FilenameModeTests.MsTestDll));
                    }
                });
            }
        }

        [TestMethod]
        public void IsoInfoForm_DescribesUdfAndHybridImages()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"docs\" + FilenameModeTests.MsTestDll, "x");
                var service = new IsoCreationService(SettingsServiceTests.TestLog);
                string Make(IsoFileSystemFormat format)
                {
                    IsoBuildPlan plan = service.Prepare(new IsoCreationOptions
                    {
                        SourceFolder = folder.Source,
                        OutputPath = folder.OutputFile(format + ".iso"),
                        VolumeLabel = "INFO",
                        Format = format
                    }, null, CancellationToken.None);
                    return service.Create(plan, null, CancellationToken.None).OutputPath;
                }

                IsoImageInfo udf = IsoImageReader.Read(Make(IsoFileSystemFormat.Udf), CancellationToken.None);
                IsoImageInfo hybrid = IsoImageReader.Read(Make(IsoFileSystemFormat.Iso9660JolietUdf), CancellationToken.None);
                Assert.AreEqual("UDF 1.02 only", IsoInfoForm.DescribeFileSystems(udf));
                Assert.AreEqual("ISO 9660 + Joliet + UDF 1.02", IsoInfoForm.DescribeFileSystems(hybrid));
                Assert.AreEqual("INFO", udf.DisplayLabel);

                UiStateTests.RunSta(async () =>
                {
                    using (var form = new IsoInfoForm(udf.FilePath) { StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ShowInTaskbar = false })
                    {
                        form.Show();
                        await UiStateTests.WaitUntil(() => UiStateTests.Find<Label>(form, "fileSystemValue").Text != "--");
                        Assert.AreEqual("UDF 1.02 only", UiStateTests.Find<Label>(form, "fileSystemValue").Text);
                        StringAssert.Contains(UiStateTests.Find<Label>(form, "treeLabel").Text, "UDF names");
                    }
                });
            }
        }

        [TestMethod]
        public void SettingsForm_OffersAllFourFileSystems()
        {
            UiStateTests.RunSta(async () =>
            {
                var settings = new AppSettings { DefaultFormat = IsoFileSystemFormat.Iso9660JolietUdf };
                using (var form = new SettingsForm(settings) { StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ShowInTaskbar = false })
                {
                    form.Show();
                    Application.DoEvents();
                    var combo = UiStateTests.Find<ComboBox>(form, "formatComboBox");
                    Assert.AreEqual(4, combo.Items.Count);
                    Assert.AreEqual(FileSystemFormats.Describe(IsoFileSystemFormat.Iso9660JolietUdf), combo.SelectedItem);
                }

                await Task.CompletedTask;
            });
        }
    }
}
