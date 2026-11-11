namespace HTISOCreator.Models
{
    public enum AppTheme
    {
        /// <summary>Follow the Windows app colour setting.</summary>
        System,
        Light,
        Dark
    }

    /// <summary>
    /// Per-user preferences, stored by <see cref="Services.SettingsService"/>.
    /// Public setters and a parameterless constructor are required by XmlSerializer.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>Folder for suggested output files; empty means "next to the source folder".</summary>
        public string DefaultOutputFolder { get; set; } = string.Empty;

        /// <summary>Volume label to suggest; empty means "derived from the source folder name".</summary>
        public string DefaultVolumeLabel { get; set; } = string.Empty;

        public IsoFileSystemFormat DefaultFormat { get; set; } = IsoFileSystemFormat.Iso9660Joliet;

        public bool RememberLastSource { get; set; } = true;

        public bool RememberLastOutput { get; set; } = true;

        public string LastSourceFolder { get; set; } = string.Empty;

        public string LastOutputFolder { get; set; } = string.Empty;

        public bool ShowCompletionDialog { get; set; } = true;

        public AppTheme Theme { get; set; } = AppTheme.System;

        /// <summary>Verify each new image (structure, paths and sizes) right after creating it.</summary>
        public bool AutoVerify { get; set; } = true;

        /// <summary>Include a SHA-256 comparison of every file when verifying (reads all data again).</summary>
        public bool VerifyHashes { get; set; }

        /// <summary>Also mount images with Windows when verifying.</summary>
        public bool VerifyWithWindowsMount { get; set; }

        /// <summary>Selected <see cref="CapacityProfile"/> id.</summary>
        public string CapacityProfileId { get; set; } = CapacityProfile.NoLimitId;

        /// <summary>Capacity used by the "Custom size" profile, in MB (1,048,576 bytes).</summary>
        public int CustomCapacityMegabytes { get; set; } = 4400;

        /// <summary>
        /// Strict Mode (default) refuses to create an image in which any name would change;
        /// Compatibility Mode allows conversions after the user confirms them.
        /// </summary>
        public FilenameMode FilenameMode { get; set; } = FilenameMode.Strict;

        public AppSettings Clone()
        {
            return (AppSettings)MemberwiseClone();
        }
    }
}
