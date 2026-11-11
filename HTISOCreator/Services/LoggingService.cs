using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Writes a daily text log under %LOCALAPPDATA%\HTREASURE LTD\HT ISO Creator\Logs.
    /// Logging never throws: a failure to log must not break ISO creation.
    /// File contents are never logged, only paths, options and errors.
    /// Each operation gets a short reference (e.g. "a1b2c3d4") that is shown to the user in
    /// error messages and history, so the matching log lines are easy to find.
    /// </summary>
    public sealed class LoggingService
    {
        public const int DefaultRetentionDays = 30;
        private static readonly Lazy<LoggingService> LazyInstance = new Lazy<LoggingService>(() => new LoggingService(DefaultLogDirectory));
        private readonly object _sync = new object();

        public LoggingService(string logDirectory)
        {
            LogDirectory = logDirectory;
        }

        public static LoggingService Instance => LazyInstance.Value;

        public static string DefaultLogDirectory =>
            Path.Combine(Helpers.AppPaths.LocalFolder, "Logs");

        public string LogDirectory { get; }

        public string CurrentLogFile =>
            Path.Combine(LogDirectory, "HTISOCreator-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");

        /// <summary>A new short reference for one operation.</summary>
        public static string NewReference()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public void Info(string message)
        {
            Write("INFO ", message, null);
        }

        public void Warning(string message)
        {
            Write("WARN ", message, null);
        }

        public void Error(string message, Exception exception = null)
        {
            Write("ERROR", message, exception);
        }

        /// <summary>Records the runtime environment once at startup, to help diagnose problems.</summary>
        public void LogEnvironment(string appVersion)
        {
            Info("HT ISO Creator " + appVersion + " started. OS: " + Environment.OSVersion +
                 (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit") +
                 ", process: " + (Environment.Is64BitProcess ? "64-bit" : "32-bit") +
                 ", .NET Framework: " + GetFrameworkVersion() +
                 ", culture: " + CultureInfo.CurrentCulture.Name +
                 ", processors: " + Environment.ProcessorCount);
        }

        /// <summary>Deletes log files older than <paramref name="days"/> days. Returns how many were deleted.</summary>
        public int DeleteOldLogs(int days)
        {
            int deleted = 0;
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    return 0;
                }

                DateTime cutoff = DateTime.Now.AddDays(-days);
                foreach (FileInfo file in new DirectoryInfo(LogDirectory).GetFiles("HTISOCreator-*.log").Where(f => f.LastWriteTime < cutoff))
                {
                    try
                    {
                        file.Delete();
                        deleted++;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        Warning("Old log file could not be deleted: " + file.Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Warning("Old log files could not be listed: " + ex.Message);
            }

            if (deleted > 0)
            {
                Info("Deleted " + deleted + " log file(s) older than " + days + " days");
            }

            return deleted;
        }

        private void Write(string level, string message, Exception exception)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                  .Append(" [").Append(level).Append("] ")
                  .Append(message);
                for (Exception e = exception; e != null; e = e.InnerException)
                {
                    sb.AppendLine().Append("    ").Append(e.GetType().FullName)
                      .Append(" (HRESULT 0x").Append(e.HResult.ToString("X8", CultureInfo.InvariantCulture)).Append("): ").Append(e.Message);
                }

                if (exception != null)
                {
                    sb.AppendLine().Append(exception.StackTrace);
                }

                sb.AppendLine();

                lock (_sync)
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(CurrentLogFile, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Deliberately ignored: logging is best effort.
            }
        }

        private static string GetFrameworkVersion()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    object release = key?.GetValue("Release");
                    object version = key?.GetValue("Version");
                    return (version ?? "unknown") + " (release " + (release ?? "?") + ")";
                }
            }
            catch (Exception)
            {
                return Environment.Version.ToString();
            }
        }
    }
}
