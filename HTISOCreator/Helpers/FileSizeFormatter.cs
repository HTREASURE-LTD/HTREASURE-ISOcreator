using System;
using System.Globalization;

namespace HTISOCreator.Helpers
{
    public static class FileSizeFormatter
    {
        private static readonly string[] Units = { "bytes", "KB", "MB", "GB", "TB" };

        /// <summary>Formats a byte count using binary (1024) units, e.g. "4.38 GB".</summary>
        public static string Format(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes.ToString("N0", CultureInfo.CurrentCulture) + (bytes == 1 ? " byte" : " bytes");
            }

            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < Units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return value.ToString(value >= 100 ? "0" : value >= 10 ? "0.0" : "0.00", CultureInfo.CurrentCulture) + " " + Units[unit];
        }

        /// <summary>Formats a size as e.g. "4.38 GB (4,700,000,000 bytes)".</summary>
        public static string FormatWithBytes(long bytes)
        {
            return bytes < 1024
                ? Format(bytes)
                : Format(bytes) + " (" + bytes.ToString("N0", CultureInfo.CurrentCulture) + " bytes)";
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            return duration.TotalHours >= 1
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", duration.Minutes, duration.Seconds);
        }
    }
}
