using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    public static class SettingsLimits
    {
        /// <summary>Upper bound for a custom capacity: 1 TB, far beyond any optical disc.</summary>
        public const int MaxCustomCapacityMegabytes = 1024 * 1024;
    }

    /// <summary>
    /// Loads and saves <see cref="AppSettings"/> as XML in the user's roaming application data
    /// folder. No administrator rights are needed. A missing or damaged file yields defaults.
    /// </summary>
    public sealed class SettingsService
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AppSettings));
        private readonly LoggingService _log;

        public SettingsService()
            : this(DefaultSettingsFile, LoggingService.Instance)
        {
        }

        public SettingsService(string settingsFile, LoggingService log)
        {
            SettingsFile = settingsFile;
            _log = log;
        }

        public static string DefaultSettingsFile =>
            Path.Combine(Helpers.AppPaths.RoamingFolder, "settings.xml");

        public string SettingsFile { get; }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    return new AppSettings();
                }

                using (FileStream stream = File.OpenRead(SettingsFile))
                {
                    var settings = (AppSettings)Serializer.Deserialize(stream);
                    return Sanitize(settings ?? new AppSettings());
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is XmlException)
            {
                _log.Error("Settings could not be read from '" + SettingsFile + "'; defaults are used", ex);
                return new AppSettings();
            }
        }

        /// <summary>Saves atomically (write to a temporary file, then replace). Returns false on failure.</summary>
        public bool Save(AppSettings settings)
        {
            string temp = SettingsFile + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
                using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
                {
                    Serializer.Serialize(writer, settings);
                }

                if (File.Exists(SettingsFile))
                {
                    File.Replace(temp, SettingsFile, null);
                }
                else
                {
                    File.Move(temp, SettingsFile);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                _log.Error("Settings could not be saved to '" + SettingsFile + "'", ex);
                try
                {
                    File.Delete(temp);
                }
                catch (Exception)
                {
                    // Best effort cleanup.
                }

                return false;
            }
        }

        /// <summary>Drops values from the file that are not usable, rather than failing later.</summary>
        private static AppSettings Sanitize(AppSettings settings)
        {
            settings.DefaultOutputFolder = settings.DefaultOutputFolder ?? string.Empty;
            settings.LastSourceFolder = settings.LastSourceFolder ?? string.Empty;
            settings.LastOutputFolder = settings.LastOutputFolder ?? string.Empty;
            settings.DefaultVolumeLabel = settings.DefaultVolumeLabel ?? string.Empty;

            if (!Enum.IsDefined(typeof(IsoFileSystemFormat), settings.DefaultFormat))
            {
                settings.DefaultFormat = IsoFileSystemFormat.Iso9660Joliet;
            }

            if (!Enum.IsDefined(typeof(AppTheme), settings.Theme))
            {
                settings.Theme = AppTheme.System;
            }

            settings.CapacityProfileId = CapacityProfile.Find(settings.CapacityProfileId).Id;
            if (settings.CustomCapacityMegabytes < 1 || settings.CustomCapacityMegabytes > SettingsLimits.MaxCustomCapacityMegabytes)
            {
                settings.CustomCapacityMegabytes = 4400;
            }

            if (settings.DefaultVolumeLabel.Length > 0 &&
                VolumeLabelValidator.Validate(settings.DefaultVolumeLabel, IsoFileSystemFormat.Iso9660) != null)
            {
                settings.DefaultVolumeLabel = string.Empty;
            }

            return settings;
        }
    }
}
