using System.Collections.Generic;

namespace HTISOCreator.Models
{
    /// <summary>Names, engines and limits of each <see cref="IsoFileSystemFormat"/>.</summary>
    public static class FileSystemFormats
    {
        /// <summary>UDF name limit in IMAPI2: 127 UTF-16 characters (254 bytes plus the compression byte).</summary>
        public const int UdfMaxNameLength = 127;

        /// <summary>Offered formats, in the order shown to the user.</summary>
        public static readonly IReadOnlyList<IsoFileSystemFormat> All = new[]
        {
            IsoFileSystemFormat.Iso9660Joliet,
            IsoFileSystemFormat.Udf,
            IsoFileSystemFormat.Iso9660JolietUdf,
            IsoFileSystemFormat.Iso9660
        };

        public static string Name(IsoFileSystemFormat format)
        {
            switch (format)
            {
                case IsoFileSystemFormat.Iso9660Joliet:
                    return "ISO 9660 + Joliet";
                case IsoFileSystemFormat.Udf:
                    return "UDF";
                case IsoFileSystemFormat.Iso9660JolietUdf:
                    return "ISO 9660 + Joliet + UDF";
                default:
                    return "ISO 9660";
            }
        }

        /// <summary>Text for the file system lists.</summary>
        public static string Describe(IsoFileSystemFormat format)
        {
            switch (format)
            {
                case IsoFileSystemFormat.Iso9660Joliet:
                    return "ISO 9660 + Joliet (64-character names)";
                case IsoFileSystemFormat.Udf:
                    return "UDF (127-character names, files over 4 GB)";
                case IsoFileSystemFormat.Iso9660JolietUdf:
                    return "ISO 9660 + Joliet + UDF (hybrid)";
                default:
                    return "ISO 9660 only (8.3-style names)";
            }
        }

        public static string EngineName(IsoFileSystemFormat format)
        {
            return UsesImapi(format) ? "Windows IMAPI2" : "DiscUtils";
        }

        public static bool UsesImapi(IsoFileSystemFormat format)
        {
            return format == IsoFileSystemFormat.Udf || format == IsoFileSystemFormat.Iso9660JolietUdf;
        }

        public static bool HasUdf(IsoFileSystemFormat format)
        {
            return UsesImapi(format);
        }

        public static bool HasJoliet(IsoFileSystemFormat format)
        {
            return format == IsoFileSystemFormat.Iso9660Joliet || format == IsoFileSystemFormat.Iso9660JolietUdf;
        }

        /// <summary>Names readers show come from UDF, Joliet or ISO 9660 (in that order of preference).</summary>
        public static string VisibleNamesSource(IsoFileSystemFormat format)
        {
            return HasUdf(format) ? "UDF" : HasJoliet(format) ? "Joliet" : "ISO 9660";
        }

        /// <summary>Largest file the format can store: ISO 9660 has 32-bit sizes; UDF-only images have 64-bit sizes.</summary>
        public static long MaxFileSize(IsoFileSystemFormat format)
        {
            return format == IsoFileSystemFormat.Udf ? long.MaxValue : uint.MaxValue;
        }
    }
}
