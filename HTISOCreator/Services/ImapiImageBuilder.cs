using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using HTISOCreator.Models;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Builds UDF and ISO 9660 + Joliet + UDF images with the IMAPI2 file system image component
    /// (IMAPI2FS.MsftFileSystemImage) that is part of Windows. No disc drive is involved: the
    /// component returns the finished image as a stream, which <see cref="IsoCreationService"/>
    /// copies to disk exactly like a DiscUtils image (progress, cancellation, safe replacement).
    ///
    /// The component is used through late-bound COM calls (documented IMAPI2 members only), so no
    /// interop assembly is needed. Every file is added with its final image path from the plan, so
    /// IMAPI2 never chooses a name itself for the UDF view. File data is read only while the image
    /// stream is read.
    /// </summary>
    internal static class ImapiImageBuilder
    {
        public const string ProgId = "IMAPI2FS.MsftFileSystemImage";

        private const int MediaTypeDisk = 12;        // IMAPI_MEDIA_TYPE_DISK: no capacity limit
        private const int FileSystemIso9660 = 1;
        private const int FileSystemJoliet = 2;
        private const int FileSystemUdf = 4;
        private const int UdfRevision102 = 0x102;

        /// <summary>Returns null when IMAPI2 can be used, otherwise a message for the user.</summary>
        public static string CheckAvailable()
        {
            try
            {
                return Type.GetTypeFromProgID(ProgId, false) == null
                    ? "The UDF formats use the Windows IMAPI2 component, which is not available on this computer."
                    : null;
            }
            catch (Exception ex) when (ex is COMException || ex is System.Security.SecurityException)
            {
                return "The Windows IMAPI2 component could not be used: " + ex.Message;
            }
        }

        /// <summary>A file to add: its image path, size, a factory for its data, and its date.</summary>
        public sealed class FileItem
        {
            public FileItem(string imagePath, long length, Func<Stream> open, DateTime? modifiedUtc)
            {
                ImagePath = imagePath;
                Length = length;
                Open = open;
                ModifiedUtc = modifiedUtc;
            }

            public string ImagePath { get; }

            public long Length { get; }

            public Func<Stream> Open { get; }

            public DateTime? ModifiedUtc { get; }
        }

        /// <summary>
        /// Lays out the image. The returned image's stream generates the image as it is read; dispose
        /// it to release the COM objects. Source errors raised while reading are rethrown as they were.
        /// </summary>
        public static ImapiImage Build(IsoFileSystemFormat format, string volumeLabel, IEnumerable<string> directories,
            IDictionary<string, DateTime> directoryTimesUtc, IEnumerable<FileItem> files)
        {
            string unavailable = CheckAvailable();
            if (unavailable != null)
            {
                throw new IsoCreationException(unavailable);
            }

            var errors = new SourceErrorBox();
            dynamic image = null;
            try
            {
                image = Activator.CreateInstance(Type.GetTypeFromProgID(ProgId, true));
                image.ChooseImageDefaultsForMediaType(MediaTypeDisk);
                image.FileSystemsToCreate = format == IsoFileSystemFormat.Udf
                    ? FileSystemUdf
                    : FileSystemIso9660 | FileSystemJoliet | FileSystemUdf;
                image.UDFRevision = UdfRevision102;
                image.VolumeName = volumeLabel;
                dynamic root = image.Root;

                var datedItems = new List<KeyValuePair<string, DateTime>>();
                foreach (string directory in directories)
                {
                    root.AddDirectory(directory);
                    DateTime modified;
                    if (directoryTimesUtc != null && directoryTimesUtc.TryGetValue(directory, out modified))
                    {
                        datedItems.Add(new KeyValuePair<string, DateTime>(directory, modified));
                    }
                }

                foreach (FileItem file in files)
                {
                    root.AddFile(file.ImagePath, new ManagedComStream(file, errors));
                    if (file.ModifiedUtc.HasValue)
                    {
                        datedItems.Add(new KeyValuePair<string, DateTime>(file.ImagePath, file.ModifiedUtc.Value));
                    }
                }

                // IMAPI2 records item dates as given (UTC here, as the UDF reader confirms).
                foreach (KeyValuePair<string, DateTime> dated in datedItems)
                {
                    dynamic item = root[dated.Key];
                    item.LastModifiedTime = dated.Value;
                    item.CreationTime = dated.Value;
                    Marshal.ReleaseComObject(item);
                }

                dynamic result = image.CreateResultImage();
                long blocks = result.TotalBlocks;
                long blockSize = result.BlockSize;
                var stream = (IStream)result.ImageStream;
                return new ImapiImage(image, result, stream, blocks * blockSize, errors);
            }
            catch (COMException ex)
            {
                if (image != null)
                {
                    Marshal.FinalReleaseComObject(image);
                }

                errors.ThrowIfSet();
                throw new IsoCreationException("Windows IMAPI2 could not lay out the image: " + ex.Message.Trim() +
                                               " (HRESULT 0x" + ex.HResult.ToString("X8") + ")", ex);
            }
        }

        /// <summary>Holds the first exception thrown by a source stream while IMAPI2 was reading it.</summary>
        internal sealed class SourceErrorBox
        {
            private ExceptionDispatchInfo _error;

            public void Set(Exception ex)
            {
                if (_error == null)
                {
                    _error = ExceptionDispatchInfo.Capture(ex);
                }
            }

            public void ThrowIfSet()
            {
                _error?.Throw();
            }
        }

        /// <summary>An IStream over one source file, opened only when IMAPI2 first reads it.</summary>
        private sealed class ManagedComStream : IStream
        {
            private readonly FileItem _file;
            private readonly SourceErrorBox _errors;
            private Stream _stream;
            private long _position;

            public ManagedComStream(FileItem file, SourceErrorBox errors)
            {
                _file = file;
                _errors = errors;
            }

            public void Read(byte[] pv, int cb, IntPtr pcbRead)
            {
                try
                {
                    if (_stream == null)
                    {
                        _stream = _file.Open();
                    }

                    int total = 0;
                    while (total < cb)
                    {
                        int read = _stream.Read(pv, total, cb - total);
                        if (read <= 0)
                        {
                            break;
                        }

                        total += read;
                    }

                    _position += total;
                    if (total == 0 || _position >= _file.Length)
                    {
                        _stream.Dispose();
                    }

                    if (pcbRead != IntPtr.Zero)
                    {
                        Marshal.WriteInt32(pcbRead, total);
                    }
                }
                catch (Exception ex)
                {
                    _errors.Set(ex);
                    throw;
                }
            }

            public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
                long target = dwOrigin == 0 ? dlibMove : dwOrigin == 1 ? _position + dlibMove : _file.Length + dlibMove;
                if (target != _position)
                {
                    if (_stream == null && target == 0)
                    {
                        _position = 0;
                    }
                    else
                    {
                        var ex = new IOException("IMAPI2 asked to move within '" + _file.ImagePath + "', which is read once from start to end.");
                        _errors.Set(ex);
                        throw ex;
                    }
                }

                if (plibNewPosition != IntPtr.Zero)
                {
                    Marshal.WriteInt64(plibNewPosition, _position);
                }
            }

            public void Stat(out STATSTG pstatstg, int grfStatFlag)
            {
                pstatstg = new STATSTG
                {
                    type = 2, // STGTY_STREAM
                    cbSize = _file.Length,
                    pwcsName = (grfStatFlag & 1) != 0 ? null : Path.GetFileName(_file.ImagePath)
                };
            }

            public void Write(byte[] pv, int cb, IntPtr pcbWritten)
            {
                throw new NotSupportedException();
            }

            public void SetSize(long libNewSize)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
                throw new NotSupportedException();
            }

            public void Commit(int grfCommitFlags)
            {
            }

            public void Revert()
            {
                throw new NotSupportedException();
            }

            public void LockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            public void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            public void Clone(out IStream ppstm)
            {
                throw new NotSupportedException();
            }
        }
    }

    /// <summary>An IMAPI2 result image: a read-only stream of known length over the COM image stream.</summary>
    internal sealed class ImapiImage : Stream
    {
        private readonly object _image;
        private readonly object _result;
        private readonly IStream _stream;
        private readonly long _length;
        private readonly ImapiImageBuilder.SourceErrorBox _errors;
        private readonly IntPtr _readCount = Marshal.AllocHGlobal(sizeof(int));
        private byte[] _buffer = new byte[0];
        private long _position;
        private bool _disposed;

        internal ImapiImage(object image, object result, IStream stream, long length, ImapiImageBuilder.SourceErrorBox errors)
        {
            _image = image;
            _result = result;
            _stream = stream;
            _length = length;
            _errors = errors;
        }

        public override bool CanRead => !_disposed;

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
            if (count == 0 || _position >= _length)
            {
                return 0;
            }

            byte[] target = buffer;
            if (offset != 0)
            {
                if (_buffer.Length < count)
                {
                    _buffer = new byte[count];
                }

                target = _buffer;
            }

            int read;
            try
            {
                _stream.Read(target, count, _readCount);
                read = Marshal.ReadInt32(_readCount);
            }
            catch (COMException ex)
            {
                _errors.ThrowIfSet();
                throw new IsoCreationException("Windows IMAPI2 stopped while generating the image: " + ex.Message.Trim() +
                                               " (HRESULT 0x" + ex.HResult.ToString("X8") + ")", ex);
            }

            _errors.ThrowIfSet();
            if (offset != 0 && read > 0)
            {
                Buffer.BlockCopy(_buffer, 0, buffer, offset, read);
            }

            _position += read;
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                Marshal.FreeHGlobal(_readCount);
                foreach (object com in new[] { (object)_stream, _result, _image })
                {
                    if (com != null && Marshal.IsComObject(com))
                    {
                        Marshal.FinalReleaseComObject(com);
                    }
                }
            }

            base.Dispose(disposing);
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
