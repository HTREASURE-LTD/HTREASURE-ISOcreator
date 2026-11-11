using System;

namespace HTISOCreator.Models
{
    public enum CreationOutcome
    {
        Succeeded,
        Failed,
        Cancelled
    }

    public enum HistoryVerificationStatus
    {
        NotVerified,
        Passed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// One ISO creation attempt. Public setters and primitive-friendly types are required by XmlSerializer.
    /// </summary>
    public sealed class HistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public CreationOutcome Outcome { get; set; }

        public string SourceFolder { get; set; } = string.Empty;

        public string OutputPath { get; set; } = string.Empty;

        public string VolumeLabel { get; set; } = string.Empty;

        public IsoFileSystemFormat Format { get; set; }

        public long ImageSize { get; set; }

        public int FileCount { get; set; }

        public int DirectoryCount { get; set; }

        public double DurationSeconds { get; set; }

        /// <summary>Error or cancellation message; empty on success.</summary>
        public string Message { get; set; } = string.Empty;

        public HistoryVerificationStatus Verification { get; set; }

        /// <summary>Description of the verification level and summary, e.g. "Full source-content verification ...".</summary>
        public string VerificationDetail { get; set; } = string.Empty;

        public DateTime? VerifiedAt { get; set; }

        /// <summary>Reference that appears in the log file for this operation.</summary>
        public string LogReference { get; set; } = string.Empty;

        /// <summary>Filename mode used for the creation; null for entries made before Stage 3.2.</summary>
        [System.Xml.Serialization.XmlIgnore]
        public FilenameMode? FilenameMode { get; set; }

        /// <summary>XML form of <see cref="FilenameMode"/>; omitted when unknown so older files still load.</summary>
        [System.Xml.Serialization.XmlElement("FilenameMode")]
        public string FilenameModeText
        {
            get { return FilenameMode?.ToString(); }
            set
            {
                FilenameMode parsed;
                FilenameMode = Enum.TryParse(value, out parsed) ? parsed : (FilenameMode?)null;
            }
        }

        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

        public string FormatName => FileSystemFormats.Name(Format);

        public void ApplyVerification(VerificationResult result)
        {
            Verification = result.Passed ? HistoryVerificationStatus.Passed : HistoryVerificationStatus.Failed;
            VerificationDetail = result.Summary;
            VerifiedAt = result.Timestamp;
        }
    }
}
