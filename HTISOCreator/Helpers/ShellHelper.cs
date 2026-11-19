using System.Diagnostics;
using System.IO;

namespace HTISOCreator.Helpers
{
    public static class ShellHelper
    {
        /// <summary>The publisher's website, as supplied by HTREASURE LTD.</summary>
        public const string CompanyWebsite = "https://www.htreasure.com";

        /// <summary>Opens a web address in the default browser.</summary>
        public static void OpenWebPage(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>Opens Explorer with <paramref name="filePath"/> selected.</summary>
        public static void ShowFileInExplorer(string filePath)
        {
            Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
        }

        public static void OpenFolder(string folderPath)
        {
            Process.Start("explorer.exe", "\"" + folderPath + "\"");
        }

        /// <summary>
        /// Opens a text or Markdown document with its associated program, or with Notepad when no
        /// program is associated with the file type (common for .md files).
        /// </summary>
        public static void OpenDocument(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Process.Start("notepad.exe", "\"" + path + "\"");
            }
        }

        /// <summary>
        /// Shows the ISO in Explorer when it exists, otherwise opens its folder.
        /// Returns false when neither exists.
        /// </summary>
        public static bool ShowOutput(string isoPath)
        {
            if (string.IsNullOrWhiteSpace(isoPath))
            {
                return false;
            }

            if (File.Exists(isoPath))
            {
                ShowFileInExplorer(isoPath);
                return true;
            }

            string folder = Path.GetDirectoryName(isoPath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                OpenFolder(folder);
                return true;
            }

            return false;
        }
    }
}
