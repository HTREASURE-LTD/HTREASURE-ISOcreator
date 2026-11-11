using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
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
    /// <summary>
    /// Drives the real forms on an STA thread with a Windows Forms synchronization context.
    /// Windows are placed off-screen; no message boxes are reachable from these paths.
    /// </summary>
    [TestClass]
    public class UiStateTests
    {
        static UiStateTests()
        {
            // Same text rendering as Program.Main; must run before any window exists in the process.
            Application.EnableVisualStyles();
            try
            {
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch (InvalidOperationException)
            {
                // A window was already created by another test class; rendering is already fixed.
            }
        }

        internal static void RunSta(Func<Task> body)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                // A real message loop, as the application uses, so async continuations are delivered normally.
                var context = new ApplicationContext();
                var synchronization = new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(synchronization);

                // Exceptions from async void event handlers surface here; without a handler WinForms would
                // show a modal error dialog and the test would hang instead of failing.
                ThreadExceptionEventHandler onUiError = (s, e) =>
                {
                    error = new AssertFailedException("Unhandled exception in a UI event handler: " + e.Exception, e.Exception);
                    context.ExitThread();
                };
                Application.ThreadException += onUiError;
                synchronization.Post(async state =>
                {
                    try
                    {
                        await body();
                    }
                    catch (Exception ex)
                    {
                        error = error ?? ex;
                    }
                    finally
                    {
                        foreach (Form open in Application.OpenForms.Cast<Form>().ToList())
                        {
                            open.Dispose();
                        }

                        context.ExitThread();
                    }
                }, null);

                try
                {
                    Application.Run(context);
                }
                finally
                {
                    Application.ThreadException -= onUiError;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(60)))
            {
                throw new TimeoutException("UI test did not finish. UI thread stack:\n" + CaptureStack(thread));
            }

            if (error != null)
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }

        /// <summary>Stack of a stuck UI thread, so a hang reports where it is instead of only timing out.</summary>
        private static string CaptureStack(Thread thread)
        {
#pragma warning disable 618 // Suspend/Resume and StackTrace(Thread) are obsolete but are the only way on .NET Framework.
            try
            {
                thread.Suspend();
                try
                {
                    return new System.Diagnostics.StackTrace(thread, false).ToString();
                }
                finally
                {
                    thread.Resume();
                }
            }
            catch (Exception ex)
            {
                return "(stack unavailable: " + ex.Message + ")";
            }
#pragma warning restore 618
        }

        internal static MainForm OpenForm(TestFolder folder, AppSettings settings = null)
        {
            var service = new SettingsService(Path.Combine(folder.Root, "settings.xml"), SettingsServiceTests.TestLog);
            service.Save(settings ?? new AppSettings { RememberLastSource = false, RememberLastOutput = false, Theme = AppTheme.Light });
            var form = new MainForm(service)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-5000, -5000),
                ShowInTaskbar = false
            };
            form.Show();
            Application.DoEvents();
            return form;
        }

        internal static T Find<T>(Control root, string name) where T : Control
        {
            return (T)root.Controls.Find(name, true).Single();
        }

        private static ToolStripMenuItem Menu(Form form, string name)
        {
            return form.MainMenuStrip.Items.OfType<ToolStripMenuItem>()
                .SelectMany(m => m.DropDownItems.OfType<ToolStripMenuItem>())
                .Single(i => i.Name == name);
        }

        internal static async Task WaitUntil(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(20))
                {
                    throw new TimeoutException("Condition not met");
                }

                await Task.Delay(20);
            }
        }

        [TestMethod]
        public void CreateButton_EnabledOnlyWhenInputsAreValid()
        {
            using (var folder = new TestFolder())
            {
                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        var create = Find<Button>(form, "createButton");
                        Assert.IsFalse(create.Enabled, "Nothing entered yet");

                        Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        Find<TextBox>(form, "volumeTextBox").Text = "TEST";
                        Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile();
                        Assert.IsTrue(create.Enabled);
                        Assert.IsTrue(Menu(form, "createIsoMenuItem").Enabled);

                        Find<TextBox>(form, "volumeTextBox").Text = "BAD LABEL";
                        Assert.IsFalse(create.Enabled, "Invalid label");
                        Find<TextBox>(form, "volumeTextBox").Text = "TEST";

                        Find<TextBox>(form, "outputTextBox").Text = Path.Combine(folder.Source, "inside.iso");
                        Assert.IsFalse(create.Enabled, "Output inside source");
                        await Task.CompletedTask;
                    }
                });
            }
        }

        [TestMethod]
        public void Scan_ShowsAnalysisAndSuggestsLabelAndOutput()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("one.txt", new byte[1000]);
                folder.AddFile(@"sub\two.txt", new byte[3000]);
                folder.AddDirectory("empty");

                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);

                        Assert.AreEqual(2, form.CurrentAnalysis.FileCount);
                        Assert.AreEqual(2, form.CurrentAnalysis.DirectoryCount);
                        Assert.AreEqual("2", Find<Label>(form, "filesValue").Text);
                        Assert.AreEqual("2", Find<Label>(form, "foldersValue").Text);
                        StringAssert.Contains(Find<Label>(form, "sourceSizeValue").Text, "3.91 KB");

                        long? expected = IsoSizeCalculator.Calculate(form.CurrentAnalysis, IsoFileSystemFormat.Iso9660Joliet);
                        Assert.AreEqual(expected, form.CalculatedIsoSize);
                        StringAssert.Contains(Find<Label>(form, "isoSizeValue").Text, expected.Value.ToString("N0"));

                        Assert.AreEqual("SOURCE_FOLDER", Find<TextBox>(form, "volumeTextBox").Text);
                        Assert.AreEqual(Path.Combine(folder.Root, "source folder.iso"), Find<TextBox>(form, "outputTextBox").Text);
                        StringAssert.Contains(Find<Label>(form, "freeSpaceValue").Text, "on " + Path.GetPathRoot(folder.Root));
                        Assert.IsTrue(Find<Button>(form, "createButton").Enabled);
                    }
                });
            }
        }

        [TestMethod]
        public void JolietToggle_RecalculatesIsoSize()
        {
            using (var folder = new TestFolder())
            {
                for (int i = 0; i < 30; i++)
                {
                    folder.AddFile("a rather long file name number " + i + ".txt", "x");
                }

                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);
                        long? isoOnly = IsoSizeCalculator.Calculate(form.CurrentAnalysis, IsoFileSystemFormat.Iso9660);
                        Assert.AreNotEqual(isoOnly, form.CalculatedIsoSize, "Joliet adds a second directory tree");

                        Find<ComboBox>(form, "fileSystemComboBox").SelectedIndex = FileSystemFormats.All.ToList().IndexOf(IsoFileSystemFormat.Iso9660);
                        await WaitUntil(() => form.CalculatedIsoSize == isoOnly);
                        Assert.AreEqual("ISO 9660", Find<Label>(form, "formatValue").Text);
                    }
                });
            }
        }

        [TestMethod]
        public void BusyState_DisablesInputsAndEnablesCancel()
        {
            using (var folder = new TestFolder())
            {
                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        Find<TextBox>(form, "volumeTextBox").Text = "TEST";
                        Find<TextBox>(form, "outputTextBox").Text = folder.OutputFile();

                        form.SetBusy(true);
                        Assert.IsTrue(form.IsBusy);
                        Assert.IsFalse(Find<Button>(form, "createButton").Enabled);
                        Assert.IsTrue(Find<Button>(form, "cancelButton").Enabled);
                        Assert.IsFalse(Find<Button>(form, "browseButton").Enabled);
                        Assert.IsFalse(Find<Button>(form, "saveAsButton").Enabled);
                        Assert.IsFalse(Find<Button>(form, "refreshButton").Enabled);
                        Assert.IsFalse(Find<ComboBox>(form, "fileSystemComboBox").Enabled);
                        Assert.IsTrue(Find<TextBox>(form, "sourceTextBox").ReadOnly);
                        Assert.IsFalse(Menu(form, "settingsMenuItem").Enabled);
                        Assert.IsFalse(Menu(form, "createIsoMenuItem").Enabled);
                        Assert.IsNotNull(await form.AcceptDroppedPathsAsync(new[] { folder.Output }), "Drops are refused while busy");

                        form.SetBusy(false);
                        Assert.IsTrue(Find<Button>(form, "createButton").Enabled);
                        Assert.IsFalse(Find<Button>(form, "cancelButton").Enabled);
                        Assert.IsTrue(Find<Button>(form, "browseButton").Enabled);
                        Assert.IsFalse(Find<TextBox>(form, "sourceTextBox").ReadOnly);
                        Assert.IsTrue(Menu(form, "settingsMenuItem").Enabled);
                    }
                });
            }
        }

        [TestMethod]
        public void DroppedFolder_PopulatesSource_AndFileIsRejected()
        {
            using (var folder = new TestFolder())
            {
                string file = folder.AddFile("a.txt", "x");
                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        string error = await form.AcceptDroppedPathsAsync(new[] { file });
                        StringAssert.Contains(error, "is a file");
                        Assert.AreEqual(string.Empty, Find<TextBox>(form, "sourceTextBox").Text);

                        error = await form.AcceptDroppedPathsAsync(new[] { folder.Source });
                        Assert.IsNull(error);
                        Assert.AreEqual(folder.Source, Find<TextBox>(form, "sourceTextBox").Text);
                        Assert.AreEqual(1, form.CurrentAnalysis.FileCount);
                        Assert.AreEqual("SOURCE_FOLDER", Find<TextBox>(form, "volumeTextBox").Text);
                    }
                });
            }
        }

        [TestMethod]
        public void Settings_AppliedAtStartupAndRememberedOnClose()
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                string outputFolder = Directory.CreateDirectory(Path.Combine(folder.Root, "iso files")).FullName;
                var settings = new AppSettings
                {
                    DefaultFormat = IsoFileSystemFormat.Iso9660,
                    DefaultOutputFolder = outputFolder,
                    DefaultVolumeLabel = "FROM_SETTINGS",
                    RememberLastSource = true,
                    RememberLastOutput = false,
                    LastSourceFolder = folder.Source,
                    Theme = AppTheme.Dark
                };

                RunSta(async () =>
                {
                    MainForm form = OpenForm(folder, settings);
                    await WaitUntil(() => form.CurrentAnalysis != null);

                    Assert.AreEqual(folder.Source, Find<TextBox>(form, "sourceTextBox").Text, "Last source restored");
                    Assert.AreEqual(IsoFileSystemFormat.Iso9660, FileSystemFormats.All[Find<ComboBox>(form, "fileSystemComboBox").SelectedIndex], "Default format applied");
                    Assert.AreEqual("FROM_SETTINGS", Find<TextBox>(form, "volumeTextBox").Text);
                    Assert.AreEqual(Path.Combine(outputFolder, "source folder.iso"), Find<TextBox>(form, "outputTextBox").Text);
                    Assert.AreEqual(ThemePalette.Dark.Window, form.BackColor, "Dark theme applied");

                    form.Close();
                    form.Dispose();
                });

                AppSettings saved = new SettingsService(Path.Combine(folder.Root, "settings.xml"), SettingsServiceTests.TestLog).Load();
                Assert.AreEqual(folder.Source, saved.LastSourceFolder);
                Assert.AreEqual(AppTheme.Dark, saved.Theme);
            }
        }

        [TestMethod]
        public void SettingsForm_ReadsBackValuesAndValidates()
        {
            using (var folder = new TestFolder())
            {
                RunSta(async () =>
                {
                    var original = new AppSettings
                    {
                        DefaultOutputFolder = folder.Output,
                        DefaultFormat = IsoFileSystemFormat.Iso9660,
                        DefaultVolumeLabel = "A_LONG_LABEL_OVER_16",
                        RememberLastSource = false,
                        ShowCompletionDialog = false,
                        Theme = AppTheme.Light,
                        LastSourceFolder = "kept"
                    };

                    using (var form = new SettingsForm(original))
                    {
                        Assert.IsNull(form.ValidateInputs());
                        AppSettings read = form.ReadValues();
                        Assert.AreEqual(folder.Output, read.DefaultOutputFolder);
                        Assert.AreEqual(IsoFileSystemFormat.Iso9660, read.DefaultFormat);
                        Assert.AreEqual("A_LONG_LABEL_OVER_16", read.DefaultVolumeLabel);
                        Assert.IsFalse(read.RememberLastSource);
                        Assert.IsFalse(read.ShowCompletionDialog);
                        Assert.AreEqual(AppTheme.Light, read.Theme);
                        Assert.AreEqual("kept", read.LastSourceFolder, "Values not on the form are preserved");

                        Find<ComboBox>(form, "formatComboBox").SelectedIndex = 0;
                        StringAssert.Contains(form.ValidateInputs(), "16 characters", "Label too long once Joliet is the default");

                        Find<ComboBox>(form, "formatComboBox").SelectedIndex = 1;
                        Find<TextBox>(form, "outputFolderTextBox").Text = Path.Combine(folder.Root, "missing");
                        StringAssert.Contains(form.ValidateInputs(), "does not exist");
                    }

                    await Task.CompletedTask;
                });
            }
        }

        [DataTestMethod]
        [DataRow(100)]
        [DataRow(125)]
        [DataRow(150)]
        [DataRow(200)]
        public void MainForm_LayoutHasNoOverlapsWhenScaled(int percent)
        {
            using (var folder = new TestFolder())
            {
                folder.AddFile("a.txt", "x");
                RunSta(async () =>
                {
                    using (MainForm form = OpenForm(folder))
                    {
                        int buttonWidth = Find<Button>(form, "createButton").Width;

                        // AutoScaleMode.Font rescales the whole form when its font grows, which is
                        // what Windows display scaling does to a DPI-aware WinForms application.
                        form.Font = new Font(form.Font.FontFamily, 9f * percent / 100f);
                        Application.DoEvents();
                        Find<TextBox>(form, "sourceTextBox").Text = folder.Source;
                        await form.ScanSourceAsync(false);

                        // Font-based scaling follows real font metrics, so the factor is close to, not exactly, the percentage.
                        double factor = Find<Button>(form, "createButton").Width / (double)buttonWidth;
                        Assert.AreEqual(percent / 100.0, factor, 0.15, "Controls should scale with the font");

                        var problems = new List<string>();
                        CheckLayout(form, problems);

                        // At 200% the window needs about 1,430 px of height. On a screen with less (this test
                        // machine has a 1080-pixel display) Windows limits the window to the screen, so only the
                        // controls inside each group and their overlaps can be checked; real 200% displays
                        // (typically 4K, 2,160 px) have room for the whole window.
                        if (form.Height >= Screen.FromControl(form).WorkingArea.Height - 2 && percent >= 200)
                        {
                            problems.RemoveAll(p => p.StartsWith("MainForm/", StringComparison.Ordinal) && p.Contains(" is outside "));
                        }
                        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));

                        string shotFolder = Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests");
                        Directory.CreateDirectory(shotFolder);
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                            bitmap.Save(Path.Combine(shotFolder, "layout-" + percent + ".png"));
                        }
                    }
                });
            }
        }

        /// <summary>Visible siblings must not overlap and must lie inside their parent.</summary>
        private static void CheckLayout(Control parent, List<string> problems)
        {
            Control[] children = parent.Controls.Cast<Control>().Where(c => c.Visible && !(c is ToolStrip)).ToArray();
            Rectangle client = parent.ClientRectangle;
            if (parent is Form)
            {
                int top = parent.Controls.OfType<MenuStrip>().Sum(m => m.Height);
                int bottom = parent.Controls.OfType<StatusStrip>().Sum(s => s.Height);
                client = new Rectangle(0, top, client.Width, client.Height - top - bottom);
            }

            foreach (Control child in children)
            {
                if (!client.Contains(child.Bounds))
                {
                    problems.Add(parent.Name + "/" + child.Name + " " + child.Bounds + " is outside " + client);
                }

                // Fixed-size labels must be wide enough for their text unless they ellipsize it.
                var label = child as Label;
                if (label != null && !label.AutoSize && !label.AutoEllipsis && label.Text.Length > 0 &&
                    TextRenderer.MeasureText(label.Text, label.Font).Width > label.Width + 2)
                {
                    problems.Add(parent.Name + "/" + child.Name + " text \"" + label.Text + "\" does not fit in " + label.Width + " px");
                }

                Rectangle needed = child.Bounds;
                foreach (Control other in children.Where(o => o != child && string.CompareOrdinal(o.Name, child.Name) > 0))
                {
                    if (needed.IntersectsWith(other.Bounds))
                    {
                        problems.Add(parent.Name + ": " + child.Name + " " + needed + " overlaps " + other.Name + " " + other.Bounds);
                    }
                }

                CheckLayout(child, problems);
            }
        }
    }
}
