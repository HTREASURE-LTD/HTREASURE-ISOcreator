using System;
using System.IO;
using System.Windows.Forms;
using HTISOCreator.Services;

namespace HTISOCreator.Helpers
{
    /// <summary>Save dialog for the filename conversion report, shared by the main, compatibility and completion windows.</summary>
    internal static class ReportExport
    {
        public const string Filter = "Text report (*.txt)|*.txt|CSV for Excel (*.csv)|*.csv";

        public static void SaveConversionReport(IWin32Window owner, FilenameConversionReport report)
        {
            if (report == null)
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save Filename Conversion Report";
                dialog.Filter = Filter;
                dialog.FileName = "Filename conversion report.txt";
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    report.Save(dialog.FileName);
                    LoggingService.Instance.Info("Filename conversion report saved: " + dialog.FileName);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    LoggingService.Instance.Error("Filename conversion report could not be saved", ex);
                    MessageBox.Show(owner, "The report could not be saved: " + ex.Message, "HT ISO Creator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
