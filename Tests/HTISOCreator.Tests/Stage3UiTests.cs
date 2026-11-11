using System;
using System.Drawing;
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
    /// <summary>
    /// Drives the Stage 3 windows and the integrated create-and-verify workflow on an STA thread.
    /// </summary>
    [TestClass]
    public class Stage3UiTests
    {
        private static T OffScreen<T>(T form) where T : Form
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-5000, -5000);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            return form;
        }

        private static AppSettings QuietSettings()
        {
            return new AppSettings
            {
                RememberLastSource = false,
                RememberLastOutput = false,
                ShowCompletionDialog = false,
                AutoVerify = true,
                VerifyHashes = true,
                Theme = AppTheme.Light
            };
        }

        [TestMethod]
        public void MainForm_CreatesVerifiesAndRecordsHistory()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("one.txt", "first");
                folder.AddFile(@"sub\two.bin", new byte[50000]);
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, QuietSettings()))
                    {
                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        UiStateTests.Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile("ui.iso");

                        UiStateTests.Find<Button>(form, "createButton").PerformClick();
                        await UiStateTests.WaitUntil(() => !form.IsBusy && form.History.Load().Count == 1);

                        HistoryEntry entry = form.History.Load().Single();
                        Assert.AreEqual(CreationOutcome.Succeeded, entry.Outcome, entry.Message);
                        Assert.AreEqual(HistoryVerificationStatus.Passed, entry.Verification, entry.VerificationDetail);
                        StringAssert.Contains(entry.VerificationDetail, "Full source-content verification");
                        Assert.AreEqual(folder.OutputFile("ui.iso"), entry.OutputPath);
                        Assert.AreEqual(2, entry.FileCount);
                        Assert.IsFalse(string.IsNullOrEmpty(entry.LogReference));
                        Assert.IsTrue(File.Exists(entry.OutputPath));
                        StringAssert.Contains(UiStateTests.Find<Label>(form, "statusLabel").Text, "verified");
                    }
                });
            }
        }

        [TestMethod]
        public void MainForm_ShowsCapacityStatusAndRemembersProfile()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("data.bin", new byte[3 * 1024 * 1024]);
                UiStateTests.RunSta(async () =>
                {
                    MainForm form = UiStateTests.OpenForm(folder, QuietSettings());
                    UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                    await form.ScanSourceAsync(false);

                    var combo = UiStateTests.Find<ComboBox>(form, "capacityComboBox");
                    var status = UiStateTests.Find<Label>(form, "capacityStatusLabel");
                    combo.SelectedItem = CapacityProfile.Find("cd650");
                    StringAssert.StartsWith(status.Text, "Fits:");
                    Assert.AreEqual(CapacityState.Fits, form.CurrentCapacity.State);

                    combo.SelectedItem = CapacityProfile.Find(CapacityProfile.CustomId);
                    Assert.IsTrue(UiStateTests.Find<NumericUpDown>(form, "customCapacityUpDown").Visible);
                    UiStateTests.Find<NumericUpDown>(form, "customCapacityUpDown").Value = 1;
                    StringAssert.StartsWith(status.Text, "Too large");
                    Assert.AreEqual(CapacityState.Exceeds, form.CurrentCapacity.State);

                    form.Close();
                    form.Dispose();
                });

                AppSettings saved = new SettingsService(Path.Combine(folder.Root, "settings.xml"), SettingsServiceTests.TestLog).Load();
                Assert.AreEqual(CapacityProfile.CustomId, saved.CapacityProfileId);
                Assert.AreEqual(1, saved.CustomCapacityMegabytes);
            }
        }

        [TestMethod]
        public void MainForm_CompatibilityCheckPreviewsWithoutWriting()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile(new string('x', 90) + ".txt", "long");
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder, QuietSettings()))
                    {
                        UiStateTests.Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        CompatibilityReport report = await form.CheckCompatibilityAsync(false);

                        Assert.AreEqual(1, report.Renames.Count);
                        Assert.IsTrue(report.NeedsReview);
                        Assert.IsFalse(form.IsBusy);
                        Assert.AreEqual(0, Directory.GetFiles(folder.Root, "*.iso", SearchOption.AllDirectories).Length);
                    }
                });
            }
        }

        [TestMethod]
        public void VerificationForm_VerifiesAndCanBeCancelled()
        {
            using (var folder = new TestFolder())
            {
                var data = new byte[24 * 1024 * 1024];
                new Random(2).NextBytes(data);
                folder.AddFile("big.bin", data);
                string iso = VerificationTests.CreateIso(folder);

                UiStateTests.RunSta(async () =>
                {
                    var service = new IsoVerificationService(SettingsServiceTests.TestLog, new WindowsMountChecker(SettingsServiceTests.TestLog));
                    using (VerificationForm form = OffScreen(new VerificationForm(service, iso, folder.Source, false, false)))
                    {
                        VerificationResult result = await form.RunVerificationAsync();
                        Assert.IsTrue(result.Passed);
                        Assert.IsTrue(UiStateTests.Find<ListView>(form, "checksListView").Items.Count >= 6);
                        Assert.IsTrue(UiStateTests.Find<Button>(form, "saveReportButton").Enabled);

                        UiStateTests.Find<CheckBox>(form, "hashCheckBox").Checked = true;
                        Task<VerificationResult> running = form.RunVerificationAsync();
                        await UiStateTests.WaitUntil(() => form.IsRunning);
                        form.RequestCancel();
                        Assert.IsNull(await running);
                        StringAssert.Contains(UiStateTests.Find<Label>(form, "summaryLabel").Text, "cancelled");
                        Assert.IsFalse(form.IsRunning);
                    }
                });
            }
        }

        [TestMethod]
        public void VerificationForm_ShowsFailuresForModifiedSource()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "aaaa");
                string iso = VerificationTests.CreateIso(folder);
                File.WriteAllText(Path.Combine(folder.Source, "a.txt"), "bbbb");

                UiStateTests.RunSta(async () =>
                {
                    var service = new IsoVerificationService(SettingsServiceTests.TestLog, new WindowsMountChecker(SettingsServiceTests.TestLog));
                    using (VerificationForm form = OffScreen(new VerificationForm(service, iso, folder.Source, true, false)))
                    {
                        VerificationResult result = await form.RunVerificationAsync();
                        Assert.IsFalse(result.Passed);
                        ListView differences = UiStateTests.Find<ListView>(form, "differencesListView");
                        Assert.AreEqual("Different content", differences.Items.Cast<ListViewItem>().Single().Text);
                        StringAssert.StartsWith(UiStateTests.Find<Label>(form, "summaryLabel").Text, "FAILED");
                    }
                });
            }
        }

        [TestMethod]
        public void HistoryForm_SearchesFiltersAndSorts()
        {
            using (var folder = new TestFolder())
            {
                var history = new HistoryService(Path.Combine(folder.Root, "history.xml"), SettingsServiceTests.TestLog);
                history.Add(new HistoryEntry { Timestamp = DateTime.Now.AddHours(-3), Outcome = CreationOutcome.Succeeded, OutputPath = @"D:\alpha.iso", VolumeLabel = "ALPHA", ImageSize = 300 });
                history.Add(new HistoryEntry { Timestamp = DateTime.Now.AddHours(-2), Outcome = CreationOutcome.Failed, OutputPath = @"D:\beta.iso", VolumeLabel = "BETA", Message = "Disk full" });
                history.Add(new HistoryEntry { Timestamp = DateTime.Now.AddHours(-1), Outcome = CreationOutcome.Succeeded, OutputPath = @"D:\gamma.iso", VolumeLabel = "GAMMA", ImageSize = 100, Verification = HistoryVerificationStatus.Passed });

                UiStateTests.RunSta(async () =>
                {
                    var verifier = new IsoVerificationService(SettingsServiceTests.TestLog, new WindowsMountChecker(SettingsServiceTests.TestLog));
                    using (HistoryForm form = OffScreen(new HistoryForm(history, verifier, false, false)))
                    {
                        Assert.AreEqual("gamma.iso", Path.GetFileName(form.VisibleEntries[0].OutputPath), "Newest first by default");
                        Assert.AreEqual(3, form.VisibleEntries.Count);

                        form.SetSearch("disk FULL");
                        Assert.AreEqual("BETA", form.VisibleEntries.Single().VolumeLabel);
                        form.SetSearch(string.Empty);

                        form.SetFilter(2);
                        Assert.AreEqual(CreationOutcome.Failed, form.VisibleEntries.Single().Outcome);
                        form.SetFilter(4);
                        Assert.AreEqual("GAMMA", form.VisibleEntries.Single().VolumeLabel);
                        form.SetFilter(0);

                        form.SortBy(5);
                        CollectionAssert.AreEqual(new long[] { 0, 100, 300 }, form.VisibleEntries.Select(e => e.ImageSize).ToArray());
                        form.SortBy(5);
                        CollectionAssert.AreEqual(new long[] { 300, 100, 0 }, form.VisibleEntries.Select(e => e.ImageSize).ToArray());
                    }

                    await Task.CompletedTask;
                });
            }
        }

        [TestMethod]
        public void IsoInfoForm_ShowsRecordedInformation()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                folder.AddFile(@"sub\b.txt", "y");
                string iso = VerificationTests.CreateIso(folder, IsoFileSystemFormat.Iso9660Joliet, "INFO_TEST");
                string notIso = folder.AddFile("plain.txt", new string('z', 50000));

                UiStateTests.RunSta(async () =>
                {
                    using (IsoInfoForm form = OffScreen(new IsoInfoForm(iso)))
                    {
                        await form.OpenImageAsync();
                        Assert.AreEqual(2, form.CurrentInfo.FileCount);
                        Assert.AreEqual(1, form.CurrentInfo.DirectoryCount);
                        Assert.AreEqual("INFO_TEST", UiStateTests.Find<Label>(form, "labelValue").Text);
                        Assert.AreEqual("ISO 9660 with Joliet extensions", UiStateTests.Find<Label>(form, "fileSystemValue").Text);
                        Assert.AreEqual("No boot record", UiStateTests.Find<Label>(form, "bootValue").Text);
                        Assert.AreEqual(1, UiStateTests.Find<TreeView>(form, "treeView").Nodes.Count);

                        UiStateTests.Find<TextBox>(form, "pathTextBox").Text = notIso;
                        await form.OpenImageAsync();
                        Assert.IsNull(form.CurrentInfo);
                        StringAssert.Contains(UiStateTests.Find<Label>(form, "statusLabel").Text, "not an ISO 9660 image");
                    }
                });
            }
        }

        [TestMethod]
        public void CompatibilityForm_BlocksCreationWhenThereAreErrors()
        {
            UiStateTests.RunSta(async () =>
            {
                var report = new CompatibilityReport { Format = IsoFileSystemFormat.Iso9660Joliet };
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Error, "Problems that prevent creation", "1 problem", new[] { "huge.bin is 5 GB" }));
                report.Renames.Add(new IsoNameChange("a;b.txt", "a_b.txt", false, "unsupported characters replaced with _"));

                using (CompatibilityForm form = OffScreen(new CompatibilityForm(report, true)))
                {
                    Assert.IsFalse(UiStateTests.Find<Button>(form, "createButton").Enabled);
                    Assert.AreEqual(1, UiStateTests.Find<ListView>(form, "renamesListView").Items.Count);
                    Assert.AreEqual(1, UiStateTests.Find<ListBox>(form, "itemsListBox").Items.Count, "First finding is selected and its items shown");
                    StringAssert.Contains(CompatibilityForm.BuildReportText(report), "a;b.txt  ->  a_b.txt");
                }

                await Task.CompletedTask;
            });
        }
    }
}
