using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using HTISOCreator.Forms;
using HTISOCreator.Helpers;
using HTISOCreator.Services;

namespace HTISOCreator
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            LoggingService.Instance.LogEnvironment(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            LoggingService.Instance.DeleteOldLogs(LoggingService.DefaultRetentionDays);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Settings and history from development builds (stored under "HT Software") are copied once.
            List<string> migrationErrors;
            List<string> migrated = AppPaths.MigrateLegacyData(AppPaths.LegacyRoamingFolder, AppPaths.RoamingFolder, out migrationErrors);
            if (migrated.Count > 0)
            {
                LoggingService.Instance.Info("Copied from the previous data folder: " + string.Join(", ", migrated));
            }

            foreach (string error in migrationErrors)
            {
                LoggingService.Instance.Warning("Could not copy previous data: " + error);
            }

            // Clean up after a run that was interrupted while writing or replacing an image.
            List<string> recovered = new PendingOperationsRegistry(PendingOperationsRegistry.DefaultRegistryFile, LoggingService.Instance).Recover();
            if (System.IO.File.Exists(PendingOperationsRegistry.LegacyRegistryFile))
            {
                recovered.AddRange(new PendingOperationsRegistry(PendingOperationsRegistry.LegacyRegistryFile, LoggingService.Instance).Recover());
            }
            if (recovered.Count > 0)
            {
                MessageBox.Show("HT ISO Creator was interrupted last time. It has tidied up:\n\n" + string.Join("\n\n", recovered),
                    "HT ISO Creator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Lets the installer detect a running copy during upgrade or uninstall (Inno Setup AppMutex).
            // Several copies may still run at once; the mutex is only a signal.
            using (new Mutex(false, "HTREASURE.HTISOCreator.Running"))
            {
                Application.Run(new MainForm());
            }

            LoggingService.Instance.Info("HT ISO Creator closed");
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LoggingService.Instance.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(
                "An unexpected error occurred. Technical details were written to the log file:\n\n" + LoggingService.Instance.CurrentLogFile,
                "HT ISO Creator", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggingService.Instance.Error("Unhandled exception (terminating=" + e.IsTerminating + ")", e.ExceptionObject as Exception);
        }
    }
}
