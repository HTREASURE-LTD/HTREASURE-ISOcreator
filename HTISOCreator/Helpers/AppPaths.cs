using System;
using System.Collections.Generic;
using System.IO;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// Where HT ISO Creator keeps its per-user data. Version 1.0.0 uses "HTREASURE LTD\HT ISO Creator"
    /// under %APPDATA% (settings, history) and %LOCALAPPDATA% (logs, pending operations).
    /// Development builds (Stages 1-3) used "HT Software\HT ISO Creator"; <see cref="MigrateLegacyData"/>
    /// copies settings and history from there once, and never deletes the old files.
    /// </summary>
    public static class AppPaths
    {
        public const string Publisher = "HTREASURE LTD";
        public const string Product = "HT ISO Creator";
        private const string LegacyPublisher = "HT Software";

        public static string RoamingFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Publisher, Product);

        public static string LocalFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Publisher, Product);

        public static string LegacyRoamingFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LegacyPublisher, Product);

        public static string LegacyLocalFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LegacyPublisher, Product);

        /// <summary>Folder with the user documentation, next to the program.</summary>
        public static string DocumentationFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation");

        /// <summary>
        /// Copies settings.xml and history.xml from the legacy folder when the new folder does not
        /// have them yet. Returns the files copied. Failures are returned as messages, never thrown.
        /// </summary>
        public static List<string> MigrateLegacyData(string legacyFolder, string newFolder, out List<string> errors)
        {
            var copied = new List<string>();
            errors = new List<string>();
            foreach (string name in new[] { "settings.xml", "history.xml" })
            {
                string source = Path.Combine(legacyFolder, name);
                string target = Path.Combine(newFolder, name);
                try
                {
                    if (File.Exists(source) && !File.Exists(target))
                    {
                        Directory.CreateDirectory(newFolder);
                        File.Copy(source, target, false);
                        copied.Add(name);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    errors.Add(name + ": " + ex.Message);
                }
            }

            return copied;
        }
    }
}
