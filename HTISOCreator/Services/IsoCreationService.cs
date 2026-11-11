using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Iso9660;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// An expected failure with a message that can be shown to the user as-is.
    /// </summary>
    public sealed class IsoCreationException : Exception
    {
        public IsoCreationException(string userMessage, Exception innerException = null)
            : base(userMessage, innerException)
        {
        }
    }

    /// <summary>
    /// Creates ISO 9660 (optionally Joliet) images with DiscUtils.
    ///
    /// DiscUtils' CDBuilder.Build() returns a stream that generates the image on demand. This
    /// service copies that stream to disk itself in 1 MB blocks, which gives exact byte progress
    /// and a safe cancellation point between blocks. The image is written to a temporary file in
    /// the output folder and only renamed to the final name after it is complete.
    /// </summary>
    public sealed class IsoCreationService
    {
        /// <summary>ISO 9660 stores file sizes in 32 bits and DiscUtils does not write multi-extent files.</summary>
        public const long MaxFileSize = uint.MaxValue;

        public const long LargeFileWarningSize = 2L * 1024 * 1024 * 1024;

        private const int CopyBufferSize = 1024 * 1024;
        private const int ErrorHandleDiskFull = 0x27;
        private const int ErrorDiskFull = 0x70;
        private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(150);

        private readonly FolderAnalysisService _folderAnalysis = new FolderAnalysisService();
        private readonly LoggingService _log;
        private readonly SafeFileReplacer _replacer;
        private readonly PendingOperationsRegistry _registry;

        public IsoCreationService()
            : this(LoggingService.Instance, FileSystemOperations.Instance,
                   new PendingOperationsRegistry(PendingOperationsRegistry.DefaultRegistryFile, LoggingService.Instance))
        {
        }

        public IsoCreationService(LoggingService log)
            : this(log, FileSystemOperations.Instance)
        {
        }

        public IsoCreationService(LoggingService log, IFileOperations fileOperations, PendingOperationsRegistry registry = null)
        {
            _log = log;
            _registry = registry;
            _replacer = new SafeFileReplacer(fileOperations, log, registry);
        }

        /// <summary>Returns the free bytes for a folder. Replaceable so tests can simulate a full drive.</summary>
        internal Func<string, long?> FreeSpaceProvider { get; set; } = DiskSpaceHelper.GetAvailableFreeSpace;

        public Task<IsoBuildPlan> PrepareAsync(IsoCreationOptions options, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => Prepare(options, progress, cancellationToken), cancellationToken);
        }

        public Task<IsoCreationResult> CreateAsync(IsoBuildPlan plan, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => Create(plan, progress, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Scans the source, maps names and checks every file can be opened. Nothing is written.
        /// </summary>
        public IsoBuildPlan Prepare(IsoCreationOptions options, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            string sourceError = PathValidator.ValidateSourceFolder(options.SourceFolder);
            if (sourceError != null)
            {
                throw new IsoCreationException(sourceError);
            }

            Report(progress, new IsoProgressInfo { Stage = IsoOperationStage.Analysing, Message = "Scanning source folder...", Elapsed = stopwatch.Elapsed });

            FolderAnalysisResult analysis;
            try
            {
                analysis = _folderAnalysis.Analyze(options.SourceFolder, cancellationToken);
            }
            catch (Exception ex) when (FolderAnalysisService.IsAccessError(ex))
            {
                throw new IsoCreationException("The source folder could not be read: " + FolderAnalysisService.DescribeAccessError(ex) + ".", ex);
            }

            var plan = new IsoBuildPlan(options, analysis);

            string labelError = VolumeLabelValidator.Validate(options.VolumeLabel, options.Format);
            if (labelError != null)
            {
                plan.Errors.Add(labelError);
            }

            string outputError = PathValidator.ValidateOutputFile(options.OutputPath, options.SourceFolder);
            if (outputError != null)
            {
                plan.Errors.Add(outputError);
            }

            foreach (string error in analysis.Errors)
            {
                plan.Errors.Add("Cannot read " + error);
            }

            plan.Warnings.AddRange(analysis.Warnings);

            if (FileSystemFormats.UsesImapi(options.Format))
            {
                string unavailable = ImapiImageBuilder.CheckAvailable();
                if (unavailable != null)
                {
                    plan.Errors.Add(unavailable);
                }
            }

            int largeFiles = 0;
            long maxFileSize = FileSystemFormats.MaxFileSize(options.Format);
            foreach (SourceFileEntry file in analysis.Files)
            {
                if (file.Length > maxFileSize)
                {
                    plan.Errors.Add(file.RelativePath + " is " + FileSizeFormatter.Format(file.Length) + ". " +
                                    FileSystemFormats.Name(options.Format) + " cannot store files of 4 GB or larger (ISO 9660 stores 32-bit sizes). " +
                                    "Choose the UDF file system, which can.");
                }
                else if (file.Length >= LargeFileWarningSize)
                {
                    largeFiles++;
                }
            }

            if (largeFiles > 0)
            {
                plan.Warnings.Add(largeFiles + " file(s) are larger than 2 GB. They are stored correctly, but some older operating systems and players cannot read files over 2 GB from a disc image.");
            }

            IsoNameMapper.MappingResult mapping = IsoNameMapper.Map(analysis.Directories, analysis.Files.Select(f => f.RelativePath), options.Format);
            plan.RenamedItems.AddRange(mapping.Changes);

            plan.Directories.AddRange(analysis.Directories
                .Select(d => mapping.Directories[d])
                .OrderBy(d => d.Count(c => c == '\\'))
                .ThenBy(d => d, StringComparer.Ordinal));

            foreach (KeyValuePair<string, DateTime> time in analysis.DirectoryTimesUtc)
            {
                plan.DirectoryTimesUtc[mapping.Directories[time.Key]] = time.Value;
            }

            foreach (SourceFileEntry file in analysis.Files)
            {
                plan.Files.Add(new PlannedFile(file, mapping.Files[file.RelativePath]));
            }

            CheckDestinationPathsAreUnique(plan);
            ApplyFilenameMode(plan);

            if (options.Format == IsoFileSystemFormat.Iso9660)
            {
                int deepest = plan.Directories.Count == 0 ? 0 : plan.Directories.Max(d => d.Count(c => c == '\\') + 1);
                if (deepest > IsoNameMapper.Iso9660RecommendedMaxDepth)
                {
                    plan.Warnings.Add("The folder structure is " + deepest + " levels deep. Strict ISO 9660 allows " +
                                      IsoNameMapper.Iso9660RecommendedMaxDepth + "; Windows and Linux read deeper images, but some older systems may not.");
                }
            }

            CheckFilesCanBeOpened(plan, progress, stopwatch, cancellationToken);
            CheckDiskSpace(plan);

            _log.Info(string.Format(CultureInfo.InvariantCulture,
                "[{0}] Prepared image: source='{1}', files={2}, folders={3}, bytes={4}, imageSize={5}, renamed={6}, warnings={7}, errors={8}, filenameMode={9}",
                options.OperationId, analysis.RootPath, analysis.FileCount, analysis.DirectoryCount, analysis.TotalBytes,
                plan.ImageSize?.ToString(CultureInfo.InvariantCulture) ?? "n/a", plan.RenamedItems.Count, plan.Warnings.Count, plan.Errors.Count, options.FilenameMode));
            foreach (string error in plan.Errors)
            {
                _log.Warning("[" + options.OperationId + "] Plan error: " + error);
            }

            return plan;
        }

        /// <summary>Lays out a DiscUtils image (ISO 9660, ISO 9660 + Joliet) and returns its generator stream.</summary>
        private static Stream BuildWithDiscUtils(IsoBuildPlan plan, List<SourceFileStream> streams, Action<SourceFileEntry> onOpened,
            Action<SourceFileEntry> onCompleted, ref int filesProcessed)
        {
            var builder = new CDBuilder
            {
                UseJoliet = plan.Options.Format == IsoFileSystemFormat.Iso9660Joliet,
                VolumeIdentifier = plan.Options.VolumeLabel
            };

                // Each item keeps its source last-modified time (DiscUtils otherwise records the build time).
            var shortNameItems = new List<IsoShortNames.Item>();
            foreach (string directory in plan.Directories)
            {
                BuildDirectoryInfo added = builder.AddDirectory(directory);
                shortNameItems.Add(new IsoShortNames.Item(directory, added, true));
                DateTime modified;
                if (plan.DirectoryTimesUtc.TryGetValue(directory, out modified) && IsRecordableTime(modified))
                {
                    added.CreationTime = modified;
                }
            }

            foreach (PlannedFile file in plan.Files)
            {
                BuildFileInfo added;
                if (file.Source.Length == 0)
                {
                    added = builder.AddFile(file.ImagePath, new byte[0]);
                    filesProcessed++;
                }
                else
                {
                    var stream = new SourceFileStream(file.Source, onOpened, onCompleted);
                    streams.Add(stream);
                    added = builder.AddFile(file.ImagePath, stream);
                }

                shortNameItems.Add(new IsoShortNames.Item(file.ImagePath, added, false));
                if (IsRecordableTime(file.Source.LastWriteTimeUtc))
                {
                    added.CreationTime = file.Source.LastWriteTimeUtc;
                }
            }

            IsoShortNames.Apply(shortNameItems);

            return builder.Build();
        }

        /// <summary>Lays out an IMAPI2 image (UDF, ISO 9660 + Joliet + UDF) and returns its generator stream.</summary>
        private static Stream BuildWithImapi(IsoBuildPlan plan, List<SourceFileStream> streams, Action<SourceFileEntry> onOpened,
            Action<SourceFileEntry> onCompleted, ref int filesProcessed)
        {
            var files = new List<ImapiImageBuilder.FileItem>();
            foreach (PlannedFile file in plan.Files)
            {
                PlannedFile current = file;
                if (file.Source.Length == 0)
                {
                    filesProcessed++;
                }

                files.Add(new ImapiImageBuilder.FileItem(file.ImagePath, file.Source.Length, () =>
                    {
                        var stream = new SourceFileStream(current.Source, onOpened, onCompleted);
                        lock (streams)
                        {
                            streams.Add(stream);
                        }

                        return stream;
                    },
                    IsRecordableTime(file.Source.LastWriteTimeUtc) ? file.Source.LastWriteTimeUtc : (DateTime?)null));
            }

            var directoryTimes = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, DateTime> time in plan.DirectoryTimesUtc)
            {
                if (IsRecordableTime(time.Value))
                {
                    directoryTimes[time.Key] = time.Value;
                }
            }

            return ImapiImageBuilder.Build(plan.Options.Format, plan.Options.VolumeLabel, plan.Directories, directoryTimes, files);
        }

        /// <summary>
        /// Safety net behind <see cref="IsoNameMapper"/>: no two source items may share an image path
        /// (compared without case, as Windows reads the image).
        /// </summary>
        private static void CheckDestinationPathsAreUnique(IsoBuildPlan plan)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in plan.Directories.Concat(plan.Files.Select(f => f.ImagePath)))
            {
                if (!seen.Add(path))
                {
                    plan.Errors.Add("Two source items would be stored as '" + path + "' in the image. Rename one of them in the source folder.");
                }
            }
        }

        /// <summary>Strict Mode turns every name conversion into an error that blocks creation.</summary>
        private static void ApplyFilenameMode(IsoBuildPlan plan)
        {
            if (plan.Options.FilenameMode != FilenameMode.Strict || plan.RenamedItems.Count == 0)
            {
                return;
            }

            IsoNameChange first = plan.RenamedItems[0];
            string advice = plan.Options.Format == IsoFileSystemFormat.Iso9660
                ? " Choose a file system with long names (ISO 9660 + Joliet or UDF), rename these items in the source folder, or choose Compatibility Mode to allow the conversions."
                : plan.Options.Format == IsoFileSystemFormat.Iso9660Joliet
                ? " Choose the UDF file system (names up to " + FileSystemFormats.UdfMaxNameLength + " characters), rename or shorten these items in the source folder, or choose Compatibility Mode to allow the conversions."
                : " Rename or shorten these items in the source folder, or choose Compatibility Mode to allow the conversions.";
            plan.StrictModeError = "Strict Mode: " + plan.RenamedItems.Count + " file or folder name(s) cannot be stored exactly in " +
                                   FileSystemFormats.Name(plan.Options.Format) +
                                   ". First: '" + first.SourcePath + "' would become '" + first.ImagePath + "' (" + first.Reason + ")." + advice;
            plan.Errors.Add(plan.StrictModeError);
        }

        /// <summary>
        /// Writes the image described by <paramref name="plan"/>. On failure or cancellation the
        /// partial file is deleted and an exception is thrown; success is never reported otherwise.
        /// </summary>
        public IsoCreationResult Create(IsoBuildPlan plan, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            if (!plan.CanCreate)
            {
                throw new InvalidOperationException("The build plan contains errors.");
            }

            // Names are never changed silently: Strict Mode plans with renames cannot be created at
            // all, and Compatibility Mode plans only after the user approved the conversions.
            if (plan.RenamedItems.Count > 0 &&
                (plan.Options.FilenameMode != FilenameMode.Compatibility || !plan.Options.ConversionsApproved))
            {
                throw new InvalidOperationException("The plan changes " + plan.RenamedItems.Count +
                                                    " name(s), and these conversions have not been approved in Compatibility Mode.");
            }

            IsoCreationOptions options = plan.Options;
            string outputPath = Path.GetFullPath(options.OutputPath);
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDirectory))
            {
                throw new IsoCreationException("The output folder no longer exists: " + outputDirectory);
            }

            if (File.Exists(outputPath) && !options.OverwriteExisting)
            {
                throw new IsoCreationException("The file '" + outputPath + "' already exists.");
            }

            string op = "[" + options.OperationId + "] ";
            _log.Info(string.Format(CultureInfo.InvariantCulture,
                "{0}ISO creation started: source='{1}', output='{2}', label='{3}', format={4}, files={5}, folders={6}",
                op, plan.Analysis.RootPath, outputPath, options.VolumeLabel, options.Format, plan.Files.Count, plan.Directories.Count));

            string tempPath = Path.Combine(outputDirectory,
                Path.GetFileName(outputPath) + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".partial");
            var streams = new List<SourceFileStream>();
            var stopwatch = Stopwatch.StartNew();
            bool succeeded = false;
            int filesProcessed = 0;
            string currentFile = null;

            try
            {
                Stream builtImage;
                if (FileSystemFormats.UsesImapi(options.Format))
                {
                    builtImage = BuildWithImapi(plan, streams, e => currentFile = e.RelativePath, e => filesProcessed++, ref filesProcessed);
                }
                else
                {
                    builtImage = BuildWithDiscUtils(plan, streams, e => currentFile = e.RelativePath, e => filesProcessed++, ref filesProcessed);
                }

                Report(progress, new IsoProgressInfo
                {
                    Stage = IsoOperationStage.Writing,
                    Message = "Calculating image layout...",
                    TotalFiles = plan.Files.Count,
                    FilesProcessed = filesProcessed,
                    Elapsed = stopwatch.Elapsed
                });

                long imageSize;
                using (Stream image = builtImage)
                {
                    imageSize = image.Length;
                    long? freeSpace = FreeSpaceProvider(outputDirectory);
                    if (freeSpace.HasValue && freeSpace.Value < imageSize)
                    {
                        throw new IsoCreationException("There is not enough free space on the output drive. The image needs " +
                                                       FileSizeFormatter.Format(imageSize) + " but only " +
                                                       FileSizeFormatter.Format(freeSpace.Value) + " is available.");
                    }

                    // Recorded so an interrupted run's partial file is removed on the next start.
                    _registry?.RegisterTemporaryFile(tempPath);
                    using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024))
                    {
                        // Reserving the full size first makes a full disk fail immediately rather than near the end.
                        output.SetLength(imageSize);
                        output.Position = 0;

                        var buffer = new byte[CopyBufferSize];
                        long written = 0;
                        TimeSpan lastReport = TimeSpan.Zero;
                        while (written < imageSize)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            int read = image.Read(buffer, 0, (int)Math.Min(buffer.Length, imageSize - written));
                            if (read <= 0)
                            {
                                throw new IsoCreationException("The image generator stopped before the image was complete.");
                            }

                            output.Write(buffer, 0, read);
                            written += read;

                            TimeSpan elapsed = stopwatch.Elapsed;
                            if (elapsed - lastReport >= ReportInterval || written == imageSize)
                            {
                                lastReport = elapsed;
                                Report(progress, new IsoProgressInfo
                                {
                                    Stage = IsoOperationStage.Writing,
                                    Message = "Writing image...",
                                    BytesWritten = written,
                                    TotalBytes = imageSize,
                                    FilesProcessed = filesProcessed,
                                    TotalFiles = plan.Files.Count,
                                    CurrentFile = currentFile,
                                    Elapsed = elapsed,
                                    EstimatedRemaining = EstimateRemaining(elapsed, written, imageSize)
                                });
                            }
                        }

                        output.Flush(true);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                Report(progress, new IsoProgressInfo
                {
                    Stage = IsoOperationStage.Finalising,
                    Message = "Checking that source files did not change...",
                    BytesWritten = imageSize,
                    TotalBytes = imageSize,
                    FilesProcessed = filesProcessed,
                    TotalFiles = plan.Files.Count,
                    Elapsed = stopwatch.Elapsed
                });

                ThrowIfSourceChanged(plan);

                // Re-check: another program may have created the file while the image was written.
                if (File.Exists(outputPath) && !options.OverwriteExisting)
                {
                    throw new IsoCreationException("The file '" + outputPath + "' was created by another program while the image was being written. It has not been replaced.");
                }

                bool replacedExisting = File.Exists(outputPath);
                _replacer.Publish(tempPath, outputPath);
                succeeded = true;
                stopwatch.Stop();

                var result = new IsoCreationResult
                {
                    OutputPath = outputPath,
                    SourceFolder = plan.Analysis.RootPath,
                    VolumeLabel = options.VolumeLabel,
                    Format = options.Format,
                    ReplacedExisting = replacedExisting,
                    ImageSize = new FileInfo(outputPath).Length,
                    Elapsed = stopwatch.Elapsed,
                    FileCount = plan.Files.Count,
                    DirectoryCount = plan.Directories.Count,
                    SourceBytes = plan.Analysis.TotalBytes,
                    LogReference = options.OperationId
                };

                _log.Info(string.Format(CultureInfo.InvariantCulture,
                    "{0}ISO creation completed: output='{1}', size={2}, elapsed={3}", op, outputPath, result.ImageSize, result.Elapsed));
                return result;
            }
            catch (OperationCanceledException)
            {
                _log.Info(op + "ISO creation cancelled by the user: output='" + outputPath + "'");
                throw;
            }
            catch (IsoCreationException ex)
            {
                _log.Error(op + "ISO creation failed: " + ex.Message, ex.InnerException);
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(op + "ISO creation failed", ex);
                throw new IsoCreationException(ToUserMessage(ex), ex);
            }
            finally
            {
                foreach (SourceFileStream stream in streams)
                {
                    stream.Dispose();
                }

                _registry?.Unregister(tempPath);
                if (!succeeded)
                {
                    TryDelete(tempPath);
                }
            }
        }

        /// <summary>Converts an unexpected exception into a message suitable for the user.</summary>
        public static string ToUserMessage(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                var sourceError = e as SourceFileException;
                if (sourceError != null)
                {
                    return "A source file could not be read, so the image was not created.\n\n" + sourceError.Message;
                }

                var creationError = e as IsoCreationException;
                if (creationError != null)
                {
                    return creationError.Message;
                }
            }

            if (ex is UnauthorizedAccessException)
            {
                return "Windows denied access to the output location. Choose a folder you have permission to write to, such as your Documents folder.";
            }

            if (ex is PathTooLongException)
            {
                return "A file path is too long. Shorten the folder names or enable Windows long path support.";
            }

            if (ex is IOException)
            {
                int code = ex.HResult & 0xFFFF;
                if (code == ErrorDiskFull || code == ErrorHandleDiskFull)
                {
                    return "The output drive ran out of free space.";
                }

                return "A file read or write error occurred: " + ex.Message;
            }

            return "An unexpected error occurred while creating the ISO image. Technical details were written to the log file.";
        }

        private void CheckFilesCanBeOpened(IsoBuildPlan plan, IProgress<IsoProgressInfo> progress, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            int total = plan.Files.Count;
            TimeSpan lastReport = TimeSpan.Zero;
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SourceFileEntry file = plan.Files[i].Source;
                if (stopwatch.Elapsed - lastReport >= ReportInterval)
                {
                    lastReport = stopwatch.Elapsed;
                    Report(progress, new IsoProgressInfo
                    {
                        Stage = IsoOperationStage.CheckingFiles,
                        Message = "Checking files can be read...",
                        FilesProcessed = i,
                        TotalFiles = total,
                        CurrentFile = file.RelativePath,
                        Elapsed = stopwatch.Elapsed
                    });
                }

                try
                {
                    using (new FileStream(file.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1))
                    {
                    }
                }
                catch (Exception ex) when (FolderAnalysisService.IsAccessError(ex))
                {
                    plan.Errors.Add("Cannot read " + file.RelativePath + ": " + SourceFileStream.DescribeOpenError(ex));
                }
            }
        }

        /// <summary>
        /// Calculates the exact image size and checks the output drive has room for it, so a lack of
        /// space is reported before anything is written. The existing output file (if any) is kept
        /// until the new image is complete, so it does not count as free space.
        /// </summary>
        private void CheckDiskSpace(IsoBuildPlan plan)
        {
            try
            {
                plan.ImageSize = IsoSizeCalculator.Calculate(plan.Analysis, plan.Options.Format);
            }
            catch (Exception ex)
            {
                _log.Error("[" + plan.Options.OperationId + "] Image size could not be calculated", ex);
                plan.ImageSize = null;
            }

            string outputFull;
            if (!plan.ImageSize.HasValue || !PathValidator.TryGetFullPath(plan.Options.OutputPath, out outputFull))
            {
                return;
            }

            long? free = FreeSpaceProvider(Path.GetDirectoryName(outputFull));
            if (free.HasValue && free.Value < plan.ImageSize.Value)
            {
                plan.Errors.Add("There is not enough free space on the output drive. The image needs " +
                                FileSizeFormatter.Format(plan.ImageSize.Value) + " but only " + FileSizeFormatter.Format(free.Value) + " is available.");
            }
        }

        /// <summary>
        /// Directory records store years 1900-2155. Dates before 1980 are almost always unset
        /// placeholder values, so for those the build time is kept instead.
        /// </summary>
        private static bool IsRecordableTime(DateTime utc)
        {
            return utc.Year >= 1980 && utc.Year <= 2155;
        }

        private static void ThrowIfSourceChanged(IsoBuildPlan plan)
        {
            var changed = new List<string>();
            foreach (PlannedFile file in plan.Files)
            {
                var info = new FileInfo(file.Source.FullPath);
                if (!info.Exists || info.Length != file.Source.Length || info.LastWriteTimeUtc != file.Source.LastWriteTimeUtc)
                {
                    changed.Add(file.Source.RelativePath);
                }
            }

            if (changed.Count > 0)
            {
                throw new IsoCreationException(changed.Count + " source file(s) changed while the image was being created, so the image may be inconsistent and was discarded:\n\n" +
                                               string.Join("\n", changed.Take(10)) +
                                               (changed.Count > 10 ? "\n...and " + (changed.Count - 10) + " more" : string.Empty) +
                                               "\n\nClose any programs that are modifying these files and try again.");
            }
        }

        private static TimeSpan? EstimateRemaining(TimeSpan elapsed, long written, long total)
        {
            // Too early to extrapolate a meaningful rate.
            if (elapsed < TimeSpan.FromSeconds(2) || written <= 0 || written * 100 < total)
            {
                return null;
            }

            double secondsPerByte = elapsed.TotalSeconds / written;
            return TimeSpan.FromSeconds(secondsPerByte * (total - written));
        }

        private void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _log.Info("Removed incomplete file '" + path + "'");
                }
            }
            catch (Exception ex)
            {
                _log.Error("Could not remove incomplete file '" + path + "'", ex);
            }
        }

        private static void Report(IProgress<IsoProgressInfo> progress, IsoProgressInfo info)
        {
            progress?.Report(info);
        }
    }
}
