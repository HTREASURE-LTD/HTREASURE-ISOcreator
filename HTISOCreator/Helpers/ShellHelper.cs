using System.Diagnostics;
using System.IO;

namespace HTISOCreator.Helpers
{
    public static class ShellHelper
    {
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
