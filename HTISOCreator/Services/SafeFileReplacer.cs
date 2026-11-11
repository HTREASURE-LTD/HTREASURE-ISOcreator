using System;
using System.IO;

namespace HTISOCreator.Services
{
    /// <summary>
    /// File operations used when publishing a finished image. Abstracted so tests can inject failures.
    /// </summary>
    public interface IFileOperations
    {
        bool Exists(string path);

        void Move(string source, string destination);

        void Delete(string path);
    }

    public sealed class FileSystemOperations : IFileOperations
    {
        public static readonly FileSystemOperations Instance = new FileSystemOperations();

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void Move(string source, string destination)
        {
            File.Move(source, destination);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Moves a completed temporary file to its final name without ever losing an existing file
    /// at that name.
    ///
    /// If the destination exists it is first renamed to a backup in the same folder (a rename
    /// on the same volume, so no data is copied). Only after the new file is in place is the
    /// backup deleted. If putting the new file in place fails, the backup is renamed back.
    /// The original file is therefore always either at its own name or at the backup name,
    /// and the backup name is reported if it cannot be restored.
    /// </summary>
    public sealed class SafeFileReplacer
    {
        private readonly IFileOperations _files;
        private readonly LoggingService _log;
        private readonly PendingOperationsRegistry _registry;

        public SafeFileReplacer(IFileOperations files, LoggingService log, PendingOperationsRegistry registry = null)
        {
            _files = files;
            _log = log;
            _registry = registry;
        }

        public void Publish(string completedFile, string destination)
        {
            if (!_files.Exists(destination))
            {
                _files.Move(completedFile, destination);
                return;
            }

            string backup = destination + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".backup";

            // Recorded first so that an interruption between the two moves can be undone on the next start.
            _registry?.RegisterBackup(backup, destination);
            try
            {
                _files.Move(destination, backup);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _registry?.Unregister(backup);
                throw new IsoCreationException("The existing file '" + destination + "' could not be replaced because it is in use or protected. " +
                                               "It has not been changed. Close any program using it (for example a mounted drive) and try again.", ex);
            }

            try
            {
                _files.Move(completedFile, destination);
            }
            catch (Exception moveError) when (moveError is IOException || moveError is UnauthorizedAccessException)
            {
                try
                {
                    _files.Move(backup, destination);
                    _registry?.Unregister(backup);
                    _log.Warning("New image could not be moved into place; the original file was restored: " + destination);
                }
                catch (Exception restoreError) when (restoreError is IOException || restoreError is UnauthorizedAccessException)
                {
                    _log.Error("Original file could not be restored from backup '" + backup + "'", restoreError);
                    throw new IsoCreationException("The new image could not be saved, and the original file could not be put back automatically. " +
                                                   "Your original file is safe and has been kept as:\n\n" + backup +
                                                   "\n\nRename it to '" + Path.GetFileName(destination) + "' to restore it.", moveError);
                }

                throw new IsoCreationException("The new image could not be saved as '" + destination + "'. The original file was left unchanged.", moveError);
            }

            try
            {
                _files.Delete(backup);
                _registry?.Unregister(backup);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // The new image is in place; only the old copy is left over. Not a failure.
                // It stays registered, so the next start removes it.
                _log.Warning("The replaced file's backup could not be deleted and was left at '" + backup + "': " + ex.Message);
            }
        }
    }
}
