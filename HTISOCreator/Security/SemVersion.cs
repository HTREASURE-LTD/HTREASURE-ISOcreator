using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HTISOCreator.Security
{
    /// <summary>Semantic Version 2.0.0. Precedence ignores build metadata, as the specification requires.</summary>
    public sealed class SemVersion : IComparable<SemVersion>, IEquatable<SemVersion>
    {
        private static readonly Regex Pattern = new Regex(
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
            RegexOptions.CultureInvariant);

        private SemVersion(int major, int minor, int patch, string pre, string build, string original)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = pre;
            Build = build;
            Original = original;
        }

        public int Major { get; private set; }

        public int Minor { get; private set; }

        public int Patch { get; private set; }

        public string Prerelease { get; private set; }

        public string Build { get; private set; }

        public string Original { get; private set; }

        public static bool TryParse(string text, out SemVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text) || text.Length > 128)
            {
                return false;
            }

            Match m = Pattern.Match(text.Trim());
            if (!m.Success)
            {
                return false;
            }

            int major, minor, patch;
            if (!int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out major) ||
                !int.TryParse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out minor) ||
                !int.TryParse(m.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out patch))
            {
                return false;
            }

            version = new SemVersion(major, minor, patch, m.Groups[4].Value, m.Groups[5].Value, text.Trim());
            return true;
        }

        public static SemVersion Parse(string text)
        {
            SemVersion v;
            if (!TryParse(text, out v))
            {
                throw new FormatException("'" + text + "' is not a valid semantic version.");
            }

            return v;
        }

        public static bool operator <(SemVersion a, SemVersion b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(SemVersion a, SemVersion b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(SemVersion a, SemVersion b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(SemVersion a, SemVersion b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator ==(SemVersion a, SemVersion b)
        {
            return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
        }

        public static bool operator !=(SemVersion a, SemVersion b)
        {
            return !(a == b);
        }

        public int CompareTo(SemVersion other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            int c = Major.CompareTo(other.Major);
            if (c != 0)
            {
                return c;
            }

            c = Minor.CompareTo(other.Minor);
            if (c != 0)
            {
                return c;
            }

            c = Patch.CompareTo(other.Patch);
            return c != 0 ? c : ComparePrerelease(Prerelease, other.Prerelease);
        }

        public bool Equals(SemVersion other)
        {
            return !ReferenceEquals(other, null) && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SemVersion);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (((((Major * 397) ^ Minor) * 397) ^ Patch) * 397) ^ StringComparer.Ordinal.GetHashCode(Prerelease);
            }
        }

        public override string ToString()
        {
            return Original;
        }

        private static int ComparePrerelease(string a, string b)
        {
            if (a.Length == 0 && b.Length == 0)
            {
                return 0;
            }

            if (a.Length == 0)
            {
                return 1; // a release has higher precedence than its prereleases
            }

            if (b.Length == 0)
            {
                return -1;
            }

            string[] pa = a.Split('.');
            string[] pb = b.Split('.');
            for (int i = 0; i < Math.Min(pa.Length, pb.Length); i++)
            {
                long ia, ib;
                bool na = long.TryParse(pa[i], NumberStyles.None, CultureInfo.InvariantCulture, out ia);
                bool nb = long.TryParse(pb[i], NumberStyles.None, CultureInfo.InvariantCulture, out ib);
                int c;
                if (na && nb)
                {
                    c = ia.CompareTo(ib);
                }
                else if (na)
                {
                    c = -1;
                }
                else if (nb)
                {
                    c = 1;
                }
                else
                {
                    c = string.CompareOrdinal(pa[i], pb[i]);
                }

                if (c != 0)
                {
                    return c;
                }
            }

            return pa.Length.CompareTo(pb.Length);
        }
    }
}
