using System;

namespace HTISOCreator.Models
{
    public enum IsoOperationStage
    {
        Analysing,
        CheckingFiles,
        Writing,
        Finalising,
        Verifying
    }

    /// <summary>
    /// Snapshot of creation progress. Values are measured, never estimated, except
    /// <see cref="EstimatedRemaining"/> which is extrapolated from the measured write rate.
    /// </summary>
    public sealed class IsoProgressInfo
    {
        public IsoOperationStage Stage { get; set; }

        public string Message { get; set; }

        /// <summary>Image bytes written so far; 0 until the Writing stage.</summary>
        public long BytesWritten { get; set; }

        /// <summary>Exact image size once known; 0 before the Writing stage.</summary>
        public long TotalBytes { get; set; }

        public int FilesProcessed { get; set; }

        public int TotalFiles { get; set; }

        /// <summary>Image path of the source file currently being read, if any.</summary>
        public string CurrentFile { get; set; }

        public TimeSpan Elapsed { get; set; }

        public TimeSpan? EstimatedRemaining { get; set; }

        /// <summary>Percentage of image bytes written, or null when not measurable (indeterminate).</summary>
        public double? Percent => TotalBytes > 0 ? BytesWritten * 100.0 / TotalBytes : (double?)null;
    }
}
