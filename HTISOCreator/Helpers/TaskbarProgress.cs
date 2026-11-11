using System;
using System.Runtime.InteropServices;
using HTISOCreator.Services;

namespace HTISOCreator.Helpers
{
    public enum TaskbarProgressState
    {
        None = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    /// <summary>
    /// Mirrors progress on the application's taskbar button (ITaskbarList3).
    /// Any failure disables the feature silently; it is cosmetic.
    /// </summary>
    public sealed class TaskbarProgress
    {
        private readonly IntPtr _window;
        private ITaskbarList3 _taskbar;

        public TaskbarProgress(IntPtr window)
        {
            _window = window;
            try
            {
                _taskbar = (ITaskbarList3)new TaskbarListRcw();
                _taskbar.HrInit();
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidCastException)
            {
                LoggingService.Instance.Warning("Taskbar progress unavailable: " + ex.Message);
                _taskbar = null;
            }
        }

        public void SetState(TaskbarProgressState state)
        {
            Invoke(t => t.SetProgressState(_window, state));
        }

        public void SetValue(long completed, long total)
        {
            Invoke(t => t.SetProgressValue(_window, (ulong)Math.Max(0, completed), (ulong)Math.Max(1, total)));
        }

        private void Invoke(Action<ITaskbarList3> action)
        {
            if (_taskbar == null)
            {
                return;
            }

            try
            {
                action(_taskbar);
            }
            catch (COMException)
            {
                _taskbar = null;
            }
        }

        [ComImport]
        [Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
        private class TaskbarListRcw
        {
        }

        [ComImport]
        [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            void HrInit();

            void AddTab(IntPtr hwnd);

            void DeleteTab(IntPtr hwnd);

            void ActivateTab(IntPtr hwnd);

            void SetActiveAlt(IntPtr hwnd);

            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

            void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);

            void SetProgressState(IntPtr hwnd, TaskbarProgressState state);
        }
    }
}
