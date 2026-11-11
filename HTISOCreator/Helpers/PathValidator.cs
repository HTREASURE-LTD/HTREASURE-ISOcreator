using System;
using System.IO;

namespace HTISOCreator.Helpers
{
    public static class PathValidator
    {
        /// <summary>Returns null when the source folder is usable, otherwise a user-facing message.</summary>
        public static string ValidateSourceFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Select a source folder.";
            }

            string full;
            if (!TryGetFullPath(path, out full) || !Path.IsPathRooted(path))
            {
                return "The source folder path is not valid.";
            }

            if (!Directory.Exists(full))
            {
                return "The source folder does not exist.";
            }

            return null;
        }

        /// <summary>
        /// Returns null when the output file path is usable, otherwise a user-facing message.
        /// Does not check whether the file already exists; the caller asks the user about that.
        /// </summary>
        public static string ValidateOutputFile(string outputPath, string sourceFolder)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return "Choose where to save the ISO file.";
            }

            string full;
            if (!TryGetFullPath(outputPath, out full) || !Path.IsPathRooted(outputPath))
            {
                return "The ISO file path is not valid.";
            }

            if (!string.Equals(Path.GetExtension(full), ".iso", StringComparison.OrdinalIgnoreCase))
            {
                return "The output file must have the .iso extension.";
            }

            if (Directory.Exists(full))
            {
                return "The output path is a folder. Enter a file name ending in .iso.";
            }

            string directory = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return "The output folder does not exist.";
            }

            string sourceFull;
            if (!string.IsNullOrWhiteSpace(sourceFolder) && TryGetFullPath(sourceFolder, out sourceFull) && IsSameOrInside(full, sourceFull))
            {
                return "The ISO file cannot be saved inside the source folder, because it would be included in itself. Choose a different location.";
            }

            return null;
        }

        /// <summary>Appends ".iso" when the user typed a file name without an extension.</summary>
        public static string EnsureIsoExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            path = path.Trim();
            return string.Equals(Path.GetExtension(path), ".iso", StringComparison.OrdinalIgnoreCase) ? path : path + ".iso";
        }

        public static bool IsSameOrInside(string candidatePath, string folderPath)
        {
            string candidate = NormalizeDirectory(candidatePath);
            string folder = NormalizeDirectory(folderPath);
            return candidate.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetFullPath(string path, out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                fullPath = Path.GetFullPath(path.Trim());
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException)
            {
                return false;
            }
        }

        private static string NormalizeDirectory(string path)
        {
            string full = Path.GetFullPath(path);
            return full.EndsWith("\\", StringComparison.Ordinal) ? full : full + "\\";
        }
    }
}
