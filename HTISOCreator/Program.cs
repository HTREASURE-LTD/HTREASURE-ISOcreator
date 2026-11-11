using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using HTISOCreator.Forms;
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

            // Clean up after a run that was interrupted while writing or replacing an image.
            List<string> recovered = new PendingOperationsRegistry(PendingOperationsRegistry.DefaultRegistryFile, LoggingService.Instance).Recover();
            if (recovered.Count > 0)
            {
                MessageBox.Show("HT ISO Creator was interrupted last time. It has tidied up:\n\n" + string.Join("\n\n", recovered),
                    "HT ISO Creator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Application.Run(new MainForm());

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
