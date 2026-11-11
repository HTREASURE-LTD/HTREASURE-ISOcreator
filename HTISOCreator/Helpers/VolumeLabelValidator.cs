using System.Globalization;
using System.Text;
using HTISOCreator.Models;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// Volume label rules. The label is written to the ISO 9660 primary volume descriptor,
    /// which only permits d-characters (A-Z, 0-9, _), up to 32 characters. When Joliet is
    /// enabled the same label goes into the Joliet descriptor, which holds 16 characters
    /// (DiscUtils silently truncates longer labels, so they are rejected here instead).
    /// </summary>
    public static class VolumeLabelValidator
    {
        public const int Iso9660MaxLength = 32;
        public const int JolietMaxLength = 16;
        public const string DefaultLabel = "ISO_IMAGE";

        public static int GetMaxLength(IsoFileSystemFormat format)
        {
            // The UDF formats use the same label in their Joliet / UDF descriptors; 16 characters fits all of them.
            return format == IsoFileSystemFormat.Iso9660 ? Iso9660MaxLength : JolietMaxLength;
        }

        public static bool IsValidCharacter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
        }

        /// <summary>Returns null when valid, otherwise a user-facing explanation.</summary>
        public static string Validate(string label, IsoFileSystemFormat format)
        {
            if (string.IsNullOrEmpty(label))
            {
                return "Enter a volume name.";
            }

            int max = GetMaxLength(format);
            if (label.Length > max)
            {
                return format == IsoFileSystemFormat.Iso9660
                    ? "The volume name can have at most " + max + " characters."
                    : "With " + FileSystemFormats.Name(format) + " the volume name can have at most " + max + " characters.";
            }

            foreach (char c in label)
            {
                if (!IsValidCharacter(c))
                {
                    return "The volume name may only contain the letters A-Z, digits 0-9 and underscore (_). '" + c + "' is not allowed.";
                }
            }

            return null;
        }

        /// <summary>Builds a valid label from a folder name, e.g. "Linux Tools (v2)" becomes "LINUX_TOOLS_V2".</summary>
        public static string Suggest(string folderName, IsoFileSystemFormat format)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return DefaultLabel;
            }

            string decomposed = folderName.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            bool lastWasUnderscore = false;
            foreach (char raw in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(raw) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                char c = char.ToUpperInvariant(raw);
                if (IsValidCharacter(c) && c != '_')
                {
                    sb.Append(c);
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                    lastWasUnderscore = true;
                }
            }

            string result = sb.ToString().TrimEnd('_');
            int max = GetMaxLength(format);
            if (result.Length > max)
            {
                result = result.Substring(0, max).TrimEnd('_');
            }

            return result.Length == 0 ? DefaultLabel : result;
        }
    }
}
