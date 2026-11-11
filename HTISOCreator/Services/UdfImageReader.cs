using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// A small, self-contained UDF reader written from ECMA-167 and the OSTA UDF specification
    /// (revisions 1.02 to 2.01, physical partitions). Like <see cref="IsoImageReader"/>, it does not
    /// share code with the writer (Windows IMAPI2), so it can verify images independently.
    ///
    /// It reads the anchor, the volume descriptors, the file set descriptor and every file entry
    /// and file identifier, checks descriptor tags (identifier, checksum, location), and records each
    /// file's data as byte ranges in the image. Metadata, virtual and sparable partitions (UDF 2.50+,
    /// packet-written discs) are reported as unsupported rather than misread.
    /// </summary>
    internal static class UdfImageReader
    {
        private const int SectorSize = IsoImageReader.SectorSize;
        private const int AnchorSector = 256;
        private const int MaxDepth = 64;
        private const int MaxDirectoryBytes = 64 * 1024 * 1024;
        private const int MaxVolumeDescriptors = 512;

        private const ushort TagPrimaryVolume = 1;
        private const ushort TagAnchor = 2;
        private const ushort TagPartition = 5;
        private const ushort TagLogicalVolume = 6;
        private const ushort TagTerminating = 8;
        private const ushort TagFileSet = 256;
        private const ushort TagFileIdentifier = 257;
        private const ushort TagFileEntry = 261;
        private const ushort TagExtendedFileEntry = 266;

        /// <summary>True when the image has a valid UDF anchor volume descriptor pointer at sector 256.</summary>
        public static bool HasAnchor(Stream stream)
        {
            if (stream.Length < (AnchorSector + 1) * (long)SectorSize)
            {
                return false;
            }

            byte[] anchor = Read(stream, AnchorSector * (long)SectorSize, SectorSize);
            return TagIsValid(anchor, 0, TagAnchor, AnchorSector) == null;
        }

        /// <summary>Reads the UDF file system into <paramref name="info"/> (UdfEntries, label, revision, problems).</summary>
        public static void ReadInto(Stream stream, IsoImageInfo info, Action<string> addProblem, CancellationToken cancellationToken)
        {
            byte[] anchor = Read(stream, AnchorSector * (long)SectorSize, SectorSize);
            long vdsLength = BitConverter.ToUInt32(anchor, 16);
            long vdsSector = BitConverter.ToUInt32(anchor, 20);
            long reserveLength = BitConverter.ToUInt32(anchor, 24);
            long reserveSector = BitConverter.ToUInt32(anchor, 28);
            if (reserveLength > 0 && reserveSector * SectorSize + reserveLength > stream.Length)
            {
                addProblem("UDF: the reserve volume descriptor sequence lies outside the image (the file is truncated).");
            }

            // ECMA-167 3/8.4.2: at least two of the anchor points 256, N-256 and N must hold an anchor.
            long lastSector = stream.Length / SectorSize - 1;
            bool secondAnchor = false;
            foreach (long sector in new[] { lastSector, lastSector - 256 })
            {
                if (sector > AnchorSector &&
                    TagIsValid(Read(stream, sector * SectorSize, SectorSize), 0, TagAnchor, sector) == null)
                {
                    secondAnchor = true;
                }
            }

            if (!secondAnchor)
            {
                addProblem("UDF: only one anchor volume descriptor pointer was found; UDF requires a second one at the end of the volume (the file may be truncated).");
            }
            int vdsCount = (int)Math.Min(MaxVolumeDescriptors, vdsLength / SectorSize);
            if (vdsCount == 0 || (vdsSector + vdsCount) * SectorSize > stream.Length)
            {
                addProblem("UDF: the main volume descriptor sequence lies outside the image.");
                return;
            }

            long partitionStart = -1;
            long partitionLength = 0;
            int partitionNumber = -1;
            int blockSize = 0;
            byte[] fileSetLongAd = null;
            int mapPartitionNumber = -1;

            for (int i = 0; i < vdsCount; i++)
            {
                long sector = vdsSector + i;
                byte[] d = Read(stream, sector * SectorSize, SectorSize);
                ushort tag = BitConverter.ToUInt16(d, 0);
                string tagError = TagIsValid(d, 0, tag, sector);
                if (tagError != null)
                {
                    addProblem("UDF: volume descriptor at sector " + sector + ": " + tagError);
                    break;
                }

                if (tag == TagTerminating)
                {
                    break;
                }

                switch (tag)
                {
                    case TagPrimaryVolume:
                        info.UdfVolumeLabel = DString(d, 24, 32);
                        break;
                    case TagPartition:
                        partitionNumber = BitConverter.ToUInt16(d, 22);
                        partitionStart = BitConverter.ToUInt32(d, 188);
                        partitionLength = BitConverter.ToUInt32(d, 192);
                        break;
                    case TagLogicalVolume:
                        blockSize = (int)BitConverter.ToUInt32(d, 212);
                        string domain = Encoding.ASCII.GetString(d, 217, 23).TrimEnd('\0', ' ');
                        if (domain == "*OSTA UDF Compliant")
                        {
                            ushort revision = BitConverter.ToUInt16(d, 216 + 24);
                            info.UdfRevision = (revision >> 8).ToString("X") + "." + (revision & 0xFF).ToString("X2");
                        }

                        fileSetLongAd = new byte[16];
                        Buffer.BlockCopy(d, 248, fileSetLongAd, 0, 16);
                        int mapCount = (int)BitConverter.ToUInt32(d, 268);
                        int mapOffset = 440;
                        for (int m = 0; m < mapCount && mapOffset + 2 <= d.Length; m++)
                        {
                            int type = d[mapOffset];
                            int length = d[mapOffset + 1];
                            if (type == 1 && length == 6 && m == 0)
                            {
                                mapPartitionNumber = BitConverter.ToUInt16(d, mapOffset + 4);
                            }
                            else
                            {
                                addProblem("UDF: the image uses a type " + type + " partition map (virtual, sparable or metadata partition), which this reader does not support.");
                                return;
                            }

                            mapOffset += Math.Max(2, length);
                        }

                        break;
                }
            }

            if (blockSize != SectorSize)
            {
                addProblem(blockSize == 0
                    ? "UDF: no logical volume descriptor was found."
                    : "UDF: the logical block size is " + blockSize + " bytes; only 2048-byte blocks are supported.");
                return;
            }

            if (partitionStart < 0 || fileSetLongAd == null || mapPartitionNumber != partitionNumber)
            {
                addProblem("UDF: the partition descriptor or partition map is missing or does not match.");
                return;
            }

            info.HasUdf = true;
            info.UdfPartitionEndBytes = (partitionStart + partitionLength) * SectorSize;
            var context = new Context(stream, partitionStart, partitionLength, addProblem);

            byte[] fileSet = context.ReadBlock(BitConverter.ToUInt32(fileSetLongAd, 4), TagFileSet, "file set descriptor");
            if (fileSet == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(info.UdfVolumeLabel))
            {
                info.UdfVolumeLabel = DString(fileSet, 112, 128);
            }

            long rootBlock = BitConverter.ToUInt32(fileSet, 400 + 4);
            context.Walk(rootBlock, info.UdfEntries, cancellationToken);
        }

        private sealed class Context
        {
            private readonly Stream _stream;
            private readonly long _partitionStart;
            private readonly long _partitionLength;
            private readonly Action<string> _addProblem;

            public Context(Stream stream, long partitionStart, long partitionLength, Action<string> addProblem)
            {
                _stream = stream;
                _partitionStart = partitionStart;
                _partitionLength = partitionLength;
                _addProblem = addProblem;
            }

            public byte[] ReadBlock(long block, ushort expectedTag, string what)
            {
                if (block >= _partitionLength || (_partitionStart + block + 1) * SectorSize > _stream.Length)
                {
                    _addProblem("UDF: the " + what + " at block " + block + " lies outside the image.");
                    return null;
                }

                byte[] d = Read(_stream, (_partitionStart + block) * SectorSize, SectorSize);
                string error = TagIsValid(d, 0, expectedTag, block);
                if (error != null)
                {
                    _addProblem("UDF: " + what + " at block " + block + ": " + error);
                    return null;
                }

                return d;
            }

            public void Walk(long rootBlock, List<IsoImageEntry> entries, CancellationToken cancellationToken)
            {
                var visited = new HashSet<long>();
                var pending = new Queue<Tuple<long, string, int>>();
                pending.Enqueue(Tuple.Create(rootBlock, string.Empty, 0));
                while (pending.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Tuple<long, string, int> item = pending.Dequeue();
                    string display = item.Item2.Length == 0 ? "the root folder" : "'" + item.Item2 + "'";
                    if (!visited.Add(item.Item1))
                    {
                        _addProblem("UDF: " + display + " refers to a directory that was already read (directory loop).");
                        continue;
                    }

                    FileData directory = ReadFileEntry(item.Item1, display);
                    if (directory == null)
                    {
                        continue;
                    }

                    if (!directory.IsDirectory)
                    {
                        _addProblem("UDF: " + display + " is not a directory.");
                        continue;
                    }

                    if (directory.Length > MaxDirectoryBytes)
                    {
                        _addProblem("UDF: the directory data for " + display + " is too large or damaged.");
                        continue;
                    }

                    byte[] data = ReadAll(directory);
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int position = 0;
                    while (position + 38 <= data.Length)
                    {
                        string error = TagIsValid(data, position, TagFileIdentifier, -1);
                        if (error != null)
                        {
                            _addProblem("UDF: a damaged file identifier was found in " + display + " (" + error + ").");
                            break;
                        }

                        byte characteristics = data[position + 18];
                        int nameLength = data[position + 19];
                        long childBlock = BitConverter.ToUInt32(data, position + 20 + 4);
                        int implementationUse = BitConverter.ToUInt16(data, position + 36);
                        int nameOffset = position + 38 + implementationUse;
                        int recordLength = (38 + implementationUse + nameLength + 3) & ~3;
                        if (position + recordLength > data.Length + 3 || nameOffset + nameLength > data.Length)
                        {
                            _addProblem("UDF: a file identifier in " + display + " runs past the end of the directory.");
                            break;
                        }

                        position += recordLength;
                        bool deleted = (characteristics & 0x04) != 0;
                        bool parent = (characteristics & 0x08) != 0;
                        if (deleted || parent)
                        {
                            continue;
                        }

                        string name = Cs0(data, nameOffset, nameLength);
                        string path = item.Item2.Length == 0 ? name : item.Item2 + "\\" + name;
                        if (name.Length == 0 || name.IndexOfAny(new[] { '\\', '/', '\0' }) >= 0)
                        {
                            _addProblem("UDF: " + display + " contains an empty or invalid name.");
                            continue;
                        }

                        if (!names.Add(name))
                        {
                            _addProblem("UDF tree: '" + path + "' " + IsoImageReader.DuplicateEntryText + ".");
                        }

                        FileData child = ReadFileEntry(childBlock, "'" + path + "'");
                        if (child == null)
                        {
                            continue;
                        }

                        bool isDirectory = (characteristics & 0x02) != 0;
                        if (isDirectory != child.IsDirectory)
                        {
                            _addProblem("UDF: '" + path + "' is marked as a " + (isDirectory ? "folder" : "file") + " but its file entry says otherwise.");
                        }

                        entries.Add(new IsoImageEntry(path, child.IsDirectory,
                            child.Ranges.Count > 0 ? child.Ranges[0][0] / SectorSize : 0,
                            child.IsDirectory ? 0 : child.Length, child.Modified)
                        {
                            DataRanges = child.Ranges,
                            InlineData = child.Inline
                        });

                        if (child.IsDirectory)
                        {
                            if (item.Item3 + 1 > MaxDepth)
                            {
                                _addProblem("UDF: '" + path + "' is nested more than " + MaxDepth + " levels deep.");
                            }
                            else
                            {
                                pending.Enqueue(Tuple.Create(childBlock, path, item.Item3 + 1));
                            }
                        }
                    }
                }
            }

            private FileData ReadFileEntry(long block, string display)
            {
                if (block >= _partitionLength || (_partitionStart + block + 1) * SectorSize > _stream.Length)
                {
                    _addProblem("UDF: the file entry for " + display + " lies outside the image.");
                    return null;
                }

                byte[] d = Read(_stream, (_partitionStart + block) * SectorSize, SectorSize);
                ushort tag = BitConverter.ToUInt16(d, 0);
                if (tag != TagFileEntry && tag != TagExtendedFileEntry)
                {
                    _addProblem("UDF: the file entry for " + display + " has descriptor tag " + tag + " instead of a file entry.");
                    return null;
                }

                string error = TagIsValid(d, 0, tag, block);
                if (error != null)
                {
                    _addProblem("UDF: the file entry for " + display + ": " + error);
                    return null;
                }

                bool extended = tag == TagExtendedFileEntry;
                int fileType = d[16 + 11];
                int allocationType = BitConverter.ToUInt16(d, 16 + 18) & 0x7;
                long length = (long)BitConverter.ToUInt64(d, 56);
                DateTime? modified = Timestamp(d, extended ? 92 : 84);
                int extendedAttributes = (int)BitConverter.ToUInt32(d, extended ? 208 : 168);
                int descriptorsLength = (int)BitConverter.ToUInt32(d, extended ? 212 : 172);
                int descriptorsOffset = (extended ? 216 : 176) + extendedAttributes;
                if (descriptorsOffset + descriptorsLength > d.Length || extendedAttributes < 0 || descriptorsLength < 0)
                {
                    _addProblem("UDF: the file entry for " + display + " is damaged (allocation descriptors run past the block).");
                    return null;
                }

                var result = new FileData { IsDirectory = fileType == 4, Length = length, Modified = modified };
                if (fileType != 4 && fileType != 5)
                {
                    _addProblem("UDF: " + display + " has file type " + fileType + ", which is not a plain file or folder.");
                }

                if (allocationType == 3)
                {
                    if (length > descriptorsLength)
                    {
                        _addProblem("UDF: the embedded data of " + display + " is shorter than its recorded size.");
                        return null;
                    }

                    result.Inline = new byte[length];
                    Buffer.BlockCopy(d, descriptorsOffset, result.Inline, 0, (int)length);
                    return result;
                }

                if (allocationType != 0 && allocationType != 1)
                {
                    _addProblem("UDF: " + display + " uses extended allocation descriptors, which this reader does not support.");
                    return null;
                }

                int adSize = allocationType == 0 ? 8 : 16;
                long remaining = length;
                for (int offset = descriptorsOffset; offset + adSize <= descriptorsOffset + descriptorsLength && remaining > 0; offset += adSize)
                {
                    uint raw = BitConverter.ToUInt32(d, offset);
                    long extentLength = raw & 0x3FFFFFFF;
                    int extentType = (int)(raw >> 30);
                    long extentBlock = BitConverter.ToUInt32(d, offset + 4);
                    if (extentLength == 0)
                    {
                        break;
                    }

                    if (extentType == 3)
                    {
                        _addProblem("UDF: " + display + " continues its allocation descriptors in another block, which this reader does not support.");
                        return null;
                    }

                    long take = Math.Min(extentLength, remaining);
                    if (extentType == 0)
                    {
                        long start = (_partitionStart + extentBlock) * SectorSize;
                        if (extentBlock + (take + SectorSize - 1) / SectorSize > _partitionLength || start + take > _stream.Length)
                        {
                            _addProblem("UDF: the data of " + display + " lies outside the image (the file is truncated or damaged).");
                            return null;
                        }

                        result.Ranges.Add(new[] { start, take });
                    }
                    else
                    {
                        // Allocated or unallocated but not recorded: reads as zeros.
                        result.Ranges.Add(new[] { -1L, take });
                    }

                    remaining -= take;
                }

                if (remaining > 0)
                {
                    _addProblem("UDF: the recorded data of " + display + " is " + remaining + " bytes shorter than its size.");
                    return null;
                }

                return result;
            }

            private byte[] ReadAll(FileData file)
            {
                if (file.Inline != null)
                {
                    return file.Inline;
                }

                var buffer = new byte[file.Length];
                int written = 0;
                foreach (long[] range in file.Ranges)
                {
                    if (range[0] >= 0)
                    {
                        byte[] part = Read(_stream, range[0], (int)range[1]);
                        Buffer.BlockCopy(part, 0, buffer, written, part.Length);
                    }

                    written += (int)range[1];
                }

                return buffer;
            }
        }

        private sealed class FileData
        {
            public bool IsDirectory;
            public long Length;
            public DateTime? Modified;
            public byte[] Inline;
            public readonly List<long[]> Ranges = new List<long[]>();
        }

        /// <summary>Checks a 16-byte descriptor tag: identifier, checksum and (when known) recorded location.</summary>
        private static string TagIsValid(byte[] d, int offset, ushort expectedTag, long expectedLocation)
        {
            ushort tag = BitConverter.ToUInt16(d, offset);
            if (tag != expectedTag)
            {
                return "expected descriptor tag " + expectedTag + " but found " + tag;
            }

            int sum = 0;
            for (int i = 0; i < 16; i++)
            {
                if (i != 4)
                {
                    sum += d[offset + i];
                }
            }

            if ((byte)sum != d[offset + 4])
            {
                return "the descriptor tag checksum is wrong";
            }

            if (expectedLocation >= 0 && BitConverter.ToUInt32(d, offset + 12) != expectedLocation)
            {
                return "the descriptor records location " + BitConverter.ToUInt32(d, offset + 12) + " instead of " + expectedLocation;
            }

            return null;
        }

        /// <summary>OSTA compressed Unicode: compression ID 8 (one byte per character) or 16 (UTF-16 big-endian).</summary>
        internal static string Cs0(byte[] d, int offset, int length)
        {
            if (length <= 1)
            {
                return string.Empty;
            }

            switch (d[offset])
            {
                case 8:
                    var sb = new StringBuilder(length - 1);
                    for (int i = 1; i < length; i++)
                    {
                        sb.Append((char)d[offset + i]);
                    }

                    return sb.ToString();
                case 16:
                    return Encoding.BigEndianUnicode.GetString(d, offset + 1, (length - 1) & ~1);
                default:
                    return string.Empty;
            }
        }

        /// <summary>A dstring: CS0 characters, with the used length in the last byte.</summary>
        private static string DString(byte[] d, int offset, int size)
        {
            int used = d[offset + size - 1];
            return used == 0 || used >= size ? null : Cs0(d, offset, used).TrimEnd('\0', ' ');
        }

        /// <summary>ECMA-167 timestamp, converted to UTC when it carries a time zone.</summary>
        private static DateTime? Timestamp(byte[] d, int offset)
        {
            ushort typeAndZone = BitConverter.ToUInt16(d, offset);
            int year = BitConverter.ToInt16(d, offset + 2);
            int month = d[offset + 4];
            int day = d[offset + 5];
            int hour = d[offset + 6];
            int minute = d[offset + 7];
            int second = d[offset + 8];
            if (year < 1 || month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59 || second > 59)
            {
                return null;
            }

            try
            {
                var value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                int zone = typeAndZone & 0x0FFF;
                if ((zone & 0x0800) != 0)
                {
                    zone -= 0x1000;
                }

                if ((typeAndZone >> 12) == 1 && zone != -2047)
                {
                    value = value.AddMinutes(-zone);
                }

                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static byte[] Read(Stream stream, long position, int length)
        {
            var buffer = new byte[length];
            stream.Position = position;
            int total = 0;
            while (total < length)
            {
                int read = stream.Read(buffer, total, length - total);
                if (read == 0)
                {
                    throw new InvalidIsoImageException("The image ended unexpectedly while reading its UDF structure (the file is truncated).");
                }

                total += read;
            }

            return buffer;
        }
    }
}
