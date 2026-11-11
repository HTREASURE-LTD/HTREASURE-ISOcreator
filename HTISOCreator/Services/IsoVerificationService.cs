using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HTISOCreator.Helpers;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Verifies an ISO image with <see cref="IsoImageReader"/> (independent of the DiscUtils writer)
    /// and, when a source folder is given, compares every path, size and optionally SHA-256 hash.
    /// A verification only passes when every check that ran passed; skipped checks are reported
    /// as skipped, never as passed.
    /// </summary>
    public sealed class IsoVerificationService
    {
        private const int HashBufferSize = 1024 * 1024;
        private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(150);

        private readonly LoggingService _log;
        private readonly WindowsMountChecker _mountChecker;

        public IsoVerificationService()
            : this(LoggingService.Instance, new WindowsMountChecker())
        {
        }

        public IsoVerificationService(LoggingService log, WindowsMountChecker mountChecker)
        {
            _log = log;
            _mountChecker = mountChecker;
        }

        public Task<VerificationResult> VerifyAsync(VerificationOptions options, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => Verify(options, null, progress, cancellationToken), cancellationToken);
        }

        /// <summary>Verifies a freshly created image against the plan it was built from (no rescan, same name mapping).</summary>
        public Task<VerificationResult> VerifyAsync(VerificationOptions options, IsoBuildPlan plan, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => Verify(options, plan, progress, cancellationToken), cancellationToken);
        }

        public VerificationResult Verify(VerificationOptions options, IsoBuildPlan plan, IProgress<IsoProgressInfo> progress, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new VerificationResult { IsoPath = options.IsoPath, SourceFolder = plan?.Analysis.RootPath ?? options.SourceFolder };
            _log.Info("Verification started: iso='" + options.IsoPath + "', source='" + (result.SourceFolder ?? "(none)") +
                      "', hashes=" + options.CompareHashes + ", windowsMount=" + options.MountWithWindows);
            Report(progress, "Reading image structure...", stopwatch);

            try
            {
                if (!CheckImageFile(options.IsoPath, result))
                {
                    return Finish(result, stopwatch);
                }

                IsoImageInfo image;
                try
                {
                    image = IsoImageReader.Read(options.IsoPath, cancellationToken);
                }
                catch (InvalidIsoImageException ex)
                {
                    result.Checks.Add(new VerificationCheck("ISO structure", VerificationCheckStatus.Failed, ex.Message));
                    return Finish(result, stopwatch);
                }

                result.Image = image;
                AddStructureChecks(options, image, result);

                Dictionary<string, SourceFileEntry> expectedFiles;
                HashSet<string> expectedDirectories;
                List<IsoNameChange> nameChanges;
                if (BuildExpectation(options, plan, image, result, cancellationToken, out expectedFiles, out expectedDirectories, out nameChanges))
                {
                    CompareWithSource(image, expectedFiles, expectedDirectories, result);
                    CheckNames(options.FilenameMode ?? plan?.Options.FilenameMode, image, expectedFiles, expectedDirectories, nameChanges, result);
                    if (options.CompareHashes)
                    {
                        CompareHashes(options.IsoPath, image, expectedFiles, result, progress, stopwatch, cancellationToken);
                    }
                    else
                    {
                        result.Checks.Add(new VerificationCheck("File contents (SHA-256)", VerificationCheckStatus.Skipped, "Not requested. File contents were not compared."));
                    }
                }

                if (options.MountWithWindows)
                {
                    Report(progress, "Mounting the image with Windows...", stopwatch);
                    result.Checks.Add(_mountChecker.Check(options.IsoPath, image, cancellationToken));
                }
                else
                {
                    result.Checks.Add(new VerificationCheck("Windows mount", VerificationCheckStatus.Skipped, "Not requested."));
                }

                return Finish(result, stopwatch);
            }
            catch (OperationCanceledException)
            {
                _log.Info("Verification cancelled: " + options.IsoPath);
                throw;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _log.Error("Verification could not read a file", ex);
                result.Checks.Add(new VerificationCheck("Read access", VerificationCheckStatus.Failed,
                    "A file could not be read during verification: " + ex.Message));
                return Finish(result, stopwatch);
            }
        }

        private static bool CheckImageFile(string isoPath, VerificationResult result)
        {
            var file = new FileInfo(isoPath);
            if (!file.Exists)
            {
                result.Checks.Add(new VerificationCheck("Image file", VerificationCheckStatus.Failed, "The file does not exist: " + isoPath));
                return false;
            }

            result.Checks.Add(file.Length % IsoImageReader.SectorSize == 0
                ? new VerificationCheck("Image file", VerificationCheckStatus.Passed, FileSizeFormatter.FormatWithBytes(file.Length) + ", a whole number of 2048-byte sectors.")
                : new VerificationCheck("Image file", VerificationCheckStatus.Warning, FileSizeFormatter.FormatWithBytes(file.Length) + " is not a whole number of 2048-byte sectors."));
            return true;
        }

        private static void AddStructureChecks(VerificationOptions options, IsoImageInfo image, VerificationResult result)
        {
            result.Checks.Add(image.Problems.Count == 0
                ? new VerificationCheck("ISO structure", VerificationCheckStatus.Passed,
                    "Volume descriptors and directory trees are valid: " + image.FileCount + " files, " + image.DirectoryCount + " folders" +
                    (image.HasUdf && image.HasIso9660 ? "; the UDF, ISO 9660 and Joliet views agree."
                        : image.HasJoliet ? "; ISO 9660 and Joliet trees agree." : "."))
                : new VerificationCheck("ISO structure", VerificationCheckStatus.Failed,
                    image.Problems.Count + " structural problem(s), first: " + image.Problems[0]));

            if (image.HasIso9660)
            {
                AddTreeCheck(result, image, "ISO 9660 directory tree", "ISO 9660 tree:", image.PrimaryEntries.Count);
            }
            else
            {
                result.Checks.Add(new VerificationCheck("ISO 9660 directory tree", VerificationCheckStatus.Skipped, "The image has no ISO 9660 view (UDF only)."));
            }

            if (image.HasJoliet)
            {
                AddTreeCheck(result, image, "Joliet directory tree", "Joliet tree:", image.JolietEntries.Count);
            }
            else
            {
                result.Checks.Add(new VerificationCheck("Joliet directory tree", VerificationCheckStatus.Skipped, "The image has no Joliet names."));
            }

            if (image.HasUdf)
            {
                AddTreeCheck(result, image, "UDF file system", "UDF", image.UdfEntries.Count);
            }

            List<string> duplicates = image.Problems.Where(p => p.Contains(IsoImageReader.DuplicateEntryText)).ToList();
            result.Checks.Add(duplicates.Count == 0
                ? new VerificationCheck("Duplicate entries", VerificationCheckStatus.Passed, "No folder contains two entries with the same name.")
                : new VerificationCheck("Duplicate entries", VerificationCheckStatus.Failed, duplicates.Count + " duplicate entry problem(s), first: " + duplicates[0]));

            string label = image.DisplayLabel ?? string.Empty;
            if (!string.IsNullOrEmpty(options.ExpectedLabel))
            {
                bool match = (!image.HasIso9660 || string.Equals(image.VolumeLabel, options.ExpectedLabel, StringComparison.Ordinal)) &&
                             (!image.HasJoliet || string.Equals(image.JolietVolumeLabel, options.ExpectedLabel, StringComparison.Ordinal)) &&
                             (!image.HasUdf || string.Equals(image.UdfVolumeLabel, options.ExpectedLabel, StringComparison.Ordinal));
                result.Checks.Add(new VerificationCheck("Volume label", match ? VerificationCheckStatus.Passed : VerificationCheckStatus.Failed,
                    match ? "'" + label + "' as expected." : "Expected '" + options.ExpectedLabel + "' but the image has '" + label + "'."));
            }
            else
            {
                result.Checks.Add(new VerificationCheck("Volume label", VerificationCheckStatus.Passed, "'" + label + "' (read from the image)."));
            }

            string formatName = FileSystemFormats.Name(image.Format) + (image.HasUdf && image.UdfRevision != null ? " (UDF " + image.UdfRevision + ")" : string.Empty);
            if (options.ExpectedFormat.HasValue && options.ExpectedFormat.Value != image.Format)
            {
                result.Checks.Add(new VerificationCheck("File system", VerificationCheckStatus.Failed,
                    "Expected " + FileSystemFormats.Name(options.ExpectedFormat.Value) + " but found " + formatName + "."));
            }
            else
            {
                result.Checks.Add(new VerificationCheck("File system", VerificationCheckStatus.Passed, formatName + "."));
            }
        }

        private static void AddTreeCheck(VerificationResult result, IsoImageInfo image, string name, string prefix, int entries)
        {
            List<string> problems = image.Problems.Where(p => p.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            result.Checks.Add(problems.Count == 0
                ? new VerificationCheck(name, VerificationCheckStatus.Passed, entries + " entries read; every record is valid.")
                : new VerificationCheck(name, VerificationCheckStatus.Failed, problems.Count + " problem(s), first: " + problems[0]));
        }

        /// <summary>
        /// Compares names exactly (letter case included) with the plan, and judges the plan's
        /// conversions by the filename mode. A passing SHA-256 comparison cannot hide a renamed
        /// file: this check fails or warns on its own.
        /// </summary>
        private static void CheckNames(FilenameMode? mode, IsoImageInfo image, Dictionary<string, SourceFileEntry> expectedFiles,
            HashSet<string> expectedDirectories, List<IsoNameChange> changes, VerificationResult result)
        {
            var actualPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (IsoImageEntry entry in image.Entries)
            {
                actualPaths[entry.Path] = entry.Path;
            }

            foreach (string expected in expectedFiles.Keys.Concat(expectedDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                string actual;
                if (actualPaths.TryGetValue(expected, out actual) && !string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    result.NameMismatches.Add("expected '" + expected + "' but the image has '" + actual + "'");
                }
            }

            result.NameDifferences.AddRange(changes.Select(c => c.SourcePath + "  ->  " + c.ImagePath + "  (" + c.Reason + ")"));

            if (result.NameMismatches.Count > 0)
            {
                result.Checks.Add(new VerificationCheck("File names", VerificationCheckStatus.Failed,
                    result.NameMismatches.Count + " name(s) in the image differ from the expected names, first: " + result.NameMismatches[0]));
                return;
            }

            int total = expectedFiles.Count + expectedDirectories.Count;
            if (changes.Count == 0)
            {
                result.Checks.Add(new VerificationCheck("File names", VerificationCheckStatus.Passed,
                    "All " + total + " file and folder names are identical to the originals" +
                    (image.HasUdf ? " (UDF names)." : image.HasJoliet ? " (Joliet names)." : ".")));
            }
            else if (mode == FilenameMode.Strict)
            {
                result.Checks.Add(new VerificationCheck("File names", VerificationCheckStatus.Failed,
                    changes.Count + " name(s) in the image differ from the originals, which Strict Mode does not allow. First: " + result.NameDifferences[0]));
            }
            else if (mode == FilenameMode.Compatibility)
            {
                result.Checks.Add(new VerificationCheck("File names", VerificationCheckStatus.Passed,
                    "Names match the conversion plan exactly: " + changes.Count + " converted as approved (Compatibility Mode), " +
                    (total - changes.Count) + " identical to the originals."));
            }
            else
            {
                result.Checks.Add(new VerificationCheck("File names", VerificationCheckStatus.Warning,
                    changes.Count + " name(s) in the image differ from the originals (listed in the report). First: " + result.NameDifferences[0]));
            }
        }

        /// <summary>
        /// Works out which paths and sizes the image should contain: from the creation plan, or by
        /// scanning the source folder and applying the same name mapping used when creating.
        /// </summary>
        private bool BuildExpectation(VerificationOptions options, IsoBuildPlan plan, IsoImageInfo image, VerificationResult result,
            CancellationToken cancellationToken, out Dictionary<string, SourceFileEntry> files, out HashSet<string> directories, out List<IsoNameChange> changes)
        {
            files = new Dictionary<string, SourceFileEntry>(StringComparer.OrdinalIgnoreCase);
            directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            changes = new List<IsoNameChange>();

            if (plan != null)
            {
                changes.AddRange(plan.RenamedItems);
                foreach (PlannedFile file in plan.Files)
                {
                    files[file.ImagePath] = file.Source;
                }

                directories.UnionWith(plan.Directories);
                return true;
            }

            if (string.IsNullOrEmpty(options.SourceFolder))
            {
                result.Checks.Add(new VerificationCheck("Source comparison", VerificationCheckStatus.Skipped,
                    "No source folder was selected, so file contents were not compared with the original files."));
                return false;
            }

            if (!Directory.Exists(options.SourceFolder))
            {
                result.Checks.Add(new VerificationCheck("Source comparison", VerificationCheckStatus.Failed, "The source folder does not exist: " + options.SourceFolder));
                return false;
            }

            FolderAnalysisResult analysis = new FolderAnalysisService().Analyze(options.SourceFolder, cancellationToken);
            if (analysis.Errors.Count > 0)
            {
                result.Checks.Add(new VerificationCheck("Source folder", VerificationCheckStatus.Failed,
                    analysis.Errors.Count + " source item(s) could not be read, first: " + analysis.Errors[0]));
            }

            IsoNameMapper.MappingResult mapping = IsoNameMapper.Map(analysis.Directories, analysis.Files.Select(f => f.RelativePath), image.Format);
            changes.AddRange(mapping.Changes);
            foreach (SourceFileEntry file in analysis.Files)
            {
                files[mapping.Files[file.RelativePath]] = file;
            }

            directories.UnionWith(analysis.Directories.Select(d => mapping.Directories[d]));
            return true;
        }

        private static void CompareWithSource(IsoImageInfo image, Dictionary<string, SourceFileEntry> expectedFiles, HashSet<string> expectedDirectories, VerificationResult result)
        {
            result.Level = VerificationLevel.SourcePathsAndSizes;
            var imageFiles = image.Entries.Where(e => !e.IsDirectory).ToDictionary(e => e.Path, StringComparer.OrdinalIgnoreCase);
            var imageDirectories = new HashSet<string>(image.Entries.Where(e => e.IsDirectory).Select(e => e.Path), StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, SourceFileEntry> expected in expectedFiles.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                IsoImageEntry entry;
                if (!imageFiles.TryGetValue(expected.Key, out entry))
                {
                    result.MissingFromImage.Add(expected.Key);
                }
                else if (entry.Length != expected.Value.Length)
                {
                    result.SizeMismatches.Add(expected.Key + " (source " + expected.Value.Length.ToString("N0", CultureInfo.InvariantCulture) +
                                              " bytes, image " + entry.Length.ToString("N0", CultureInfo.InvariantCulture) + " bytes)");
                }
            }

            result.MissingFromImage.AddRange(expectedDirectories.Where(d => !imageDirectories.Contains(d)).OrderBy(d => d).Select(d => d + "\\"));
            result.ExtraInImage.AddRange(imageFiles.Keys.Where(p => !expectedFiles.ContainsKey(p)).OrderBy(p => p));
            result.ExtraInImage.AddRange(imageDirectories.Where(d => !expectedDirectories.Contains(d)).OrderBy(d => d).Select(d => d + "\\"));
            result.FilesCompared = expectedFiles.Count;

            bool structureMatches = result.MissingFromImage.Count == 0 && result.ExtraInImage.Count == 0;
            result.Checks.Add(new VerificationCheck("Files and folders", structureMatches ? VerificationCheckStatus.Passed : VerificationCheckStatus.Failed,
                structureMatches
                    ? "All " + expectedFiles.Count + " files and " + expectedDirectories.Count + " folders are present, with nothing extra."
                    : result.MissingFromImage.Count + " missing from the image, " + result.ExtraInImage.Count + " in the image but not in the source."));
            result.Checks.Add(new VerificationCheck("File sizes", result.SizeMismatches.Count == 0 ? VerificationCheckStatus.Passed : VerificationCheckStatus.Failed,
                result.SizeMismatches.Count == 0
                    ? "Every file present in both has the same size."
                    : result.SizeMismatches.Count + " file(s) differ in size from the source."));
        }

        private void CompareHashes(string isoPath, IsoImageInfo image, Dictionary<string, SourceFileEntry> expectedFiles, VerificationResult result,
            IProgress<IsoProgressInfo> progress, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            var imageFiles = image.Entries.Where(e => !e.IsDirectory).ToDictionary(e => e.Path, StringComparer.OrdinalIgnoreCase);
            var comparable = expectedFiles
                .Where(p => imageFiles.ContainsKey(p.Key) && imageFiles[p.Key].Length == p.Value.Length)
                .OrderBy(p => imageFiles[p.Key].ExtentSector)
                .ToList();

            // Each file is read once from the source and once from the image, then the whole image is hashed.
            long total = comparable.Sum(p => p.Value.Length) * 2 + image.FileLength;
            long done = 0;
            TimeSpan lastReport = TimeSpan.Zero;
            int index = 0;
            var buffer = new byte[HashBufferSize];
            Action<int> onRead = n =>
            {
                done += n;
                if (stopwatch.Elapsed - lastReport >= ReportInterval)
                {
                    lastReport = stopwatch.Elapsed;
                    Report(progress, "Comparing file contents (SHA-256)...", stopwatch, done, total, index, comparable.Count, comparable.Count > index ? comparable[index].Key : null);
                }
            };

            using (var imageStream = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024))
            {
                foreach (KeyValuePair<string, SourceFileEntry> pair in comparable)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string sourceHash;
                    try
                    {
                        using (var source = new FileStream(pair.Value.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024))
                        {
                            sourceHash = Hash(source, buffer, onRead, cancellationToken);
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        result.ContentMismatches.Add(pair.Key + " (the source file could not be read: " + SourceFileStream.DescribeOpenError(ex) + ")");
                        index++;
                        continue;
                    }

                    using (Stream extent = IsoImageReader.OpenExtent(imageStream, imageFiles[pair.Key]))
                    {
                        if (Hash(extent, buffer, onRead, cancellationToken) != sourceHash)
                        {
                            result.ContentMismatches.Add(pair.Key);
                        }
                    }

                    result.BytesHashed += pair.Value.Length;
                    index++;
                }

                imageStream.Position = 0;
                Report(progress, "Calculating the SHA-256 of the whole image...", stopwatch, done, total, index, comparable.Count, null);
                result.ImageSha256 = Hash(imageStream, buffer, onRead, cancellationToken);
            }

            result.Level = VerificationLevel.SourceContent;
            result.Checks.Add(new VerificationCheck("File contents (SHA-256)",
                result.ContentMismatches.Count == 0 ? VerificationCheckStatus.Passed : VerificationCheckStatus.Failed,
                result.ContentMismatches.Count == 0
                    ? "SHA-256 of all " + comparable.Count + " comparable files matches the source (" + FileSizeFormatter.Format(result.BytesHashed) + ")."
                    : result.ContentMismatches.Count + " file(s) have different content from the source."));
        }

        private static string Hash(Stream stream, byte[] buffer, Action<int> onRead, CancellationToken cancellationToken)
        {
            using (SHA256 sha = SHA256.Create())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    onRead(read);
                }

                sha.TransformFinalBlock(buffer, 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", string.Empty);
            }
        }

        private VerificationResult Finish(VerificationResult result, Stopwatch stopwatch)
        {
            result.Elapsed = stopwatch.Elapsed;
            _log.Info("Verification finished: " + result.Summary + " iso='" + result.IsoPath + "'");
            foreach (VerificationCheck check in result.Checks.Where(c => c.Status == VerificationCheckStatus.Failed))
            {
                _log.Warning("Verification check failed: " + check.Name + ": " + check.Detail);
            }

            return result;
        }

        private static void Report(IProgress<IsoProgressInfo> progress, string message, Stopwatch stopwatch, long done = 0, long total = 0, int files = 0, int totalFiles = 0, string current = null)
        {
            TimeSpan elapsed = stopwatch.Elapsed;
            TimeSpan? remaining = null;
            if (total > 0 && done > 0 && elapsed >= TimeSpan.FromSeconds(2) && done * 100 >= total)
            {
                remaining = TimeSpan.FromSeconds(elapsed.TotalSeconds / done * (total - done));
            }

            progress?.Report(new IsoProgressInfo
            {
                Stage = IsoOperationStage.Verifying,
                Message = message,
                BytesWritten = done,
                TotalBytes = total,
                FilesProcessed = files,
                TotalFiles = totalFiles,
                CurrentFile = current,
                Elapsed = elapsed,
                EstimatedRemaining = remaining
            });
        }
    }
}
