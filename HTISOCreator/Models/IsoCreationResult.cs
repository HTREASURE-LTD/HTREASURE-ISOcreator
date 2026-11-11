using System;

namespace HTISOCreator.Models
{
    public sealed class IsoCreationResult
    {
        public string OutputPath { get; set; }

        public string SourceFolder { get; set; }

        public string VolumeLabel { get; set; }

        public IsoFileSystemFormat Format { get; set; }

        public long ImageSize { get; set; }

        public TimeSpan Elapsed { get; set; }

        public int FileCount { get; set; }

        public int DirectoryCount { get; set; }

        public long SourceBytes { get; set; }

        /// <summary>True when an existing file at <see cref="OutputPath"/> was replaced.</summary>
        public bool ReplacedExisting { get; set; }

        /// <summary>The operation reference used in the log file.</summary>
        public string LogReference { get; set; }
    }
}
