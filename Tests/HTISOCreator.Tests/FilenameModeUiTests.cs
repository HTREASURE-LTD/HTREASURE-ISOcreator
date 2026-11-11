using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Forms;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// Stage 3.2 user interface: the Filename Compatibility section, Strict Mode rejection through
    /// the real Create button, and the Compatibility Mode confirmation.
    /// </summary>
    [TestClass]
    public class FilenameModeUiTests
    {
        private static AppSettings Settings(FilenameMode mode)
        {
            return new AppSettings
            {
                RememberLastSource = false,
                RememberLastOutput = false,
                ShowCompletionDialog = false,
                AutoVerify = true,
                VerifyHashes = true,
                Theme = AppTheme.Light,
                FilenameMode = mode
            };
        }

        private static async Task<CompatibilityForm> WaitForCompatibilityWindow()
        {
            await UiStateTests.WaitUntil(() => Application.OpenForms.OfType<CompatibilityForm>().Any(f => f.Visible));
            return Application.OpenForms.OfType<CompatibilityForm>().First();
        }

        [TestMethod]
        public void MainForm_ShowsModeAndPreservationStatus_AndSavesTheMode()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"bin\" + FilenameModeTests.MsTestDll, "dll");
                folder.AddFile("ok.txt", "x");
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, Settings(FilenameMode.Strict)))
                    {
                        var strict = UiStateTests.Find<RadioButton>(form, "strictModeRadioButton");
                        var compatibility = UiStateTests.Find<RadioButton>(form, "compatibilityModeRadioButton");
                        var status = UiStateTests.Find<Label>(form, "filenameStatusLabel");
                        Assert.IsTrue(strict.Checked);
                        StringAssert.Contains(strict.Text.Replace("&", string.Empty), "Strict Mode");
                        StringAssert.Contains(compatibility.Text.Replace("&", string.Empty), "Compatibility Mode");
                        Assert.IsFalse(UiStateTests.Find<Button>(form, "exportNamesButton").Enabled, "Nothing to export before a scan");

                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        StringAssert.Contains(status.Text, "1 name(s) cannot be preserved (1 program file(s)). Creation is blocked.");
                        Assert.IsTrue(UiStateTests.Find<Button>(form, "exportNamesButton").Enabled);

                        compatibility.Checked = true;
                        StringAssert.Contains(status.Text, "1 name(s) will be converted (1 program file(s)). Confirmation required.");
                        Assert.AreEqual(FilenameMode.Compatibility,
                            new SettingsService(Path.Combine(folder.Root, "settings.xml"), SettingsServiceTests.TestLog).Load().FilenameMode,
                            "The selected mode is saved immediately");

                        File.Delete(Path.Combine(folder.Source, "bin", FilenameModeTests.MsTestDll));
                        await form.ScanSourceAsync(true);
                        StringAssert.Contains(status.Text, "Every file and folder name (2) will be preserved exactly.");
                    }
                });
            }
        }

        [TestMethod]
        public void MainForm_StrictModeRejectsAndKeepsExistingIso()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"Tests\bin\Release\net462\" + FilenameModeTests.MsTestDll, "dll");
                string output = folder.OutputFile("existing.iso");
                File.WriteAllBytes(output, Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray());
                byte[] before = File.ReadAllBytes(output);

                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, Settings(FilenameMode.Strict)))
                    {
                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        UiStateTests.Find<TextBox>(form, "outputTextBox").Text = output;
                        form.ConfirmOverwriteForTests(output);

                        UiStateTests.Find<Button>(form, "createButton").PerformClick();
                        CompatibilityForm review = await WaitForCompatibilityWindow();
                        StringAssert.StartsWith(UiStateTests.Find<Label>(review, "summaryLabel").Text, "Strict Mode: 1 name(s) cannot be preserved");
                        Assert.IsFalse(UiStateTests.Find<Button>(review, "createButton").Visible, "Strict Mode offers no way to continue");
                        Assert.IsFalse(UiStateTests.Find<CheckBox>(review, "confirmCheckBox").Visible);
                        var renamed = UiStateTests.Find<ListView>(review, "renamesListView").Items[0];
                        Assert.AreEqual(FilenameModeTests.MsTestDll, renamed.SubItems[0].Text, "Original name");
                        Assert.AreEqual(FilenameModeTests.MsTestDllInImage, renamed.SubItems[1].Text, "Proposed conversion");
                        StringAssert.Contains(renamed.SubItems[2].Text, "longer than 64");
                        UiStateTests.Find<Button>(review, "closeButton").PerformClick();

                        await UiStateTests.WaitUntil(() => !form.IsBusy && form.History.Load().Count == 1);
                        HistoryEntry entry = form.History.Load().Single();
                        Assert.AreEqual(CreationOutcome.Failed, entry.Outcome);
                        StringAssert.StartsWith(entry.Message, "Strict Mode:");
                        Assert.AreEqual(FilenameMode.Strict, entry.FilenameMode);
                        StringAssert.Contains(UiStateTests.Find<Label>(form, "statusLabel").Text, "Strict Mode: the ISO image was not created");
                    }
                });

                CollectionAssert.AreEqual(before, File.ReadAllBytes(output), "The existing ISO is untouched");
                Assert.AreEqual(1, Directory.GetFiles(folder.Output).Length);
            }
        }

        [TestMethod]
        public void MainForm_CompatibilityModeRequiresConfirmationThenCreatesAndVerifies()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(@"Tests\bin\Debug\net462\" + FilenameModeTests.MsTestDll, "debug");
                folder.AddFile(@"Tests\bin\Release\net462\" + FilenameModeTests.MsTestDll, "release");
                folder.AddFile("readme.txt", "x");

                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, Settings(FilenameMode.Compatibility)))
                    {
                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        UiStateTests.Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile("compat.iso");

                        UiStateTests.Find<Button>(form, "createButton").PerformClick();
                        CompatibilityForm review = await WaitForCompatibilityWindow();
                        var confirm = UiStateTests.Find<CheckBox>(review, "confirmCheckBox");
                        var create = UiStateTests.Find<Button>(review, "createButton");
                        Assert.IsTrue(confirm.Visible);
                        Assert.IsFalse(confirm.Checked);
                        StringAssert.Contains(confirm.Text, "2 are DLL or program files, which may stop working when renamed");
                        Assert.IsFalse(create.Enabled, "Create stays disabled until the conversions are confirmed");
                        Assert.AreEqual(2, UiStateTests.Find<ListView>(review, "renamesListView").Items.Count);

                        confirm.Checked = true;
                        Assert.IsTrue(create.Enabled);
                        create.PerformClick();

                        await UiStateTests.WaitUntil(() => !form.IsBusy && form.History.Load().Count == 1);
                        HistoryEntry entry = form.History.Load().Single();
                        Assert.AreEqual(CreationOutcome.Succeeded, entry.Outcome, entry.Message);
                        Assert.AreEqual(HistoryVerificationStatus.Passed, entry.Verification, entry.VerificationDetail);
                        Assert.AreEqual(FilenameMode.Compatibility, entry.FilenameMode);
                        IsoImageInfo info = IsoImageReader.Read(entry.OutputPath, System.Threading.CancellationToken.None);
                        Assert.IsTrue(info.Entries.Any(e => e.Path.EndsWith(FilenameModeTests.MsTestDllInImage, StringComparison.Ordinal)));
                    }
                });
            }
        }

        [TestMethod]
        public void CompatibilityForm_CancelInCompatibilityModeCreatesNothing()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(FilenameModeTests.MsTestDll, "dll");
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, Settings(FilenameMode.Compatibility)))
                    {
                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        UiStateTests.Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile("never.iso");

                        UiStateTests.Find<Button>(form, "createButton").PerformClick();
                        CompatibilityForm review = await WaitForCompatibilityWindow();
                        UiStateTests.Find<Button>(review, "closeButton").PerformClick();
                        await UiStateTests.WaitUntil(() => !form.IsBusy);
                        Assert.AreEqual(0, form.History.Load().Count, "A cancelled review is not an attempt");
                    }
                });

                Assert.AreEqual(0, Directory.GetFiles(folder.Output).Length);
            }
        }
    }
}
