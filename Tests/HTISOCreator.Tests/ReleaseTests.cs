using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HTISOCreator.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Models;
using HTISOCreator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTISOCreator.Tests
{
    /// <summary>Stage 4: version 1.0.0 metadata, branding, data folders, upgrades, window placement and accessibility.</summary>
    [TestClass]
    public class ReleaseTests
    {
        private static readonly Assembly App = typeof(MainForm).Assembly;

        [TestMethod]
        public void VersionAndPublisherMetadata_AreForRelease100()
        {
            Assert.AreEqual(new Version(1, 0, 0, 0), App.GetName().Version);
            Assert.AreEqual("1.0.0.0", App.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            Assert.AreEqual("1.0.0", App.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Assert.AreEqual("HTREASURE LTD", App.GetCustomAttribute<AssemblyCompanyAttribute>().Company);
            Assert.AreEqual("HT ISO Creator", App.GetCustomAttribute<AssemblyProductAttribute>().Product);
            StringAssert.Contains(App.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright, "HTREASURE LTD");

            // The metadata Windows shows in File Explorer (Properties > Details).
            FileVersionInfo exe = FileVersionInfo.GetVersionInfo(App.Location);
            Assert.AreEqual("1.0.0", exe.ProductVersion);
            Assert.AreEqual("1.0.0.0", exe.FileVersion);
            Assert.AreEqual("HTREASURE LTD", exe.CompanyName);
            Assert.AreEqual("HT ISO Creator", exe.ProductName);
        }

        [TestMethod]
        public void AboutWindow_ShowsPublisherVersionAndDataIsoScope()
        {
            UiStateTests.RunSta(async () =>
            {
                using (var about = new AboutForm { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false })
                {
                    about.Show();
                    Application.DoEvents();
                    Assert.AreEqual("HTREASURE LTD", UiStateTests.Find<Label>(about, "developerValueLabel").Text);
                    StringAssert.StartsWith(UiStateTests.Find<Label>(about, "versionValueLabel").Text, "1.0.0 (build 1.0.0.0, ");
                    StringAssert.Contains(UiStateTests.Find<Label>(about, "copyrightLabel").Text, "HTREASURE LTD");
                    StringAssert.Contains(UiStateTests.Find<Label>(about, "descriptionLabel").Text, "does not create bootable images");
                    StringAssert.Contains(UiStateTests.Find<Label>(about, "licenceLabel").Text, "IMAPI2");
                }

                await Task.CompletedTask;
            });
        }

        [TestMethod]
        public void DataFolders_UseThePublisherName()
        {
            StringAssert.Contains(SettingsService.DefaultSettingsFile, @"\HTREASURE LTD\HT ISO Creator\settings.xml");
            StringAssert.Contains(HistoryService.DefaultHistoryFile, @"\HTREASURE LTD\HT ISO Creator\history.xml");
            StringAssert.Contains(LoggingService.DefaultLogDirectory, @"\HTREASURE LTD\HT ISO Creator\Logs");
            StringAssert.Contains(PendingOperationsRegistry.DefaultRegistryFile, @"\HTREASURE LTD\HT ISO Creator\pending-operations.txt");
            StringAssert.Contains(PendingOperationsRegistry.LegacyRegistryFile, @"\HT Software\HT ISO Creator\");
        }

        [TestMethod]
        public void LegacyData_IsCopiedOnce_AndNeverOverwrittenOrDeleted()
        {
            using (var folder = new TestFolder())
            {
                string legacy = Path.Combine(folder.Root, "legacy");
                string current = Path.Combine(folder.Root, "current");
                Directory.CreateDirectory(legacy);
                File.WriteAllText(Path.Combine(legacy, "settings.xml"), "<old settings/>");
                File.WriteAllText(Path.Combine(legacy, "history.xml"), "<old history/>");

                List<string> errors;
                CollectionAssert.AreEquivalent(new[] { "settings.xml", "history.xml" }, AppPaths.MigrateLegacyData(legacy, current, out errors));
                Assert.AreEqual(0, errors.Count);
                Assert.AreEqual("<old settings/>", File.ReadAllText(Path.Combine(current, "settings.xml")));
                Assert.IsTrue(File.Exists(Path.Combine(legacy, "settings.xml")), "The old files are kept");

                File.WriteAllText(Path.Combine(current, "settings.xml"), "<new settings/>");
                File.WriteAllText(Path.Combine(legacy, "settings.xml"), "<changed old settings/>");
                Assert.AreEqual(0, AppPaths.MigrateLegacyData(legacy, current, out errors).Count, "Nothing is copied again");
                Assert.AreEqual("<new settings/>", File.ReadAllText(Path.Combine(current, "settings.xml")), "Current settings are never overwritten");
                Assert.AreEqual(0, AppPaths.MigrateLegacyData(Path.Combine(folder.Root, "missing"), current, out errors).Count);
            }
        }

        [TestMethod]
        public void Stage33SettingsFile_LoadsWithEveryPreferenceKept()
        {
            using (var folder = new TestFolder())
            {
                // Written by the Stage 3.3 build (no window placement fields).
                string file = Path.Combine(folder.Root, "settings.xml");
                File.WriteAllText(file, @"<?xml version=""1.0"" encoding=""utf-8""?>
<AppSettings xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <DefaultOutputFolder>D:\Images</DefaultOutputFolder>
  <DefaultVolumeLabel>MY_DISC</DefaultVolumeLabel>
  <DefaultFormat>Udf</DefaultFormat>
  <RememberLastSource>true</RememberLastSource>
  <RememberLastOutput>false</RememberLastOutput>
  <LastSourceFolder>C:\Data</LastSourceFolder>
  <LastOutputFolder />
  <ShowCompletionDialog>false</ShowCompletionDialog>
  <Theme>Dark</Theme>
  <AutoVerify>true</AutoVerify>
  <VerifyHashes>true</VerifyHashes>
  <VerifyWithWindowsMount>true</VerifyWithWindowsMount>
  <CapacityProfileId>dvd5</CapacityProfileId>
  <CustomCapacityMegabytes>4400</CustomCapacityMegabytes>
  <FilenameMode>Compatibility</FilenameMode>
</AppSettings>");
                AppSettings loaded = new SettingsService(file, SettingsServiceTests.TestLog).Load();
                Assert.AreEqual(IsoFileSystemFormat.Udf, loaded.DefaultFormat);
                Assert.AreEqual(FilenameMode.Compatibility, loaded.FilenameMode);
                Assert.AreEqual(@"D:\Images", loaded.DefaultOutputFolder);
                Assert.AreEqual("MY_DISC", loaded.DefaultVolumeLabel);
                Assert.AreEqual("dvd5", loaded.CapacityProfileId);
                Assert.IsTrue(loaded.VerifyHashes && loaded.VerifyWithWindowsMount && loaded.AutoVerify);
                Assert.IsFalse(loaded.ShowCompletionDialog);
                Assert.AreEqual(AppTheme.Dark, loaded.Theme);
                Assert.AreEqual(0, loaded.WindowWidth, "No saved window placement yet");
            }
        }

        [TestMethod]
        public void MainForm_RemembersWindowPlacement_AndIgnoresOffScreenPositions()
        {
            using (var folder = new TestFolder())
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                var wanted = new Rectangle(area.Left + 20, area.Top + 10, 820, Math.Min(area.Height - 20, 720));
                var settings = new AppSettings
                {
                    RememberLastSource = false,
                    Theme = AppTheme.Light,
                    WindowLeft = wanted.Left,
                    WindowTop = wanted.Top,
                    WindowWidth = wanted.Width,
                    WindowHeight = wanted.Height
                };
                string settingsFile = Path.Combine(folder.Root, "settings.xml");
                UiStateTests.RunSta(async () =>
                {
                    var service = new SettingsService(settingsFile, SettingsServiceTests.TestLog);
                    service.Save(settings);
                    using (var form = new MainForm(service) { ShowInTaskbar = false })
                    {
                        form.Show();
                        Application.DoEvents();
                        Assert.AreEqual(wanted, form.Bounds, "Saved placement is restored");
                        form.Bounds = new Rectangle(wanted.Left + 15, wanted.Top + 5, wanted.Width - 10, wanted.Height - 10);
                        form.Close();
                    }

                    AppSettings saved = service.Load();
                    Assert.AreEqual(wanted.Left + 15, saved.WindowLeft);
                    Assert.AreEqual(wanted.Width - 10, saved.WindowWidth);
                    Assert.IsFalse(saved.WindowMaximized);

                    saved.WindowLeft = -30000;
                    saved.WindowTop = -30000;
                    service.Save(saved);
                    using (var form = new MainForm(service) { ShowInTaskbar = false })
                    {
                        form.Show();
                        Application.DoEvents();
                        Assert.IsTrue(Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(form.Bounds)), "An off-screen position is not restored");
                        form.Close();
                    }

                    await Task.CompletedTask;
                });
            }
        }

        [TestMethod]
        public void ReadOnlyExistingIso_IsNeverReplaced()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "new content");
                string output = folder.OutputFile("protected.iso");
                File.WriteAllBytes(output, new byte[] { 1, 2, 3 });
                File.SetAttributes(output, FileAttributes.ReadOnly);
                try
                {
                    StringAssert.Contains(PathValidator.ValidateOutputFile(output, folder.Source), "read-only");
                    IsoBuildPlan plan = new IsoCreationService(SettingsServiceTests.TestLog).Prepare(new IsoCreationOptions
                    {
                        SourceFolder = folder.Source,
                        OutputPath = output,
                        VolumeLabel = "RO",
                        OverwriteExisting = true
                    }, null, CancellationToken.None);
                    Assert.IsFalse(plan.CanCreate);
                    CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(output));
                    Assert.AreEqual(1, Directory.GetFiles(folder.Output).Length, "No backup or temporary file");
                }
                finally
                {
                    File.SetAttributes(output, FileAttributes.Normal);
                }
            }
        }

        [TestMethod]
        public void ReservedDeviceNames_AreRejectedAsOutputFile()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                foreach (string name in new[] { "CON.iso", "con.iso", "NUL.iso", "aux.backup.iso", "COM1.iso", "lpt9.iso" })
                {
                    StringAssert.Contains(PathValidator.ValidateOutputFile(folder.OutputFile(name), folder.Source), "reserves for a device", name);
                }

                foreach (string name in new[] { "CONSOLE.iso", "COM10.iso", "my CON.iso", "LPT.iso", "Contoso.iso" })
                {
                    Assert.IsNull(PathValidator.ValidateOutputFile(folder.OutputFile(name), folder.Source), name);
                }
            }
        }

        [TestMethod]
        public void MountErrors_AreReadableText_NotClixml()
        {
            const string Clixml = "#< CLIXML\r\n<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\">" +
                                  "<S S=\"Error\">Mount-DiskImage : The disc image file is corrupted._x000D__x000A_</S>" +
                                  "<S S=\"Error\">At line:1 char:6_x000D__x000A_</S></Objs>";
            Assert.AreEqual("Mount-DiskImage : The disc image file is corrupted.", WindowsMountChecker.FirstLine(Clixml));
            Assert.AreEqual("Plain error", WindowsMountChecker.FirstLine("Plain error\r\nsecond line"));
        }

        [TestMethod]
        public void MainForm_InputsHaveAccessibleNames_AndKeyboardOrderIsUnique()
        {
            using (var folder = new TestFolder())
            {
                UiStateTests.RunSta(async () =>
                {
                    using (MainForm form = UiStateTests.OpenForm(folder))
                    {
                        var unnamed = new List<string>();
                        var duplicates = new List<string>();
                        Walk(form, control =>
                        {
                            // (The text box inside a NumericUpDown is part of that control and takes its name.)
                            if ((control is TextBox || control is ComboBox || control is NumericUpDown || control is ProgressBar) &&
                                !(control.Parent is NumericUpDown) && string.IsNullOrEmpty(control.AccessibleName))
                            {
                                unnamed.Add(control.Name);
                            }

                            var tabbable = control.Controls.Cast<Control>().Where(c => c.TabStop && !(c is Label)).ToList();
                            duplicates.AddRange(tabbable.GroupBy(c => c.TabIndex).Where(g => g.Count() > 1)
                                .Select(g => control.Name + ": " + string.Join("/", g.Select(c => c.Name))));
                        });
                        Assert.AreEqual(0, unnamed.Count, "No accessible name: " + string.Join(", ", unnamed));
                        Assert.AreEqual(0, duplicates.Count, "Same tab index: " + string.Join("; ", duplicates));

                        var help = (ToolStripMenuItem)form.MainMenuStrip.Items.Cast<ToolStripItem>().Single(i => i.Name == "helpMenu");
                        CollectionAssert.IsSubsetOf(new[] { "USER_GUIDE.md", "FILESYSTEM_GUIDE.md", "TROUBLESHOOTING.md", "RELEASE_NOTES.md", "THIRD_PARTY_NOTICES.md" },
                            help.DropDownItems.OfType<ToolStripMenuItem>().Select(i => i.Tag as string).Where(t => t != null).ToList());
                    }

                    await Task.CompletedTask;
                });
            }
        }

        private static void Walk(Control parent, Action<Control> visit)
        {
            visit(parent);
            foreach (Control child in parent.Controls)
            {
                Walk(child, visit);
            }
        }
    }
}
