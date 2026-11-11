using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Scans a source folder: files, sub-folders (including empty ones), total size and
    /// items that cannot be read.
    /// </summary>
    public sealed class FolderAnalysisService
    {
        private const uint IoReparseTagMountPoint = 0xA0000003;
        private const uint IoReparseTagSymlink = 0xA000000C;

        public FolderAnalysisResult Analyze(string sourceFolder, CancellationToken cancellationToken)
        {
            string root = Path.GetFullPath(sourceFolder);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("The source folder does not exist: " + root);
            }

            string rootWithSlash = root.EndsWith("\\", StringComparison.Ordinal) ? root : root + "\\";
            var result = new FolderAnalysisResult(root);
            var pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(root));

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DirectoryInfo current = pending.Pop();
                string currentRelative = Relative(rootWithSlash, current.FullName);

                IEnumerable<FileSystemInfo> entries;
                try
                {
                    // Materialise now so access errors for this folder are caught here.
                    entries = new List<FileSystemInfo>(current.EnumerateFileSystemInfos());
                }
                catch (Exception ex) when (IsAccessError(ex))
                {
                    result.Errors.Add(DisplayPath(currentRelative) + ": " + DescribeAccessError(ex));
                    continue;
                }

                var subDirectories = new List<DirectoryInfo>();
                foreach (FileSystemInfo entry in entries)
                {
                    string relative = Relative(rootWithSlash, entry.FullName);
                    try
                    {
                        var directory = entry as DirectoryInfo;
                        if (directory != null)
                        {
                            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0 && IsLinkOrJunction(directory.FullName))
                            {
                                // Following links could include data twice or loop forever.
                                result.Warnings.Add(relative + ": skipped because it is a link or junction to another folder.");
                                continue;
                            }

                            result.Directories.Add(relative);
                            result.DirectoryTimesUtc[relative] = directory.LastWriteTimeUtc;
                            subDirectories.Add(directory);
                        }
                        else
                        {
                            var file = (FileInfo)entry;
                            long length = file.Length;
                            result.Files.Add(new SourceFileEntry(file.FullName, relative, length, file.LastWriteTimeUtc));
                            result.TotalBytes += length;
                        }
                    }
                    catch (Exception ex) when (IsAccessError(ex))
                    {
                        result.Errors.Add(relative + ": " + DescribeAccessError(ex));
                    }
                }

                // Push in reverse so folders are visited in natural order.
                for (int i = subDirectories.Count - 1; i >= 0; i--)
                {
                    pending.Push(subDirectories[i]);
                }
            }

            result.Directories.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        internal static bool IsAccessError(Exception ex)
        {
            return ex is UnauthorizedAccessException || ex is IOException || ex is SecurityException;
        }

        internal static string DescribeAccessError(Exception ex)
        {
            if (ex is UnauthorizedAccessException || ex is SecurityException)
            {
                return "access denied";
            }

            if (ex is PathTooLongException)
            {
                return "the path is too long (enable Windows long path support or shorten the folder names)";
            }

            if (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                return "it no longer exists";
            }

            return ex.Message;
        }

        private static string Relative(string rootWithSlash, string fullPath)
        {
            if (fullPath.Length < rootWithSlash.Length)
            {
                return string.Empty;
            }

            return fullPath.Substring(rootWithSlash.Length).TrimEnd('\\');
        }

        private static string DisplayPath(string relative)
        {
            return relative.Length == 0 ? "(source folder)" : relative;
        }

        /// <summary>
        /// True for symbolic links and junctions. Other reparse points (for example
        /// OneDrive cloud folders) are ordinary folders and are scanned normally.
        /// </summary>
        private static bool IsLinkOrJunction(string path)
        {
            WIN32_FIND_DATA data;
            IntPtr handle = FindFirstFile(path.TrimEnd('\\'), out data);
            if (handle == InvalidHandleValue)
            {
                return true;
            }

            FindClose(handle);
            return data.dwReserved0 == IoReparseTagMountPoint || data.dwReserved0 == IoReparseTagSymlink;
        }

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFile(string fileName, out WIN32_FIND_DATA findFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FindClose(IntPtr findFile);
    }
}
