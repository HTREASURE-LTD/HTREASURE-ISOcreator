using System.Collections.Generic;

namespace HTISOCreator.Models
{
    /// <summary>
    /// A source item and the name it will have inside the image.
    /// </summary>
    public sealed class IsoNameChange
    {
        public IsoNameChange(string sourcePath, string imagePath, bool isDirectory, string reason = "")
        {
            SourcePath = sourcePath;
            ImagePath = imagePath;
            IsDirectory = isDirectory;
            Reason = reason;
        }

        public string SourcePath { get; }

        public string ImagePath { get; }

        public bool IsDirectory { get; }

        /// <summary>Why the name changed, e.g. "longer than 64 characters; duplicate name after conversion".</summary>
        public string Reason { get; }
    }

    public sealed class PlannedFile
    {
        public PlannedFile(SourceFileEntry source, string imagePath)
        {
            Source = source;
            ImagePath = imagePath;
        }

        public SourceFileEntry Source { get; }

        public string ImagePath { get; }
    }

    /// <summary>
    /// Validated layout of the image, produced before any data is written so the user can
    /// review errors, warnings and renamed items.
    /// </summary>
    public sealed class IsoBuildPlan
    {
        public IsoBuildPlan(IsoCreationOptions options, FolderAnalysisResult analysis)
        {
            Options = options;
            Analysis = analysis;
        }

        public IsoCreationOptions Options { get; }

        public FolderAnalysisResult Analysis { get; }

        public List<PlannedFile> Files { get; } = new List<PlannedFile>();

        /// <summary>Image paths of all directories, parents before children.</summary>
        public List<string> Directories { get; } = new List<string>();

        /// <summary>Last-modified time (UTC) of each source directory, keyed by image path.</summary>
        public Dictionary<string, System.DateTime> DirectoryTimesUtc { get; } = new Dictionary<string, System.DateTime>(System.StringComparer.Ordinal);

        /// <summary>Exact image size calculated from the layout, or null when no image can be built.</summary>
        public long? ImageSize { get; set; }

        /// <summary>Names that had to be changed to satisfy the selected file system.</summary>
        public List<IsoNameChange> RenamedItems { get; } = new List<IsoNameChange>();

        /// <summary>
        /// Set when Strict Mode blocks creation because names would change. The same message is
        /// also in <see cref="Errors"/>; the renamed items themselves are in <see cref="RenamedItems"/>.
        /// </summary>
        public string StrictModeError { get; set; }

        /// <summary>Problems that prevent creation.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Compatibility notes the user should see but which do not prevent creation.</summary>
        public List<string> Warnings { get; } = new List<string>();

        public bool CanCreate => Errors.Count == 0;
    }
}
