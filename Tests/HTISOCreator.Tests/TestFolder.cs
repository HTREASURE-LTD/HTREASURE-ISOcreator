using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DiscUtils.Iso9660;
using Microsoft.Win32.SafeHandles;

namespace HTISOCreator.Tests
{
    /// <summary>
    /// A temporary working folder with "source" and "output" sub-folders, deleted on dispose.
    /// </summary>
    internal sealed class TestFolder : IDisposable
    {
        public TestFolder()
        {
            Root = Path.Combine(Path.GetTempPath(), "HTISOCreator.Tests", Guid.NewGuid().ToString("N"));
            Source = Path.Combine(Root, "source folder");
            Output = Path.Combine(Root, "output");
            Directory.CreateDirectory(Source);
            Directory.CreateDirectory(Output);
        }

        public string Root { get; }

        public string Source { get; }

        public string Output { get; }

        public string OutputFile(string name = "test.iso")
        {
            return Path.Combine(Output, name);
        }

        public string AddFile(string relativePath, string content)
        {
            return AddFile(relativePath, Encoding.UTF8.GetBytes(content));
        }

        public string AddFile(string relativePath, byte[] content)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, content);
            return path;
        }

        public string AddDirectory(string relativePath)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>Creates a sparse file of the given logical size that uses almost no disk space.</summary>
        public string AddSparseFile(string relativePath, long length)
        {
            string path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                int bytesReturned;
                if (!DeviceIoControl(stream.SafeFileHandle, FsctlSetSparse, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                stream.SetLength(length);
            }

            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Reads every file in an image into memory, keyed by path without the ";1" version suffix.</summary>
        public static Dictionary<string, byte[]> ReadImageFiles(string isoPath, bool joliet, out List<string> directories, out string volumeLabel)
        {
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (FileStream stream = File.OpenRead(isoPath))
            using (var reader = new CDReader(stream, joliet))
            {
                volumeLabel = reader.VolumeLabel;
                directories = new List<string>();
                foreach (string directory in reader.GetDirectories("\\", "*.*", SearchOption.AllDirectories))
                {
                    directories.Add(directory.TrimStart('\\'));
                }

                foreach (string file in reader.GetFiles("\\", "*.*", SearchOption.AllDirectories))
                {
                    using (Stream content = reader.OpenFile(file, FileMode.Open))
                    using (var memory = new MemoryStream())
                    {
                        content.CopyTo(memory);
                        files[StripVersion(file.TrimStart('\\'))] = memory.ToArray();
                    }
                }
            }

            return files;
        }

        private static string StripVersion(string path)
        {
            int semicolon = path.LastIndexOf(';');
            string withoutVersion = semicolon >= 0 ? path.Substring(0, semicolon) : path;
            return withoutVersion.EndsWith(".", StringComparison.Ordinal) ? withoutVersion.TrimEnd('.') : withoutVersion;
        }

        private const uint FsctlSetSparse = 0x000900C4;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(SafeFileHandle device, uint ioControlCode, IntPtr inBuffer, int inBufferSize,
            IntPtr outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);
    }
}
