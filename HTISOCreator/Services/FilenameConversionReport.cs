using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>Where a name conversion stands when the report is written.</summary>
    public enum ConversionStatus
    {
        /// <summary>Strict Mode: the conversion is not allowed, so no image is created.</summary>
        BlockedByStrictMode,

        /// <summary>Compatibility Mode: shown to the user, not yet confirmed.</summary>
        AwaitingApproval,

        /// <summary>Compatibility Mode: confirmed by the user.</summary>
        Approved,

        /// <summary>The image was created with this conversion.</summary>
        AppliedInImage
    }

    /// <summary>
    /// A filename conversion report (TXT or CSV): every source item whose name differs in the
    /// image, with the reason, the status and collision warnings. Available before creation
    /// (compatibility window, main window) and after it (completion window).
    /// </summary>
    public sealed class FilenameConversionReport
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string SourceFolder { get; set; }

        public string DestinationIso { get; set; }

        public IsoFileSystemFormat Format { get; set; }

        public FilenameMode Mode { get; set; }

        public ConversionStatus Status { get; set; }

        public int TotalFiles { get; set; }

        public int TotalFolders { get; set; }

        public List<IsoNameChange> Changes { get; } = new List<IsoNameChange>();

        public int FilesAffected => Changes.Count(c => !c.IsDirectory);

        public int FoldersAffected => Changes.Count(c => c.IsDirectory);

        public List<IsoNameChange> Collisions => Changes.Where(IsCollision).ToList();

        /// <summary>Report for a prepared (or created) plan.</summary>
        public static FilenameConversionReport FromPlan(IsoBuildPlan plan, bool created)
        {
            var report = new FilenameConversionReport
            {
                SourceFolder = plan.Analysis.RootPath,
                DestinationIso = plan.Options.OutputPath,
                Format = plan.Options.Format,
                Mode = plan.Options.FilenameMode,
                TotalFiles = plan.Files.Count,
                TotalFolders = plan.Directories.Count
            };
            report.Changes.AddRange(plan.RenamedItems);
            report.Status = created ? ConversionStatus.AppliedInImage
                : plan.Options.FilenameMode == FilenameMode.Strict ? ConversionStatus.BlockedByStrictMode
                : plan.Options.ConversionsApproved ? ConversionStatus.Approved
                : ConversionStatus.AwaitingApproval;
            return report;
        }

        /// <summary>Report from a folder scan alone (no files are opened), for the main window.</summary>
        public static FilenameConversionReport FromAnalysis(FolderAnalysisResult analysis, string destinationIso, IsoFileSystemFormat format, FilenameMode mode)
        {
            IsoNameMapper.MappingResult mapping = IsoNameMapper.Map(analysis.Directories, analysis.Files.Select(f => f.RelativePath), format);
            var report = new FilenameConversionReport
            {
                SourceFolder = analysis.RootPath,
                DestinationIso = destinationIso,
                Format = format,
                Mode = mode,
                TotalFiles = analysis.FileCount,
                TotalFolders = analysis.DirectoryCount,
                Status = mode == FilenameMode.Strict ? ConversionStatus.BlockedByStrictMode : ConversionStatus.AwaitingApproval
            };
            report.Changes.AddRange(mapping.Changes);
            return report;
        }

        public static string ModeName(FilenameMode mode)
        {
            return mode == FilenameMode.Strict ? "Strict Mode - Preserve Original Filenames" : "Compatibility Mode - Allow Filename Conversion";
        }

        public static string StatusName(ConversionStatus status)
        {
            switch (status)
            {
                case ConversionStatus.BlockedByStrictMode:
                    return "Blocked (Strict Mode)";
                case ConversionStatus.AwaitingApproval:
                    return "Awaiting confirmation";
                case ConversionStatus.Approved:
                    return "Approved by user";
                default:
                    return "Applied in image";
            }
        }

        public static bool IsCollision(IsoNameChange change)
        {
            return change.Reason.Contains(IsoNameMapper.DuplicateReason);
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HT ISO Creator - Filename Conversion Report");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine("Date:             " + Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("Source folder:    " + SourceFolder);
            sb.AppendLine("Destination ISO:  " + (string.IsNullOrEmpty(DestinationIso) ? "(not chosen)" : DestinationIso));
            sb.AppendLine("File system:      " + FileSystemFormats.Name(Format) + " (" + FileSystemFormats.EngineName(Format) + ")");
            sb.AppendLine("Filename mode:    " + ModeName(Mode));
            sb.AppendLine("Status:           " + (Changes.Count == 0 ? "All names preserved exactly" : StatusName(Status)));
            sb.AppendLine("Totals:           " + TotalFiles + " files, " + TotalFolders + " folders");
            sb.AppendLine("Affected:         " + FilesAffected + " file(s), " + FoldersAffected + " folder(s)");
            sb.AppendLine("Collisions:       " + Collisions.Count);
            sb.AppendLine();
            if (Format != IsoFileSystemFormat.Iso9660)
            {
                sb.AppendLine("Names compared: the " + FileSystemFormats.VisibleNamesSource(Format) + " names that Windows and other systems show." +
                              (Format == IsoFileSystemFormat.Udf ? string.Empty : " Alias names for older readers are not filename changes and are not listed."));
                sb.AppendLine();
            }

            if (Changes.Count == 0)
            {
                sb.AppendLine("No names change. Every file and folder keeps its original name in the image.");
                return sb.ToString();
            }

            if (Collisions.Count > 0)
            {
                sb.AppendLine("Collision warnings (" + Collisions.Count + ")");
                sb.AppendLine(new string('-', 60));
                foreach (IsoNameChange change in Collisions)
                {
                    sb.AppendLine("  " + change.SourcePath + "  ->  " + change.ImagePath + "  (numbered so that it does not replace another item)");
                }

                sb.AppendLine();
            }

            sb.AppendLine("Conversions (" + Changes.Count + ")");
            sb.AppendLine(new string('-', 60));
            foreach (IsoNameChange change in Changes)
            {
                sb.AppendLine((change.IsDirectory ? "[Folder] " : "[File]   ") + change.SourcePath);
                sb.AppendLine("         -> " + change.ImagePath);
                sb.AppendLine("         Reason: " + change.Reason + ". Status: " + StatusName(Status) + ".");
            }

            return sb.ToString();
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();
            AppendRow(sb, "Report", "HT ISO Creator - Filename Conversion Report");
            AppendRow(sb, "Date", Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            AppendRow(sb, "Source folder", SourceFolder);
            AppendRow(sb, "Destination ISO", DestinationIso ?? string.Empty);
            AppendRow(sb, "File system", FileSystemFormats.Name(Format));
            AppendRow(sb, "Engine", FileSystemFormats.EngineName(Format));
            AppendRow(sb, "Filename mode", ModeName(Mode));
            AppendRow(sb, "Total files", TotalFiles.ToString(CultureInfo.InvariantCulture));
            AppendRow(sb, "Total folders", TotalFolders.ToString(CultureInfo.InvariantCulture));
            AppendRow(sb, "Files affected", FilesAffected.ToString(CultureInfo.InvariantCulture));
            AppendRow(sb, "Folders affected", FoldersAffected.ToString(CultureInfo.InvariantCulture));
            AppendRow(sb, "Collision warnings", Collisions.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();
            AppendRow(sb, "Type", "Original relative path", "Destination relative path", "Reason", "Status", "Collision");
            foreach (IsoNameChange change in Changes)
            {
                AppendRow(sb, change.IsDirectory ? "Folder" : "File", change.SourcePath, change.ImagePath, change.Reason, StatusName(Status),
                    IsCollision(change) ? "Yes" : "No");
            }

            return sb.ToString();
        }

        /// <summary>Saves as CSV when the file name ends in .csv, otherwise as text. UTF-8 with BOM so Excel reads Unicode names.</summary>
        public void Save(string path)
        {
            bool csv = string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(path, csv ? ToCsv() : ToText(), new UTF8Encoding(true));
        }

        private static void AppendRow(StringBuilder sb, params string[] values)
        {
            sb.AppendLine(string.Join(",", values.Select(Escape)));
        }

        /// <summary>RFC 4180 quoting; a leading = + - @ is prefixed with ' so spreadsheets do not run it as a formula.</summary>
        internal static string Escape(string value)
        {
            value = value ?? string.Empty;
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0)
            {
                value = "'" + value;
            }

            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || value != value.Trim()
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
