using System;
using System.IO;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Thrown when a source file cannot be read or has changed since the folder was scanned.
    /// </summary>
    public sealed class SourceFileException : IOException
    {
        public SourceFileException(string relativePath, string message, Exception innerException = null)
            : base(message, innerException)
        {
            RelativePath = relativePath;
        }

        public string RelativePath { get; }
    }

    /// <summary>
    /// A read-only stream over a source file that is handed to DiscUtils. The file is opened
    /// only when DiscUtils first reads it and closed as soon as it has been fully read, so only
    /// one source handle is open at a time. This also lets the creation service report which
    /// file is being written and how many are done, and detect files that changed after the scan.
    /// </summary>
    internal sealed class SourceFileStream : Stream
    {
        private readonly SourceFileEntry _entry;
        private readonly Action<SourceFileEntry> _onOpened;
        private readonly Action<SourceFileEntry> _onCompleted;
        private FileStream _inner;
        private long _position;
        private bool _completed;
        private bool _disposed;

        public SourceFileStream(SourceFileEntry entry, Action<SourceFileEntry> onOpened, Action<SourceFileEntry> onCompleted)
        {
            _entry = entry;
            _onOpened = onOpened;
            _onCompleted = onCompleted;
        }

        public override bool CanRead => !_disposed;

        public override bool CanSeek => !_disposed;

        public override bool CanWrite => false;

        public override long Length => _entry.Length;

        public override long Position
        {
            get { return _position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            if (_position >= _entry.Length || count == 0)
            {
                return 0;
            }

            EnsureOpen();
            if (_inner.Position != _position)
            {
                _inner.Position = _position;
            }

            int wanted = (int)Math.Min(count, _entry.Length - _position);
            int total = 0;
            while (total < wanted)
            {
                int read;
                try
                {
                    read = _inner.Read(buffer, offset + total, wanted - total);
                }
                catch (IOException ex)
                {
                    throw new SourceFileException(_entry.RelativePath, "Could not read '" + _entry.RelativePath + "': " + ex.Message, ex);
                }

                if (read == 0)
                {
                    throw new SourceFileException(_entry.RelativePath, "'" + _entry.RelativePath + "' became shorter while the image was being created.");
                }

                total += read;
            }

            _position += total;
            if (_position >= _entry.Length)
            {
                Complete();
            }

            return total;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            long target;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    target = offset;
                    break;
                case SeekOrigin.Current:
                    target = _position + offset;
                    break;
                default:
                    target = _entry.Length + offset;
                    break;
            }

            if (target < 0)
            {
                throw new IOException("Attempt to seek before the start of the stream.");
            }

            _position = target;
            return _position;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                CloseInner();
            }

            base.Dispose(disposing);
        }

        private void EnsureOpen()
        {
            if (_inner != null)
            {
                return;
            }

            try
            {
                // FileShare.Read: other programs may read, but nobody may modify the file while it is copied.
                _inner = new FileStream(_entry.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
            }
            catch (Exception ex) when (FolderAnalysisService.IsAccessError(ex))
            {
                throw new SourceFileException(_entry.RelativePath, "Could not open '" + _entry.RelativePath + "': " + DescribeOpenError(ex), ex);
            }

            if (_inner.Length != _entry.Length)
            {
                CloseInner();
                throw new SourceFileException(_entry.RelativePath, "'" + _entry.RelativePath + "' changed size after the source folder was scanned.");
            }

            _onOpened?.Invoke(_entry);
        }

        private void Complete()
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            CloseInner();
            _onCompleted?.Invoke(_entry);
        }

        private void CloseInner()
        {
            if (_inner != null)
            {
                _inner.Dispose();
                _inner = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SourceFileStream));
            }
        }

        internal static string DescribeOpenError(Exception ex)
        {
            const int ErrorSharingViolation = 32;
            const int ErrorLockViolation = 33;
            int code = ex.HResult & 0xFFFF;
            if (ex is IOException && !(ex is FileNotFoundException) && (code == ErrorSharingViolation || code == ErrorLockViolation))
            {
                return "the file is in use by another program";
            }

            return FolderAnalysisService.DescribeAccessError(ex);
        }
    }
}
