using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>Thrown when a file is not an ISO 9660 image at all.</summary>
    public sealed class InvalidIsoImageException : Exception
    {
        public InvalidIsoImageException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// A small, self-contained ISO 9660 / Joliet reader written from the ECMA-119 and Joliet
    /// specifications, with <see cref="UdfImageReader"/> for UDF and UDF bridge images. It deliberately does not use DiscUtils, so that verifying an image does not
    /// rely on the same code that wrote it. It reads volume descriptors and walks both directory
    /// trees, checking that every record and data extent lies inside the file.
    /// </summary>
    public static class IsoImageReader
    {
        public const int SectorSize = 2048;
        private const int FirstDescriptorSector = 16;
        private const int MaxDescriptors = 64;
        private const int MaxDepth = 64;
        private const int MaxDirectoryBytes = 64 * 1024 * 1024;
        private const int MaxProblems = 200;

        /// <summary>Text in a problem that reports two entries with the same name in one folder.</summary>
        public const string DuplicateEntryText = "appears more than once in the same folder";

        public static IsoImageInfo Read(string path, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024))
            {
                return Read(stream, path, cancellationToken);
            }
        }

        public static IsoImageInfo Read(Stream stream, string path, CancellationToken cancellationToken)
        {
            var info = new IsoImageInfo { FilePath = path, FileLength = stream.Length };
            if (stream.Length < (FirstDescriptorSector + 2) * SectorSize)
            {
                throw new InvalidIsoImageException("The file is " + FileSizeFormatter.Format(stream.Length) +
                                                   ", which is too small to be an ISO 9660 image.");
            }

            byte[] primaryRoot = null;
            byte[] jolietRoot = null;
            bool terminated = false;
            bool udfAnchor = UdfImageReader.HasAnchor(stream);
            for (int i = 0; i < MaxDescriptors; i++)
            {
                long sector = FirstDescriptorSector + i;
                if ((sector + 1) * SectorSize > stream.Length)
                {
                    break;
                }

                byte[] descriptor = ReadBytes(stream, sector * SectorSize, SectorSize);
                if (Encoding.ASCII.GetString(descriptor, 1, 5) != "CD001")
                {
                    if (i == 0)
                    {
                        if (udfAnchor)
                        {
                            // A UDF-only image: sector 16 starts the UDF volume recognition sequence instead.
                            terminated = true;
                            break;
                        }

                        throw new InvalidIsoImageException("No ISO 9660 or UDF volume descriptor was found. The file is not an ISO 9660 image or a UDF image (it may be a different disc format or another kind of file).");
                    }

                    AddProblem(info, "The volume descriptor list is not terminated correctly.");
                    break;
                }

                byte type = descriptor[0];
                if (type == 255)
                {
                    terminated = true;
                    break;
                }

                if (type == 0)
                {
                    info.HasBootRecord = true;
                }
                else if (type == 1 && primaryRoot == null)
                {
                    primaryRoot = ReadPrimaryDescriptor(descriptor, info);
                }
                else if (type == 2 && descriptor[88] == 0x25 && descriptor[89] == 0x2F &&
                         (descriptor[90] == 0x40 || descriptor[90] == 0x43 || descriptor[90] == 0x45))
                {
                    info.HasJoliet = true;
                    info.JolietVolumeLabel = TrimField(Encoding.BigEndianUnicode.GetString(descriptor, 40, 32));
                    jolietRoot = Slice(descriptor, 156, 34);
                }
            }

            if (primaryRoot == null && !udfAnchor)
            {
                throw new InvalidIsoImageException("The image has no primary volume descriptor, so it is not a valid ISO 9660 image.");
            }

            if (!terminated)
            {
                AddProblem(info, "No volume descriptor set terminator was found.");
            }

            info.HasIso9660 = primaryRoot != null;
            if (udfAnchor)
            {
                UdfImageReader.ReadInto(stream, info, problem => AddProblem(info, problem), cancellationToken);
                if (!info.HasUdf && primaryRoot == null)
                {
                    throw new InvalidIsoImageException("The image has a UDF anchor but its UDF file system could not be read: " +
                                                       (info.Problems.Count > 0 ? info.Problems[0] : "unknown problem") + ".");
                }

                if (primaryRoot == null)
                {
                    info.DeclaredVolumeBytes = info.UdfPartitionEndBytes;
                }
            }

            if (info.DeclaredVolumeBytes > info.FileLength)
            {
                AddProblem(info, "The image is truncated: its header declares " + FileSizeFormatter.FormatWithBytes(info.DeclaredVolumeBytes) +
                                 " but the file is only " + FileSizeFormatter.FormatWithBytes(info.FileLength) + ".");
            }

            if (primaryRoot != null)
            {
                WalkTree(stream, primaryRoot, false, info, info.PrimaryEntries, cancellationToken);
                if (info.HasJoliet)
                {
                    WalkTree(stream, jolietRoot, true, info, info.JolietEntries, cancellationToken);
                    CompareTrees(info);
                }
            }

            // Windows shows UDF when present, otherwise Joliet, otherwise ISO 9660.
            info.Entries.AddRange(info.HasUdf ? info.UdfEntries : info.HasJoliet ? info.JolietEntries : info.PrimaryEntries);
            if (info.HasUdf && primaryRoot != null)
            {
                CompareUdfWithIso(info);
            }

            return info;
        }

        /// <summary>Opens a file's data: one extent (ISO 9660 / Joliet), UDF ranges, or UDF embedded data.</summary>
        public static Stream OpenExtent(Stream image, IsoImageEntry entry)
        {
            if (entry.InlineData != null)
            {
                return new MemoryStream(entry.InlineData, false);
            }

            if (entry.DataRanges != null)
            {
                return new RangesStream(image, entry.DataRanges);
            }

            return new ExtentStream(image, entry.ExtentSector * SectorSize, entry.Length);
        }

        /// <summary>Reads several byte ranges of the image as one stream; offset -1 reads as zeros.</summary>
        private sealed class RangesStream : Stream
        {
            private readonly Stream _image;
            private readonly List<long[]> _ranges;
            private int _index;
            private long _offsetInRange;
            private long _position;

            public RangesStream(Stream image, List<long[]> ranges)
            {
                _image = image;
                _ranges = ranges;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => _ranges.Sum(r => r[1]);

            public override long Position
            {
                get { return _position; }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                while (_index < _ranges.Count && _offsetInRange >= _ranges[_index][1])
                {
                    _index++;
                    _offsetInRange = 0;
                }

                if (_index >= _ranges.Count || count == 0)
                {
                    return 0;
                }

                long[] range = _ranges[_index];
                int take = (int)Math.Min(count, range[1] - _offsetInRange);
                int read;
                if (range[0] < 0)
                {
                    Array.Clear(buffer, offset, take);
                    read = take;
                }
                else
                {
                    _image.Position = range[0] + _offsetInRange;
                    read = _image.Read(buffer, offset, take);
                }

                _offsetInRange += read;
                _position += read;
                return read;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private static byte[] ReadPrimaryDescriptor(byte[] d, IsoImageInfo info)
        {
            int blockSize = BitConverter.ToUInt16(d, 128);
            if (blockSize != SectorSize)
            {
                throw new InvalidIsoImageException("The image uses a logical block size of " + blockSize + " bytes. Only 2048-byte blocks are supported.");
            }

            uint spaceLe = BitConverter.ToUInt32(d, 80);
            uint spaceBe = (uint)(d[84] << 24 | d[85] << 16 | d[86] << 8 | d[87]);
            if (spaceLe != spaceBe)
            {
                AddProblem(info, "The volume size is recorded inconsistently (little-endian " + spaceLe + ", big-endian " + spaceBe + ").");
            }

            info.DeclaredVolumeBytes = (long)spaceLe * SectorSize;
            info.SystemIdentifier = TrimField(Encoding.ASCII.GetString(d, 8, 32));
            info.VolumeLabel = TrimField(Encoding.ASCII.GetString(d, 40, 32));
            info.PublisherIdentifier = TrimField(Encoding.ASCII.GetString(d, 318, 128));
            info.PreparerIdentifier = TrimField(Encoding.ASCII.GetString(d, 446, 128));
            info.ApplicationIdentifier = TrimField(Encoding.ASCII.GetString(d, 574, 128));
            info.VolumeCreationTime = ParseDescriptorDate(d, 813);
            info.VolumeModificationTime = ParseDescriptorDate(d, 830);
            return Slice(d, 156, 34);
        }

        private static void WalkTree(Stream stream, byte[] rootRecord, bool joliet, IsoImageInfo info, List<IsoImageEntry> entries, CancellationToken cancellationToken)
        {
            string treeName = joliet ? "Joliet" : "ISO 9660";
            var visited = new HashSet<long>();
            var pending = new Queue<Tuple<long, long, string, int>>();
            pending.Enqueue(Tuple.Create((long)BitConverter.ToUInt32(rootRecord, 2), (long)BitConverter.ToUInt32(rootRecord, 10), string.Empty, 0));

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Tuple<long, long, string, int> directory = pending.Dequeue();
                long sector = directory.Item1;
                long length = directory.Item2;
                string dirPath = directory.Item3;
                string display = dirPath.Length == 0 ? "the root folder" : "'" + dirPath + "'";

                if (!visited.Add(sector))
                {
                    AddProblem(info, treeName + " tree: " + display + " refers to a directory that was already read (directory loop).");
                    continue;
                }

                if (length <= 0 || length > MaxDirectoryBytes || sector * SectorSize + length > info.FileLength)
                {
                    AddProblem(info, treeName + " tree: the directory data for " + display + " lies outside the image or is damaged.");
                    continue;
                }

                byte[] data = ReadBytes(stream, sector * SectorSize, (int)length);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int position = 0;
                while (position < data.Length)
                {
                    int recordLength = data[position];
                    if (recordLength == 0)
                    {
                        // Records never cross sector boundaries; zero padding fills the rest of the sector.
                        position = (position / SectorSize + 1) * SectorSize;
                        continue;
                    }

                    int idLength = position + 32 < data.Length ? data[position + 32] : 0;
                    if (recordLength < 34 || position + recordLength > data.Length || 33 + idLength > recordLength)
                    {
                        AddProblem(info, treeName + " tree: a damaged directory record was found in " + display + ".");
                        break;
                    }

                    if (idLength == 1 && (data[position + 33] == 0 || data[position + 33] == 1))
                    {
                        position += recordLength;
                        continue;
                    }

                    byte flags = data[position + 25];
                    long extent = BitConverter.ToUInt32(data, position + 2);
                    long size = BitConverter.ToUInt32(data, position + 10);
                    bool isDirectory = (flags & 0x02) != 0;
                    string name = DecodeName(data, position + 33, idLength, joliet, isDirectory);
                    string entryPath = dirPath.Length == 0 ? name : dirPath + "\\" + name;
                    DateTime? recorded = ParseRecordDate(data, position + 18);
                    position += recordLength;

                    if ((flags & 0x80) != 0)
                    {
                        AddProblem(info, treeName + " tree: '" + entryPath + "' is stored in several extents, which this reader does not support.");
                        continue;
                    }

                    if (!names.Add(name))
                    {
                        AddProblem(info, treeName + " tree: '" + entryPath + "' " + DuplicateEntryText + ".");
                    }

                    if (!isDirectory && extent * SectorSize + size > info.FileLength)
                    {
                        AddProblem(info, treeName + " tree: the data of '" + entryPath + "' lies outside the image (the file is truncated or damaged).");
                    }

                    entries.Add(new IsoImageEntry(entryPath, isDirectory, extent, isDirectory ? 0 : size, recorded));
                    if (isDirectory)
                    {
                        if (directory.Item4 + 1 > MaxDepth)
                        {
                            AddProblem(info, treeName + " tree: '" + entryPath + "' is nested more than " + MaxDepth + " levels deep.");
                        }
                        else
                        {
                            pending.Enqueue(Tuple.Create(extent, size, entryPath, directory.Item4 + 1));
                        }
                    }
                }
            }
        }

        /// <summary>Both trees of a Joliet image must point at the same file data.</summary>
        private static void CompareTrees(IsoImageInfo info)
        {
            List<string> primary = info.PrimaryEntries.Where(e => !e.IsDirectory).Select(e => e.ExtentSector + ":" + e.Length).OrderBy(s => s, StringComparer.Ordinal).ToList();
            List<string> joliet = info.JolietEntries.Where(e => !e.IsDirectory).Select(e => e.ExtentSector + ":" + e.Length).OrderBy(s => s, StringComparer.Ordinal).ToList();
            int primaryDirs = info.PrimaryEntries.Count(e => e.IsDirectory);
            int jolietDirs = info.JolietEntries.Count(e => e.IsDirectory);
            if (!primary.SequenceEqual(joliet) || primaryDirs != jolietDirs)
            {
                AddProblem(info, "The ISO 9660 and Joliet directory trees describe different contents (" +
                                 primary.Count + " vs " + joliet.Count + " files, " + primaryDirs + " vs " + jolietDirs + " folders).");
            }
        }

        /// <summary>
        /// In a UDF bridge image the UDF and ISO 9660 views must list the same files: the same number of
        /// files and folders, and the same file data (start sector and size).
        /// </summary>
        private static void CompareUdfWithIso(IsoImageInfo info)
        {
            Func<IsoImageEntry, string> key = e => (e.Length == 0 ? "empty" : e.ExtentSector.ToString(CultureInfo.InvariantCulture)) + ":" + e.Length;
            List<string> udf = info.UdfEntries.Where(e => !e.IsDirectory).Select(key).OrderBy(s => s, StringComparer.Ordinal).ToList();
            List<string> iso = info.PrimaryEntries.Where(e => !e.IsDirectory).Select(key).OrderBy(s => s, StringComparer.Ordinal).ToList();
            int udfDirs = info.UdfEntries.Count(e => e.IsDirectory);
            int isoDirs = info.PrimaryEntries.Count(e => e.IsDirectory);
            if (!udf.SequenceEqual(iso) || udfDirs != isoDirs)
            {
                AddProblem(info, "The UDF and ISO 9660 views describe different contents (" +
                                 udf.Count + " vs " + iso.Count + " files, " + udfDirs + " vs " + isoDirs + " folders).");
            }
        }

        private static string DecodeName(byte[] data, int offset, int length, bool joliet, bool isDirectory)
        {
            string name = joliet ? Encoding.BigEndianUnicode.GetString(data, offset, length - length % 2) : Encoding.ASCII.GetString(data, offset, length);
            int semicolon = name.LastIndexOf(';');
            if (semicolon >= 0)
            {
                name = name.Substring(0, semicolon);
            }

            // ISO 9660 writes "NAME." for files without an extension; the dot is not part of the name.
            return !isDirectory && name.EndsWith(".", StringComparison.Ordinal) ? name.TrimEnd('.') : name;
        }

        /// <summary>7-byte directory record date: years since 1900, month, day, hour, minute, second, offset in 15-minute units.</summary>
        private static DateTime? ParseRecordDate(byte[] d, int offset)
        {
            int month = d[offset + 1];
            int day = d[offset + 2];
            if (month < 1 || month > 12 || day < 1 || day > 31 || d[offset + 3] > 23 || d[offset + 4] > 59 || d[offset + 5] > 59)
            {
                return null;
            }

            try
            {
                var local = new DateTime(1900 + d[offset], month, day, d[offset + 3], d[offset + 4], d[offset + 5], DateTimeKind.Unspecified);
                return DateTime.SpecifyKind(local.AddMinutes(-15 * (sbyte)d[offset + 6]), DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        /// <summary>17-byte descriptor date "YYYYMMDDHHMMSScc" plus offset; all zeros means "not specified".</summary>
        private static DateTime? ParseDescriptorDate(byte[] d, int offset)
        {
            string text = Encoding.ASCII.GetString(d, offset, 14);
            DateTime parsed;
            if (text.Trim('0', ' ', '\0').Length == 0 ||
                !DateTime.TryParseExact(text, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return null;
            }

            return DateTime.SpecifyKind(parsed.AddMinutes(-15 * (sbyte)d[offset + 16]), DateTimeKind.Utc);
        }

        private static string TrimField(string value)
        {
            string trimmed = value.Trim(' ', '\0');
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static void AddProblem(IsoImageInfo info, string problem)
        {
            if (info.Problems.Count < MaxProblems)
            {
                info.Problems.Add(problem);
            }
        }

        private static byte[] Slice(byte[] source, int offset, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private static byte[] ReadBytes(Stream stream, long position, int length)
        {
            var buffer = new byte[length];
            stream.Position = position;
            int total = 0;
            while (total < length)
            {
                int read = stream.Read(buffer, total, length - total);
                if (read == 0)
                {
                    throw new InvalidIsoImageException("The image ended unexpectedly while reading its structure (the file is truncated).");
                }

                total += read;
            }

            return buffer;
        }

        /// <summary>Read-only view of one data extent of the image.</summary>
        private sealed class ExtentStream : Stream
        {
            private readonly Stream _image;
            private readonly long _start;
            private readonly long _length;
            private long _position;

            public ExtentStream(Stream image, long start, long length)
            {
                _image = image;
                _start = start;
                _length = length;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => _length;

            public override long Position
            {
                get { return _position; }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long remaining = _length - _position;
                if (remaining <= 0)
                {
                    return 0;
                }

                _image.Position = _start + _position;
                int read = _image.Read(buffer, offset, (int)Math.Min(count, remaining));
                _position += read;
                return read;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
