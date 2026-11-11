using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using HTISOCreator.Services;

namespace HTISOCreator.Helpers
{
    public static class DiskSpaceHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(
            string directoryName,
            out ulong freeBytesAvailableToCaller,
            out ulong totalNumberOfBytes,
            out ulong totalNumberOfFreeBytes);

        /// <summary>
        /// Returns the bytes available to the current user on the volume holding
        /// <paramref name="directory"/>. Works for local drives, mapped drives and UNC paths.
        /// Returns null when the value cannot be determined.
        /// </summary>
        public static long? GetAvailableFreeSpace(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return null;
            }

            string path = directory.EndsWith("\\") ? directory : directory + "\\";
            ulong available, total, free;
            if (!GetDiskFreeSpaceEx(path, out available, out total, out free))
            {
                LoggingService.Instance.Warning("GetDiskFreeSpaceEx failed for " + path + ": " + new Win32Exception(Marshal.GetLastWin32Error()).Message);
                return null;
            }

            return available > long.MaxValue ? long.MaxValue : (long)available;
        }
    }
}
