using System;
using System.IO;
using System.Linq;
using DiscUtils.Iso9660;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Calculates the exact size of the image that would be created, including volume
    /// descriptors, path tables, directory records and sector alignment. It runs the same
    /// DiscUtils layout as a real build, with placeholder streams that report each file's
    /// length but are never read, so no file data is touched.
    /// </summary>
    public static class IsoSizeCalculator
    {
        /// <summary>
        /// Returns the image size in bytes, or null when no image can be built
        /// (for example a file is 4 GB or larger).
        /// </summary>
        public static long? Calculate(FolderAnalysisResult analysis, IsoFileSystemFormat format)
        {
            if (analysis.Files.Any(f => f.Length > FileSystemFormats.MaxFileSize(format)))
            {
                return null;
            }

            IsoNameMapper.MappingResult mapping = IsoNameMapper.Map(analysis.Directories, analysis.Files.Select(f => f.RelativePath), format);
            if (FileSystemFormats.UsesImapi(format))
            {
                return CalculateWithImapi(analysis, mapping, format);
            }

            var builder = new CDBuilder
            {
                UseJoliet = format == IsoFileSystemFormat.Iso9660Joliet,
                VolumeIdentifier = "SIZE"
            };

            // Same names as a real build (see IsoShortNames), so the size matches exactly.
            var shortNameItems = new System.Collections.Generic.List<IsoShortNames.Item>();
            foreach (string directory in analysis.Directories.Select(d => mapping.Directories[d]).OrderBy(d => d.Count(c => c == '\\')))
            {
                shortNameItems.Add(new IsoShortNames.Item(directory, builder.AddDirectory(directory), true));
            }

            foreach (SourceFileEntry file in analysis.Files)
            {
                string imagePath = mapping.Files[file.RelativePath];
                shortNameItems.Add(new IsoShortNames.Item(imagePath, builder.AddFile(imagePath, new PlaceholderStream(file.Length)), false));
            }

            IsoShortNames.Apply(shortNameItems);

            using (Stream image = builder.Build())
            {
                return image.Length;
            }
        }

        /// <summary>
        /// IMAPI2 reports the image size once the layout exists (TotalBlocks x BlockSize), before any data
        /// is read. The placeholder data streams read as zeros in case IMAPI2 reads a small file early.
        /// Returns null when IMAPI2 is not available.
        /// </summary>
        private static long? CalculateWithImapi(FolderAnalysisResult analysis, IsoNameMapper.MappingResult mapping, IsoFileSystemFormat format)
        {
            if (ImapiImageBuilder.CheckAvailable() != null)
            {
                return null;
            }

            System.Collections.Generic.List<string> directories = analysis.Directories.Select(d => mapping.Directories[d])
                .OrderBy(d => d.Count(c => c == '\\')).ThenBy(d => d, StringComparer.Ordinal).ToList();
            var files = analysis.Files.Select(f => new ImapiImageBuilder.FileItem(mapping.Files[f.RelativePath], f.Length,
                () => new ZeroStream(f.Length), null));
            using (ImapiImage image = ImapiImageBuilder.Build(format, "SIZE", directories, null, files))
            {
                return image.Length;
            }
        }

        /// <summary>Reads as zeros; used only if IMAPI2 reads data while laying out the image.</summary>
        private sealed class ZeroStream : Stream
        {
            private readonly long _length;
            private long _position;

            public ZeroStream(long length)
            {
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
                int take = (int)Math.Min(count, _length - _position);
                Array.Clear(buffer, offset, take);
                _position += take;
                return take;
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

        /// <summary>A stream with a length and no readable data.</summary>
        private sealed class PlaceholderStream : Stream
        {
            private readonly long _length;

            public PlaceholderStream(long length)
            {
                _length = length;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => _length;

            public override long Position { get; set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("Size calculation must not read file data.");
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                Position = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? Position + offset : _length + offset;
                return Position;
            }

            public override void Flush()
            {
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
