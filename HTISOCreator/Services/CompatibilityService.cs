using System;
using System.Collections.Generic;
using System.Linq;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    public enum FindingSeverity
    {
        Error,
        Warning,
        Info
    }

    public sealed class CompatibilityFinding
    {
        public CompatibilityFinding(FindingSeverity severity, string title, string detail, IEnumerable<string> items = null)
        {
            Severity = severity;
            Title = title;
            Detail = detail;
            Items = items?.ToList() ?? new List<string>();
        }

        public FindingSeverity Severity { get; }

        public string Title { get; }

        public string Detail { get; }

        public List<string> Items { get; }
    }

    public sealed class CompatibilityReport
    {
        public IsoFileSystemFormat Format { get; set; }

        public FilenameMode FilenameMode { get; set; }

        /// <summary>Renamed items whose reason includes a collision with another converted name.</summary>
        public int CollisionCount => Renames.Count(r => r.Reason.Contains(IsoNameMapper.DuplicateReason));

        public List<CompatibilityFinding> Findings { get; } = new List<CompatibilityFinding>();

        public List<IsoNameChange> Renames { get; } = new List<IsoNameChange>();

        public bool HasErrors => Findings.Any(f => f.Severity == FindingSeverity.Error);

        /// <summary>True when the user should see the report before an image is created.</summary>
        public bool NeedsReview => HasErrors || Renames.Count > 0 || Findings.Any(f => f.Severity == FindingSeverity.Warning);
    }

    /// <summary>
    /// Builds a filename and compatibility report from a creation plan: unsupported characters,
    /// long names, duplicates after conversion, directory depth, path length, file size limits,
    /// skipped items and disc capacity.
    /// </summary>
    public static class CompatibilityService
    {
        public const int Iso9660MaxPathLength = 255;
        private const int MaxItems = 500;

        public static CompatibilityReport Build(IsoBuildPlan plan, CapacityEvaluation capacity)
        {
            IsoFileSystemFormat format = plan.Options.Format;
            var report = new CompatibilityReport { Format = format, FilenameMode = plan.Options.FilenameMode };
            report.Renames.AddRange(plan.RenamedItems);

            List<string> otherErrors = plan.Errors.Where(e => !ReferenceEquals(e, plan.StrictModeError)).ToList();
            if (otherErrors.Count > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Error, "Problems that prevent creation",
                    otherErrors.Count + " problem(s) must be resolved before the image can be created.", otherErrors.Take(MaxItems)));
            }

            if (plan.StrictModeError != null)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Error, "Names that cannot be preserved (Strict Mode)",
                    plan.RenamedItems.Count + " name(s) cannot be stored exactly, so Strict Mode blocks creation. Rename them in the source, " +
                    "or switch to Compatibility Mode to allow the conversions shown below.",
                    plan.RenamedItems.Take(MaxItems).Select(Describe)));
            }

            if (plan.Analysis.Warnings.Count > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Items that will be skipped",
                    "Links and junctions are not followed, so their contents are not included.", plan.Analysis.Warnings));
            }

            List<IsoNameChange> duplicates = plan.RenamedItems.Where(r => r.Reason.Contains(IsoNameMapper.DuplicateReason)).ToList();
            if (duplicates.Count > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Duplicate names after conversion",
                    duplicates.Count + " name(s) would collide with another converted name and were given a numbered suffix, " +
                    "so every item keeps its own path. Check that nothing refers to them by their original names.",
                    duplicates.Take(MaxItems).Select(d => d.SourcePath + "  →  " + d.ImagePath)));
            }

            List<IsoNameChange> programFiles = plan.RenamedItems.Where(r => !r.IsDirectory && IsProgramFile(r.SourcePath)).ToList();
            if (programFiles.Count > 0 && plan.StrictModeError == null)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Program files that will be renamed",
                    programFiles.Count + " DLL, executable or other program file(s) would get a different name. Programs load these by their exact " +
                    "names, so software run from the image may stop working.",
                    programFiles.Take(MaxItems).Select(Describe)));
            }

            if (plan.RenamedItems.Count > 0 && plan.StrictModeError == null)
            {
                string limits = RulesFor(format);
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Names that will change",
                    plan.RenamedItems.Count + " file or folder name(s) will be stored differently. " + limits +
                    " Compatibility Mode: review the conversion preview and confirm it before the image is created."));
            }

            var directoryDepths = plan.Directories.Select(d => new { Path = d, Depth = d.Count(c => c == '\\') + 1 }).ToList();
            var tooDeep = directoryDepths.Where(d => d.Depth > IsoNameMapper.Iso9660RecommendedMaxDepth).Select(d => d.Path).ToList();
            if (tooDeep.Count > 0)
            {
                report.Findings.Add(format == IsoFileSystemFormat.Iso9660
                    ? new CompatibilityFinding(FindingSeverity.Warning, "Folders nested more than 8 levels",
                        "Strict ISO 9660 allows 8 levels. Windows and Linux read deeper images; some older systems may not.", tooDeep.Take(MaxItems))
                    : new CompatibilityFinding(FindingSeverity.Info, "Folders nested more than 8 levels",
                        "Readers that use the " + FileSystemFormats.VisibleNamesSource(format) + " names (Windows, Linux, macOS) handle this." +
                        (format == IsoFileSystemFormat.Udf ? string.Empty : " Readers that only understand plain ISO 9660 may not."), tooDeep.Take(MaxItems)));
            }

            if (format == IsoFileSystemFormat.Iso9660)
            {
                List<string> longPaths = plan.Files.Select(f => f.ImagePath).Concat(plan.Directories).Where(p => p.Length + 1 > Iso9660MaxPathLength).ToList();
                if (longPaths.Count > 0)
                {
                    report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Paths longer than 255 characters",
                        "ISO 9660 limits full paths to 255 characters; some readers cannot open these items.", longPaths.Take(MaxItems)));
                }
            }

            List<string> largeFiles = plan.Analysis.Files
                .Where(f => f.Length >= IsoCreationService.LargeFileWarningSize && f.Length <= FileSystemFormats.MaxFileSize(format))
                .Select(f => f.RelativePath + " (" + FileSizeFormatter.Format(f.Length) + ")").ToList();
            if (largeFiles.Count > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Files larger than 2 GB",
                    "Stored correctly, but some older operating systems and players cannot read files over 2 GB from a disc image.", largeFiles.Take(MaxItems)));
            }

            if (format == IsoFileSystemFormat.Iso9660JolietUdf && plan.Files.Count + plan.Directories.Count > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Info, "Hybrid image: Joliet and ISO 9660 views",
                    "Windows, macOS and Linux read the UDF names, which are the names checked and verified. For older readers the image also has " +
                    "IMAPI2's own Joliet names (up to about 110 characters; standard Joliet allows 64, so very old Joliet readers may not show them) " +
                    "and plain ISO 9660 8.3 alias names such as MICROS~1.DLL. Files must be smaller than 4 GB."));
            }

            if (format != IsoFileSystemFormat.Iso9660)
            {
                List<string> supplementary = plan.Analysis.Files.Select(f => f.RelativePath).Concat(plan.Analysis.Directories)
                    .Where(p => p.Any(char.IsSurrogate)).ToList();
                if (supplementary.Count > 0)
                {
                    report.Findings.Add(new CompatibilityFinding(FindingSeverity.Info, "Characters outside the Basic Multilingual Plane",
                        "Names with characters such as emoji are stored as UTF-16. Windows shows them correctly; very old readers may not.", supplementary.Take(MaxItems)));
                }
            }

            if (capacity != null && capacity.State == CapacityState.Exceeds)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Warning, "Larger than the selected disc",
                    capacity.Describe() + " The image can still be created; the capacity profile does not change the image."));
            }

            var nonEmpty = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in plan.Analysis.Files.Select(f => f.RelativePath).Concat(plan.Analysis.Directories))
            {
                int slash = path.LastIndexOf('\\');
                if (slash > 0)
                {
                    nonEmpty.Add(path.Substring(0, slash));
                }
            }

            int emptyFolders = plan.Analysis.Directories.Count(d => !nonEmpty.Contains(d));
            if (emptyFolders > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Info, "Empty folders",
                    emptyFolders + " empty folder(s) will be included."));
            }

            int emptyFiles = plan.Analysis.Files.Count(f => f.Length == 0);
            if (emptyFiles > 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Info, "Zero-byte files", emptyFiles + " empty file(s) will be included."));
            }

            if (report.Findings.Count == 0)
            {
                report.Findings.Add(new CompatibilityFinding(FindingSeverity.Info, "No compatibility problems found",
                    "All names, sizes and folder depths fit the " + FileSystemFormats.Name(format) + " file system."));
            }

            return report;
        }

        /// <summary>
        /// Explains the plain ISO 9660 alias names, which are always shortened and are not a loss of
        /// the original names (Joliet keeps those). Shown in the compatibility window and reports.
        /// </summary>
        public const string AliasNote =
            "ISO 9660 also requires a plain alias name for every item (upper case, A-Z 0-9 _, at most 30 characters) for very old readers. " +
            "Aliases are always created, are unique in each folder and are not filename changes: Windows, macOS, Linux and 7-Zip show the Joliet names, " +
            "which are the names Strict Mode checks.";

        /// <summary>The naming rules of a file system, for messages and the compatibility window.</summary>
        public static string RulesFor(IsoFileSystemFormat format)
        {
            switch (format)
            {
                case IsoFileSystemFormat.Iso9660Joliet:
                    return "Joliet names can have up to 64 Unicode characters and cannot contain ; * ? : / \\.";
                case IsoFileSystemFormat.Udf:
                case IsoFileSystemFormat.Iso9660JolietUdf:
                    return "UDF names can have up to " + FileSystemFormats.UdfMaxNameLength + " Unicode characters; every character Windows allows is kept.";
                default:
                    return "ISO 9660 names can use only A-Z, 0-9 and _, with at most 30 characters.";
            }
        }

        /// <summary>Explains the extra name views a format writes, which are not filename changes; null if none.</summary>
        public static string AliasNoteFor(IsoFileSystemFormat format)
        {
            switch (format)
            {
                case IsoFileSystemFormat.Iso9660Joliet:
                    return AliasNote;
                case IsoFileSystemFormat.Iso9660JolietUdf:
                    return "The image also has Joliet and plain ISO 9660 alias names for older readers. They are not filename changes: " +
                           "Windows, macOS and Linux show the UDF names, which are the names Strict Mode checks.";
                default:
                    return null;
            }
        }

        /// <summary>File types that programs locate by exact name.</summary>
        public static readonly string[] ProgramExtensions =
        {
            ".dll", ".exe", ".sys", ".ocx", ".drv", ".cpl", ".scr", ".msi", ".msp", ".com", ".bat", ".cmd", ".ps1", ".vbs", ".js",
            ".manifest", ".config", ".inf", ".cat", ".so", ".dylib", ".jar", ".pdb", ".winmd"
        };

        public static bool IsProgramFile(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            return ProgramExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
        }

        private static string Describe(IsoNameChange change)
        {
            return change.SourcePath + "  →  " + change.ImagePath + "  (" + change.Reason + ")";
        }
    }
}
