using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DiscUtils.Iso9660;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Gives every file and folder a unique, valid name in the plain ISO 9660 tree.
    /// </summary>
    /// <remarks>
    /// DiscUtils 0.16.13 creates the plain ISO 9660 name ("short name") itself, and its
    /// generator has three faults: it never makes names unique (its source says
    /// "TODO: Make unique"), so "Project.csproj.nuget.g.props" and "...g.targets" both
    /// become HTISOCREATOR.CSP; it splits at the first dot, so
    /// "Microsoft.TestPlatform.Utilities.dll" becomes MICROSOFT.TES and loses its
    /// real extension; and it never shortens folder names or removes their dots.
    /// Programs that read the Joliet tree (Windows, 7-Zip, Linux by default) are not
    /// affected, but a reader that only understands plain ISO 9660 would see one file
    /// where there are several.
    ///
    /// This class replaces only the names DiscUtils had to change. A name that is
    /// already valid (for example every name in an "ISO 9660 only" image, which
    /// <see cref="IsoNameMapper"/> has already converted) keeps its DiscUtils name. The
    /// new names are written to DiscUtils's ShortName backing field, the only way to
    /// set them in this version; the field is checked by a unit test, and a missing field
    /// stops creation with an error instead of producing a damaged image.
    /// Directory records are sorted by DiscUtils when the image is built, after these
    /// names are set.
    /// </remarks>
    public static class IsoShortNames
    {
        /// <summary>Longest name (name + "." + extension, or folder name) written.</summary>
        public const int MaxNameLength = 30;

        private const int MaxExtensionLength = 8;

        private static readonly FieldInfo ShortNameField =
            typeof(BuildDirectoryMember).GetField("<ShortName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>True when this DiscUtils version allows the names to be replaced.</summary>
        public static bool IsSupported => ShortNameField != null && ShortNameField.FieldType == typeof(string);

        /// <summary>One item added to a <see cref="CDBuilder"/>.</summary>
        public sealed class Item
        {
            public Item(string imagePath, BuildDirectoryMember member, bool isDirectory)
            {
                ImagePath = imagePath;
                Member = member;
                IsDirectory = isDirectory;
            }

            /// <summary>Path inside the image, with backslashes (the Joliet path).</summary>
            public string ImagePath { get; }

            public BuildDirectoryMember Member { get; }

            public bool IsDirectory { get; }
        }

        /// <summary>
        /// Replaces DiscUtils's plain ISO 9660 names where needed so that every name is
        /// valid and unique within its folder. Call after all items are added and before
        /// <see cref="CDBuilder.Build"/>.
        /// </summary>
        public static void Apply(IEnumerable<Item> items)
        {
            if (!IsSupported)
            {
                throw new InvalidOperationException(
                    "This version of DiscUtils does not allow ISO 9660 names to be corrected, so the image cannot be created safely.");
            }

            foreach (IGrouping<string, Item> folder in items
                         .GroupBy(i => ParentOf(i.ImagePath), StringComparer.OrdinalIgnoreCase))
            {
                var used = new HashSet<string>(StringComparer.Ordinal);
                var toRename = new List<Item>();

                // Names DiscUtils left unchanged are already valid; they are kept and reserved first.
                foreach (Item item in folder.OrderBy(i => i.ImagePath, StringComparer.Ordinal))
                {
                    string current = item.Member.ShortName;
                    if (string.Equals(WithoutVersion(current), WithoutVersion(item.Member.Name), StringComparison.Ordinal) && used.Add(Key(current)))
                    {
                        continue;
                    }

                    toRename.Add(item);
                }

                foreach (Item item in toRename)
                {
                    string longName = WithoutVersion(item.Member.Name);
                    string name = item.IsDirectory
                        ? MakeDirectoryName(longName, used)
                        : MakeFileName(longName, used);
                    ShortNameField.SetValue(item.Member, name);
                }
            }
        }

        /// <summary>A valid, unique file name such as "MICROSOFT_TESTPLATFORM_UTI.DLL;1".</summary>
        internal static string MakeFileName(string longName, ISet<string> used)
        {
            int dot = longName.LastIndexOf('.');
            string stem = Clean(dot > 0 ? longName.Substring(0, dot) : longName);
            string extension = Clean(dot > 0 ? longName.Substring(dot + 1) : string.Empty);
            if (extension.Length > MaxExtensionLength)
            {
                extension = extension.Substring(0, MaxExtensionLength);
            }

            if (stem.Length == 0)
            {
                stem = "_";
            }

            int maxStem = MaxNameLength - extension.Length;
            for (int counter = 0; ; counter++)
            {
                string candidate = Fit(stem, maxStem, counter) + "." + extension;
                if (used.Add(Key(candidate)))
                {
                    return candidate + ";1";
                }
            }
        }

        /// <summary>A valid, unique folder name (no dots, at most 30 characters).</summary>
        internal static string MakeDirectoryName(string longName, ISet<string> used)
        {
            string stem = Clean(longName);
            if (stem.Length == 0)
            {
                stem = "_";
            }

            for (int counter = 0; ; counter++)
            {
                string candidate = Fit(stem, MaxNameLength, counter);
                if (used.Add(Key(candidate)))
                {
                    return candidate;
                }
            }
        }

        /// <summary>
        /// Comparison key: without the ";1" version and a trailing dot, so that file "A."
        /// and folder "A" also count as the same name.
        /// </summary>
        private static string Key(string name)
        {
            return WithoutVersion(name).TrimEnd('.').ToUpperInvariant();
        }

        /// <summary>DiscUtils stores file names with a ";1" version; the version is not part of the name.</summary>
        private static string WithoutVersion(string name)
        {
            int semicolon = name.IndexOf(';');
            return semicolon >= 0 ? name.Substring(0, semicolon) : name;
        }

        private static string Fit(string stem, int maxLength, int counter)
        {
            if (counter == 0)
            {
                return stem.Length <= maxLength ? stem : stem.Substring(0, maxLength);
            }

            string suffix = "_" + counter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int keep = Math.Max(0, Math.Min(stem.Length, maxLength - suffix.Length));
            return stem.Substring(0, keep) + suffix;
        }

        /// <summary>Upper case; everything except A-Z, 0-9 and _ becomes _.</summary>
        private static string Clean(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (char c in text.ToUpperInvariant())
            {
                builder.Append((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ? c : '_');
            }

            return builder.ToString();
        }

        private static string ParentOf(string imagePath)
        {
            string trimmed = imagePath.Trim('\\');
            int slash = trimmed.LastIndexOf('\\');
            return slash < 0 ? string.Empty : trimmed.Substring(0, slash);
        }
    }
}
