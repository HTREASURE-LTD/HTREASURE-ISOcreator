using System;
using System.Collections.Generic;
using System.Linq;

namespace HTISOCreator.Models
{
    /// <summary>A file or directory read from an image's directory records.</summary>
    public sealed class IsoImageEntry
    {
        public IsoImageEntry(string path, bool isDirectory, long extentSector, long length, DateTime? recordedTime)
        {
            Path = path;
            IsDirectory = isDirectory;
            ExtentSector = extentSector;
            Length = length;
            RecordedTime = recordedTime;
        }

        /// <summary>Path inside the image with '\' separators, without the ";1" version suffix.</summary>
        public string Path { get; }

        public bool IsDirectory { get; }

        /// <summary>First 2048-byte sector of the data.</summary>
        public long ExtentSector { get; }

        public long Length { get; }

        /// <summary>The date stored in the directory record, as UTC; null when the record holds no date.</summary>
        public DateTime? RecordedTime { get; }

        /// <summary>
        /// UDF only: the file's data as {byte offset, length} ranges in the image, in order. An offset of
        /// -1 means a range that reads as zeros. Null for ISO 9660 / Joliet entries (one extent).
        /// </summary>
        public List<long[]> DataRanges { get; set; }

        /// <summary>UDF only: data embedded in the file entry itself (small files).</summary>
        public byte[] InlineData { get; set; }
    }

    /// <summary>
    /// What <see cref="Services.IsoImageReader"/> found in an image. Every value is read from the
    /// image itself; values the image does not record are null rather than guessed.
    /// </summary>
    public sealed class IsoImageInfo
    {
        public string FilePath { get; set; }

        public long FileLength { get; set; }

        /// <summary>Size declared by the primary volume descriptor (volume space size x 2048).</summary>
        public long DeclaredVolumeBytes { get; set; }

        public string VolumeLabel { get; set; }

        public string JolietVolumeLabel { get; set; }

        public bool HasJoliet { get; set; }

        /// <summary>An El Torito boot record volume descriptor is present.</summary>
        public bool HasBootRecord { get; set; }

        public string SystemIdentifier { get; set; }

        public string PublisherIdentifier { get; set; }

        public string PreparerIdentifier { get; set; }

        public string ApplicationIdentifier { get; set; }

        public DateTime? VolumeCreationTime { get; set; }

        public DateTime? VolumeModificationTime { get; set; }

        /// <summary>
        /// Entries of the tree Windows shows: UDF when present, otherwise Joliet when present,
        /// otherwise ISO 9660.
        /// </summary>
        public List<IsoImageEntry> Entries { get; } = new List<IsoImageEntry>();

        /// <summary>Entries of the Joliet tree (empty when the image has no Joliet names).</summary>
        public List<IsoImageEntry> JolietEntries { get; } = new List<IsoImageEntry>();

        /// <summary>Entries of the UDF file system (empty when the image has no UDF).</summary>
        public List<IsoImageEntry> UdfEntries { get; } = new List<IsoImageEntry>();

        /// <summary>The image has an ISO 9660 primary volume descriptor.</summary>
        public bool HasIso9660 { get; set; }

        /// <summary>The image has a readable UDF file system.</summary>
        public bool HasUdf { get; set; }

        public string UdfVolumeLabel { get; set; }

        /// <summary>UDF revision from the logical volume descriptor, e.g. "1.02".</summary>
        public string UdfRevision { get; set; }

        /// <summary>End of the UDF partition in bytes.</summary>
        public long UdfPartitionEndBytes { get; set; }

        /// <summary>Entries of the ISO 9660 (primary) tree.</summary>
        public List<IsoImageEntry> PrimaryEntries { get; } = new List<IsoImageEntry>();

        /// <summary>Structural problems. Any entry here means the image is damaged or non-standard.</summary>
        public List<string> Problems { get; } = new List<string>();

        public int FileCount => Entries.Count(e => !e.IsDirectory);

        public int DirectoryCount => Entries.Count(e => e.IsDirectory);

        public IsoFileSystemFormat Format =>
            HasUdf ? (HasJoliet ? IsoFileSystemFormat.Iso9660JolietUdf : IsoFileSystemFormat.Udf)
            : HasJoliet ? IsoFileSystemFormat.Iso9660Joliet
            : IsoFileSystemFormat.Iso9660;

        /// <summary>Label as Windows shows it: UDF, then Joliet, then ISO 9660.</summary>
        public string DisplayLabel => UdfVolumeLabel ?? JolietVolumeLabel ?? VolumeLabel;
    }
}
