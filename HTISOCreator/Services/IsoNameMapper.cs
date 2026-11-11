using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Converts source file and folder names into names the selected image file system can store.
    ///
    /// DiscUtils does not enforce these limits itself: in testing, Joliet names longer than
    /// 64 characters and ISO 9660 names longer than 30 characters produced images Windows
    /// reported as corrupt. Every name is therefore mapped here, collisions are resolved,
    /// and each change is reported to the user before anything is written.
    /// </summary>
    public static class IsoNameMapper
    {
        public const int JolietMaxNameLength = 64;
        public const int Iso9660MaxNameLength = 30;
        public const int Iso9660MaxExtensionLength = 8;
        public const int Iso9660RecommendedMaxDepth = 8;

        /// <summary>Text included in a change's reason when a numbered suffix was needed to avoid a collision.</summary>
        public const string DuplicateReason = "duplicate name after conversion";

        private const string JolietInvalidCharacters = "*/:;?\\";

        public sealed class MappingResult
        {
            /// <summary>Source relative path to image relative path, for every directory.</summary>
            public Dictionary<string, string> Directories { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>Source relative path to image relative path, for every file.</summary>
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>Items whose own name changed (descendants of a renamed folder are not repeated).</summary>
            public List<IsoNameChange> Changes { get; } = new List<IsoNameChange>();
        }

        private sealed class Node
        {
            public string Name;
            public string SourcePath;
            public bool IsDirectory;
            public string ImagePath;
            public readonly Dictionary<string, Node> Children = new Dictionary<string, Node>(StringComparer.Ordinal);
        }

        public static MappingResult Map(IEnumerable<string> directories, IEnumerable<string> files, IsoFileSystemFormat format)
        {
            var root = new Node { Name = string.Empty, SourcePath = string.Empty, IsDirectory = true, ImagePath = string.Empty };
            foreach (string directory in directories)
            {
                Insert(root, directory, true);
            }

            foreach (string file in files)
            {
                Insert(root, file, false);
            }

            var result = new MappingResult();
            var pending = new Stack<Node>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                Node parent = pending.Pop();
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Unchanged names claim their slot first so that only problem names get suffixes.
                IEnumerable<Node> ordered = parent.Children.Values
                    .OrderBy(n => MapComponent(n.Name, n.IsDirectory, format) == n.Name ? 0 : 1)
                    .ThenBy(n => n.IsDirectory ? 0 : 1)
                    .ThenBy(n => n.Name, StringComparer.Ordinal);

                foreach (Node child in ordered)
                {
                    string candidate = MapComponent(child.Name, child.IsDirectory, format);
                    string unique = candidate;
                    for (int n = 1; used.Contains(unique); n++)
                    {
                        unique = MakeUnique(candidate, child.IsDirectory, n, format);
                    }

                    used.Add(unique);
                    child.ImagePath = parent.ImagePath.Length == 0 ? unique : parent.ImagePath + "\\" + unique;
                    if (!string.Equals(unique, child.Name, StringComparison.Ordinal))
                    {
                        result.Changes.Add(new IsoNameChange(child.SourcePath, child.ImagePath, child.IsDirectory,
                            DescribeChange(child.Name, candidate, unique, child.IsDirectory, format)));
                    }

                    if (child.IsDirectory)
                    {
                        result.Directories[child.SourcePath] = child.ImagePath;
                        pending.Push(child);
                    }
                    else
                    {
                        result.Files[child.SourcePath] = child.ImagePath;
                    }
                }
            }

            result.Changes.Sort((a, b) => string.Compare(a.SourcePath, b.SourcePath, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        /// <summary>Maps one path component (no separators) to a name valid for <paramref name="format"/>.</summary>
        public static string MapComponent(string name, bool isDirectory, IsoFileSystemFormat format)
        {
            if (FileSystemFormats.HasUdf(format))
            {
                return MapUdf(name);
            }

            return format == IsoFileSystemFormat.Iso9660Joliet
                ? MapJoliet(name)
                : MapIso9660(name, isDirectory);
        }

        /// <summary>
        /// UDF (as written by IMAPI2) stores any Windows file name up to 127 characters. Longer names
        /// are shortened here, keeping the extension, so that IMAPI2 never shortens one silently.
        /// </summary>
        private static string MapUdf(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(c < 0x20 ? '_' : c);
            }

            string mapped = sb.ToString();
            return mapped.Length <= FileSystemFormats.UdfMaxNameLength ? mapped : Shorten(mapped, string.Empty, FileSystemFormats.UdfMaxNameLength);
        }

        /// <summary>
        /// Explains why a name changed. Only describes the conversion; it does not alter it.
        /// </summary>
        public static string DescribeChange(string original, string candidate, string unique, bool isDirectory, IsoFileSystemFormat format)
        {
            var reasons = new List<string>();
            if (FileSystemFormats.HasUdf(format))
            {
                if (original.Any(c => c < 0x20))
                {
                    reasons.Add("control characters replaced with _");
                }

                if (original.Length > FileSystemFormats.UdfMaxNameLength)
                {
                    reasons.Add("longer than " + FileSystemFormats.UdfMaxNameLength + " characters (UDF limit)");
                }
            }
            else if (format == IsoFileSystemFormat.Iso9660Joliet)
            {
                if (original.Any(c => c < 0x20 || JolietInvalidCharacters.IndexOf(c) >= 0))
                {
                    reasons.Add("unsupported characters replaced with _");
                }

                if (original.Length > JolietMaxNameLength)
                {
                    reasons.Add("longer than " + JolietMaxNameLength + " characters");
                }
            }
            else
            {
                string withoutAccents = RemoveDiacritics(original);
                if (withoutAccents != original)
                {
                    reasons.Add("accents removed");
                }

                if (original.Any(char.IsLower))
                {
                    reasons.Add("converted to upper case");
                }

                string cleaned = isDirectory ? withoutAccents : withoutAccents.Replace(".", string.Empty);
                if (cleaned.Any(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) ||
                    (!isDirectory && original.Count(c => c == '.') > 1) || (isDirectory && original.Contains(".")))
                {
                    reasons.Add("unsupported characters replaced with _");
                }

                int dot = original.LastIndexOf('.');
                bool hasExtension = !isDirectory && dot > 0;
                if (hasExtension && original.Length - dot - 1 > Iso9660MaxExtensionLength)
                {
                    reasons.Add("extension longer than " + Iso9660MaxExtensionLength + " characters");
                }

                if (candidate.Length < original.Length || original.Length > Iso9660MaxNameLength)
                {
                    reasons.Add("longer than " + Iso9660MaxNameLength + " characters");
                }
            }

            if (!string.Equals(candidate, unique, StringComparison.Ordinal))
            {
                reasons.Add(DuplicateReason);
            }

            return reasons.Count == 0 ? "converted to a valid name" : string.Join("; ", reasons.Distinct());
        }

        private static void Insert(Node root, string relativePath, bool isDirectory)
        {
            string[] parts = relativePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            Node current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                bool last = i == parts.Length - 1;
                Node child;
                if (!current.Children.TryGetValue(parts[i], out child))
                {
                    child = new Node
                    {
                        Name = parts[i],
                        SourcePath = current.SourcePath.Length == 0 ? parts[i] : current.SourcePath + "\\" + parts[i],
                        IsDirectory = !last || isDirectory
                    };
                    current.Children.Add(parts[i], child);
                }

                current = child;
            }
        }

        private static string MapJoliet(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(c < 0x20 || JolietInvalidCharacters.IndexOf(c) >= 0 ? '_' : c);
            }

            string mapped = sb.ToString();
            return mapped.Length <= JolietMaxNameLength ? mapped : Shorten(mapped, string.Empty, JolietMaxNameLength);
        }

        private static string MapIso9660(string name, bool isDirectory)
        {
            string upper = RemoveDiacritics(name).ToUpperInvariant();
            if (isDirectory)
            {
                string dir = CleanIso(upper);
                if (dir.Length > Iso9660MaxNameLength)
                {
                    dir = dir.Substring(0, Iso9660MaxNameLength);
                }

                return dir.Length == 0 ? "_" : dir;
            }

            string baseName;
            string extension;
            SplitIsoFileName(upper, out baseName, out extension);
            return ComposeIsoFileName(baseName, extension, string.Empty);
        }

        private static void SplitIsoFileName(string upper, out string baseName, out string extension)
        {
            int dot = upper.LastIndexOf('.');
            if (dot > 0)
            {
                baseName = CleanIso(upper.Substring(0, dot));
                extension = CleanIso(upper.Substring(dot + 1));
            }
            else
            {
                baseName = CleanIso(upper);
                extension = string.Empty;
            }

            if (extension.Length > Iso9660MaxExtensionLength)
            {
                extension = extension.Substring(0, Iso9660MaxExtensionLength);
            }

            if (baseName.Length == 0)
            {
                baseName = "_";
            }
        }

        /// <summary>
        /// The dot is always counted because DiscUtils writes "NAME." for files without an extension.
        /// </summary>
        private static string ComposeIsoFileName(string baseName, string extension, string suffix)
        {
            int maxBase = Iso9660MaxNameLength - 1 - extension.Length - suffix.Length;
            if (baseName.Length > maxBase)
            {
                baseName = baseName.Substring(0, maxBase);
            }

            string name = baseName + suffix;
            return extension.Length > 0 ? name + "." + extension : name;
        }

        private static string MakeUnique(string candidate, bool isDirectory, int n, IsoFileSystemFormat format)
        {
            if (FileSystemFormats.HasUdf(format))
            {
                return Shorten(candidate, "~" + n.ToString(CultureInfo.InvariantCulture), FileSystemFormats.UdfMaxNameLength);
            }

            if (format == IsoFileSystemFormat.Iso9660Joliet)
            {
                return Shorten(candidate, "~" + n.ToString(CultureInfo.InvariantCulture), JolietMaxNameLength);
            }

            string suffix = "_" + n.ToString(CultureInfo.InvariantCulture);
            if (isDirectory)
            {
                string cut = candidate.Length > Iso9660MaxNameLength - suffix.Length
                    ? candidate.Substring(0, Iso9660MaxNameLength - suffix.Length)
                    : candidate;
                return cut + suffix;
            }

            string baseName;
            string extension;
            SplitIsoFileName(candidate, out baseName, out extension);
            return ComposeIsoFileName(baseName, extension, suffix);
        }

        /// <summary>
        /// Shortens a Joliet name to <paramref name="max"/> characters, keeping a short extension
        /// and inserting <paramref name="suffix"/> before it. Never splits a surrogate pair.
        /// </summary>
        private static string Shorten(string name, string suffix, int max)
        {
            int dot = name.LastIndexOf('.');
            string extension = dot > 0 && name.Length - dot <= 16 ? name.Substring(dot) : string.Empty;
            string baseName = name.Substring(0, name.Length - extension.Length);
            int maxBase = Math.Max(1, max - extension.Length - suffix.Length);
            if (baseName.Length > maxBase)
            {
                int cut = maxBase;
                if (char.IsHighSurrogate(baseName[cut - 1]))
                {
                    cut--;
                }

                baseName = baseName.Substring(0, cut);
            }

            return baseName + suffix + extension;
        }

        private static string CleanIso(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' ? c : '_');
            }

            return sb.ToString();
        }

        private static string RemoveDiacritics(string value)
        {
            string decomposed = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
