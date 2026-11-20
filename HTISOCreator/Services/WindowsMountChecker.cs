using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Mounts an image with Windows' own ISO driver (Mount-DiskImage) and checks that Windows lists
    /// the same files and sizes as <see cref="IsoImageReader"/>. The image is always dismounted
    /// again unless it was already mounted before the check.
    /// </summary>
    public class WindowsMountChecker
    {
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(60);
        private readonly LoggingService _log;

        public WindowsMountChecker()
            : this(LoggingService.Instance)
        {
        }

        public WindowsMountChecker(LoggingService log)
        {
            _log = log;
        }

        /// <summary>
        /// Windows can mount ISO images (Mount-DiskImage) from Windows 8 onwards. On Windows 7 the check is skipped;
        /// creation and every other verification check work the same.
        /// </summary>
        public static bool IsAvailable
        {
            get { return IsAvailableOn(Environment.OSVersion.Version); }
        }

        public const string UnavailableMessage = "Not available on Windows 7: Windows can mount ISO images only from Windows 8 onwards. The other checks are not affected.";

        internal static bool IsAvailableOn(Version windowsVersion)
        {
            return windowsVersion >= new Version(6, 2);
        }

        public virtual VerificationCheck Check(string isoPath, IsoImageInfo image, CancellationToken cancellationToken)
        {
            const string Name = "Windows mount";
            if (!IsAvailable)
            {
                return new VerificationCheck(Name, VerificationCheckStatus.Skipped, UnavailableMessage);
            }

            string quoted = "'" + Path.GetFullPath(isoPath).Replace("'", "''") + "'";

            string output;
            string error;
            string existing = RunPowerShell("$d = Get-DiskImage -ImagePath " + quoted + " -ErrorAction Stop; if ($d.Attached) { ($d | Get-Volume).DriveLetter }", out output, out error)
                ? output.Trim()
                : string.Empty;
            bool mountedHere = existing.Length == 0;
            string letter = existing;

            try
            {
                if (mountedHere)
                {
                    if (!RunPowerShell("$i = Mount-DiskImage -ImagePath " + quoted + " -PassThru -ErrorAction Stop; Start-Sleep -Milliseconds 500; ($i | Get-Volume).DriveLetter", out output, out error))
                    {
                        _log.Warning("Mount-DiskImage failed: " + error);
                        return new VerificationCheck(Name, VerificationCheckStatus.Failed, "Windows could not mount the image: " + FirstLine(error));
                    }

                    letter = output.Trim();
                }

                if (letter.Length != 1)
                {
                    return new VerificationCheck(Name, VerificationCheckStatus.Failed, "Windows mounted the image but did not assign it a drive letter.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                return CompareListing(letter + ":\\", image, cancellationToken);
            }
            finally
            {
                if (mountedHere && letter.Length > 0 && !RunPowerShell("Dismount-DiskImage -ImagePath " + quoted + " -ErrorAction Stop | Out-Null", out output, out error))
                {
                    _log.Error("The image could not be dismounted after verification: " + error);
                }
            }
        }

        private static VerificationCheck CompareListing(string root, IsoImageInfo image, CancellationToken cancellationToken)
        {
            var windowsFiles = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var windowsDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relative = path.Substring(root.Length);
                if (Directory.Exists(path))
                {
                    windowsDirectories.Add(relative);
                }
                else
                {
                    windowsFiles[relative] = new FileInfo(path).Length;
                }
            }

            var differences = new List<string>();
            foreach (IsoImageEntry entry in image.Entries)
            {
                if (entry.IsDirectory)
                {
                    if (!windowsDirectories.Contains(entry.Path))
                    {
                        differences.Add("not listed by Windows: " + entry.Path);
                    }

                    continue;
                }

                long size;
                if (!windowsFiles.TryGetValue(entry.Path, out size))
                {
                    differences.Add("not listed by Windows: " + entry.Path);
                }
                else if (size != entry.Length)
                {
                    differences.Add("different size in Windows: " + entry.Path);
                }
            }

            int expectedTotal = image.Entries.Count;
            if (windowsFiles.Count + windowsDirectories.Count != expectedTotal)
            {
                differences.Add("Windows lists " + (windowsFiles.Count + windowsDirectories.Count) + " items, the image records " + expectedTotal);
            }

            return differences.Count == 0
                ? new VerificationCheck("Windows mount", VerificationCheckStatus.Passed,
                    "Windows mounted the image as " + root.TrimEnd('\\') + " and lists the same " + windowsFiles.Count + " files and " + windowsDirectories.Count + " folders with the same sizes.")
                : new VerificationCheck("Windows mount", VerificationCheckStatus.Failed,
                    differences.Count + " difference(s) between Windows and the image records, first: " + differences[0]);
        }

        private static bool RunPowerShell(string script, out string output, out string error)
        {
            // Errors are written as plain text: with -EncodedCommand and redirected output, PowerShell
            // would otherwise write them to stderr as CLIXML, which is not readable in a message.
            string wrapped = "$ProgressPreference='SilentlyContinue'; try { " + script +
                             " } catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(wrapped));
            var info = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(info))
            {
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)CommandTimeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    output = string.Empty;
                    error = "PowerShell did not respond within " + CommandTimeout.TotalSeconds + " seconds.";
                    return false;
                }

                output = stdout.Result;
                error = stderr.Result;
                return process.ExitCode == 0 && error.Trim().Length == 0;
            }
        }

        internal static string FirstLine(string text)
        {
            text = text ?? string.Empty;
            if (text.TrimStart().StartsWith("#< CLIXML", StringComparison.Ordinal))
            {
                text = FromClixml(text);
            }

            string line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "unknown error";
            return line.Trim();
        }

        /// <summary>Extracts the error text from PowerShell's CLIXML error stream (fallback only).</summary>
        private static string FromClixml(string clixml)
        {
            var text = new StringBuilder();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(clixml, "<S S=\"Error\">(.*?)</S>"))
            {
                text.Append(m.Groups[1].Value);
            }

            string decoded = System.Text.RegularExpressions.Regex.Replace(text.ToString(), "_x([0-9A-Fa-f]{4})_",
                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
            return System.Net.WebUtility.HtmlDecode(decoded);
        }
    }
}
