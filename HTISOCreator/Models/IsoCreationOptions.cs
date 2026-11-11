namespace HTISOCreator.Models
{
    /// <summary>
    /// File systems HT ISO Creator can write. ISO 9660 and ISO 9660 + Joliet are written by
    /// DiscUtils; the UDF formats by the IMAPI2 file system image component built into Windows.
    /// </summary>
    public enum IsoFileSystemFormat
    {
        /// <summary>ISO 9660 only: upper-case names, A-Z 0-9 _ characters, max 30 characters.</summary>
        Iso9660,

        /// <summary>ISO 9660 plus Joliet: Unicode names up to 64 characters.</summary>
        Iso9660Joliet,

        /// <summary>UDF only (written by Windows IMAPI2): names up to 127 characters, files of 4 GB and larger.</summary>
        Udf,

        /// <summary>ISO 9660 + Joliet + UDF bridge image (written by Windows IMAPI2).</summary>
        Iso9660JolietUdf
    }

    /// <summary>
    /// Everything the user chose for one ISO creation run.
    /// </summary>
    public sealed class IsoCreationOptions
    {
        public string SourceFolder { get; set; }

        public string OutputPath { get; set; }

        public string VolumeLabel { get; set; }

        public IsoFileSystemFormat Format { get; set; } = IsoFileSystemFormat.Iso9660Joliet;

        /// <summary>Strict Mode refuses any name change; Compatibility Mode needs <see cref="ConversionsApproved"/>.</summary>
        public FilenameMode FilenameMode { get; set; } = FilenameMode.Strict;

        /// <summary>
        /// Compatibility Mode only: the user has reviewed and confirmed every name conversion in
        /// the plan. <see cref="Services.IsoCreationService.Create"/> refuses to write converted
        /// names without it.
        /// </summary>
        public bool ConversionsApproved { get; set; }

        /// <summary>True when the user has confirmed that an existing output file may be replaced.</summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>Short reference written on every log line for this operation and shown to the user.</summary>
        public string OperationId { get; set; } = Services.LoggingService.NewReference();
    }
}
