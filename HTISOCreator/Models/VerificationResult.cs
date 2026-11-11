using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HTISOCreator.Helpers;

namespace HTISOCreator.Models
{
    public enum VerificationCheckStatus
    {
        Passed,
        Failed,
        Warning,
        Skipped
    }

    /// <summary>How thoroughly an image was checked.</summary>
    public enum VerificationLevel
    {
        /// <summary>Image structure only; no source folder was compared.</summary>
        Structural,

        /// <summary>Structure plus every path and file size compared with the source folder.</summary>
        SourcePathsAndSizes,

        /// <summary>As above plus SHA-256 of every file's content compared with the source.</summary>
        SourceContent
    }

    public sealed class VerificationCheck
    {
        public VerificationCheck(string name, VerificationCheckStatus status, string detail)
        {
            Name = name;
            Status = status;
            Detail = detail;
        }

        public string Name { get; }

        public VerificationCheckStatus Status { get; }

        public string Detail { get; }
    }

    public sealed class VerificationOptions
    {
        public string IsoPath { get; set; }

        /// <summary>Optional. Without it only the image structure is verified.</summary>
        public string SourceFolder { get; set; }

        /// <summary>Compare SHA-256 of every file with the source (reads all data again).</summary>
        public bool CompareHashes { get; set; }

        /// <summary>Also mount the image with Windows and compare what Windows lists.</summary>
        public bool MountWithWindows { get; set; }

        /// <summary>Optional label the image must have.</summary>
        public string ExpectedLabel { get; set; }

        /// <summary>Optional file system the image must use.</summary>
        public IsoFileSystemFormat? ExpectedFormat { get; set; }

        /// <summary>
        /// How names are judged when comparing with the source. Strict: every name must equal the
        /// original. Compatibility: names must match the conversion plan exactly. Null: names
        /// that differ from the originals are reported as a warning.
        /// </summary>
        public FilenameMode? FilenameMode { get; set; }
    }

    public sealed class VerificationResult
    {
        public string IsoPath { get; set; }

        public string SourceFolder { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public TimeSpan Elapsed { get; set; }

        public VerificationLevel Level { get; set; }

        public IsoImageInfo Image { get; set; }

        public string ImageSha256 { get; set; }

        public List<VerificationCheck> Checks { get; } = new List<VerificationCheck>();

        public List<string> MissingFromImage { get; } = new List<string>();

        public List<string> ExtraInImage { get; } = new List<string>();

        /// <summary>Source items whose name in the image differs from the original ("original -> image (reason)").</summary>
        public List<string> NameDifferences { get; } = new List<string>();

        /// <summary>Items whose name in the image differs from what the conversion plan expects (letter case included).</summary>
        public List<string> NameMismatches { get; } = new List<string>();

        public List<string> SizeMismatches { get; } = new List<string>();

        public List<string> ContentMismatches { get; } = new List<string>();

        public int FilesCompared { get; set; }

        public long BytesHashed { get; set; }

        public bool Passed => Checks.Count > 0 && Checks.All(c => c.Status != VerificationCheckStatus.Failed);

        public string LevelDescription
        {
            get
            {
                switch (Level)
                {
                    case VerificationLevel.SourceContent:
                        return "Full source-content verification (paths, sizes and SHA-256)";
                    case VerificationLevel.SourcePathsAndSizes:
                        return "Source verification (paths and sizes; contents not hashed)";
                    default:
                        return "Structural verification only (no source folder compared)";
                }
            }
        }

        public string Summary
        {
            get
            {
                if (!Passed)
                {
                    int failed = Checks.Count(c => c.Status == VerificationCheckStatus.Failed);
                    return "FAILED: " + failed + " check(s) failed. " + LevelDescription + ".";
                }

                return "Passed. " + LevelDescription + ".";
            }
        }

        /// <summary>Plain-text report suitable for saving or attaching to a support request.</summary>
        public string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HT ISO Creator - ISO Verification Report");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine("Date:          " + Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("ISO file:      " + IsoPath);
            sb.AppendLine("Source folder: " + (string.IsNullOrEmpty(SourceFolder) ? "(not compared)" : SourceFolder));
            sb.AppendLine("Level:         " + LevelDescription);
            sb.AppendLine("Result:        " + (Passed ? "PASSED" : "FAILED"));
            sb.AppendLine("Time taken:    " + FileSizeFormatter.FormatDuration(Elapsed));
            if (Image != null)
            {
                sb.AppendLine("Image size:    " + FileSizeFormatter.FormatWithBytes(Image.FileLength));
                sb.AppendLine("Volume label:  " + (Image.DisplayLabel ?? "(none)"));
                sb.AppendLine("File system:   " + FileSystemFormats.Name(Image.Format) + (Image.HasUdf && Image.UdfRevision != null ? " (UDF " + Image.UdfRevision + ")" : string.Empty));
                sb.AppendLine("Contents:      " + Image.FileCount + " files, " + Image.DirectoryCount + " folders");
            }

            if (!string.IsNullOrEmpty(ImageSha256))
            {
                sb.AppendLine("Image SHA-256: " + ImageSha256);
            }

            sb.AppendLine();
            sb.AppendLine("Checks");
            sb.AppendLine(new string('-', 60));
            foreach (VerificationCheck check in Checks)
            {
                sb.AppendLine("[" + check.Status.ToString().ToUpperInvariant() + "] " + check.Name + ": " + check.Detail);
            }

            AppendList(sb, "Names that differ from the plan", NameMismatches);
            AppendList(sb, "Names that differ from the original", NameDifferences);
            AppendList(sb, "Missing from the image", MissingFromImage);
            AppendList(sb, "In the image but not in the source", ExtraInImage);
            AppendList(sb, "Different size", SizeMismatches);
            AppendList(sb, "Different content (SHA-256)", ContentMismatches);
            if (Image != null)
            {
                AppendList(sb, "Structural problems", Image.Problems);
            }

            return sb.ToString();
        }

        private static void AppendList(StringBuilder sb, string title, IList<string> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine(title + " (" + items.Count + ")");
            sb.AppendLine(new string('-', 60));
            foreach (string item in items)
            {
                sb.AppendLine("  " + item);
            }
        }
    }
}
