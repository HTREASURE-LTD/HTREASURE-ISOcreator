using System;
using System.Collections.Generic;

namespace HTISOCreator.Models
{
    /// <summary>
    /// A regular file found in the source folder.
    /// </summary>
    public sealed class SourceFileEntry
    {
        public SourceFileEntry(string fullPath, string relativePath, long length, DateTime lastWriteTimeUtc)
        {
            FullPath = fullPath;
            RelativePath = relativePath;
            Length = length;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string FullPath { get; }

        /// <summary>Path relative to the source root, using '\' separators.</summary>
        public string RelativePath { get; }

        public long Length { get; }

        public DateTime LastWriteTimeUtc { get; }
    }

    /// <summary>
    /// Result of scanning a source folder.
    /// </summary>
    public sealed class FolderAnalysisResult
    {
        public FolderAnalysisResult(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public List<SourceFileEntry> Files { get; } = new List<SourceFileEntry>();

        /// <summary>Every sub-folder (relative to the root), including empty ones.</summary>
        public List<string> Directories { get; } = new List<string>();

        /// <summary>Last-modified time (UTC) of each sub-folder, keyed by relative path.</summary>
        public Dictionary<string, DateTime> DirectoryTimesUtc { get; } = new Dictionary<string, DateTime>(StringComparer.Ordinal);

        /// <summary>Items that could not be read and would be missing from the image.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Items deliberately skipped (for example junctions), reported to the user.</summary>
        public List<string> Warnings { get; } = new List<string>();

        public long TotalBytes { get; set; }

        public int FileCount => Files.Count;

        public int DirectoryCount => Directories.Count;
    }
}
