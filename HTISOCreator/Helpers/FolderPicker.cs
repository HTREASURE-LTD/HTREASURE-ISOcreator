using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HTISOCreator.Services;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// Shows the modern Windows "Select Folder" dialog (IFileOpenDialog with FOS_PICKFOLDERS),
    /// the same dialog File Explorer uses. Falls back to FolderBrowserDialog if COM fails.
    /// </summary>
    public static class FolderPicker
    {
        private const uint FosNoChangeDir = 0x00000008;
        private const uint FosPickFolders = 0x00000020;
        private const uint FosForceFileSystem = 0x00000040;
        private const uint FosPathMustExist = 0x00000800;
        private const uint SigdnFileSysPath = 0x80058000;
        private const int ErrorCancelled = unchecked((int)0x800704C7);

        /// <summary>Returns the chosen folder, or null if the user cancelled.</summary>
        public static string Show(IWin32Window owner, string title, string initialFolder)
        {
            try
            {
                return ShowModern(owner, title, initialFolder);
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidCastException)
            {
                LoggingService.Instance.Warning("Modern folder dialog unavailable, using the classic one: " + ex.Message);
                return ShowClassic(owner, title, initialFolder);
            }
        }

        private static string ShowModern(IWin32Window owner, string title, string initialFolder)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRcw();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosPathMustExist | FosNoChangeDir);
                dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                {
                    IShellItem folder;
                    Guid shellItemId = typeof(IShellItem).GUID;
                    if (SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, ref shellItemId, out folder) == 0)
                    {
                        dialog.SetFolder(folder);
                        Marshal.ReleaseComObject(folder);
                    }
                }

                int hr = dialog.Show(owner?.Handle ?? IntPtr.Zero);
                if (hr == ErrorCancelled)
                {
                    return null;
                }

                Marshal.ThrowExceptionForHR(hr);

                IShellItem result;
                dialog.GetResult(out result);
                try
                {
                    string path;
                    result.GetDisplayName(SigdnFileSysPath, out path);
                    return path;
                }
                finally
                {
                    Marshal.ReleaseComObject(result);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        private static string ShowClassic(IWin32Window owner, string title, string initialFolder)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = title;
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                {
                    dialog.SelectedPath = initialFolder;
                }

                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid riid, out IShellItem item);

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRcw
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint fileTypes, IntPtr filterSpec);

            void SetFileTypeIndex(uint fileType);

            void GetFileTypeIndex(out uint fileType);

            void Advise(IntPtr events, out uint cookie);

            void Unadvise(uint cookie);

            void SetOptions(uint options);

            void GetOptions(out uint options);

            void SetDefaultFolder(IShellItem item);

            void SetFolder(IShellItem item);

            void GetFolder(out IShellItem item);

            void GetCurrentSelection(out IShellItem item);

            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

            void GetResult(out IShellItem item);

            void AddPlace(IShellItem item, int placement);

            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);

            void Close(int hr);

            void SetClientGuid(ref Guid guid);

            void ClearClientData();

            void SetFilter(IntPtr filter);

            void GetResults(out IntPtr items);

            void GetSelectedItems(out IntPtr items);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);

            void GetParent(out IShellItem parent);

            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string name);

            void GetAttributes(uint mask, out uint attributes);

            void Compare(IShellItem other, uint hint, out int order);
        }
    }
}
