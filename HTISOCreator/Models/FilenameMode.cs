namespace HTISOCreator.Models
{
    /// <summary>What happens when a source name cannot be stored exactly in the selected file system.</summary>
    public enum FilenameMode
    {
        /// <summary>
        /// Strict Mode - Preserve Original Filenames. Creation is refused if any file or folder
        /// would have a different name in the names readers show (Joliet names, or ISO 9660
        /// names when Joliet is off). The default.
        /// </summary>
        Strict,

        /// <summary>
        /// Compatibility Mode - Allow Filename Conversion. Names are converted, but only after the
        /// user has reviewed every conversion and confirmed it.
        /// </summary>
        Compatibility
    }
}
