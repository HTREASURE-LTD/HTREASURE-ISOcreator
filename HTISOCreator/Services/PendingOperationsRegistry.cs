using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Records temporary and backup files while an image is being written or replaced, so that
    /// if the application or computer stops unexpectedly they can be dealt with on the next start:
    /// incomplete temporary images are deleted, and an original ISO that was moved aside as a
    /// backup is put back if the new image never replaced it.
    /// </summary>
    public sealed class PendingOperationsRegistry
    {
        private const string TempKind = "temp";
        private const string BackupKind = "backup";
        private readonly object _sync = new object();
        private readonly LoggingService _log;

        public PendingOperationsRegistry(string registryFile, LoggingService log)
        {
            RegistryFile = registryFile;
            _log = log;
        }

        public static string DefaultRegistryFile =>
            Path.Combine(Helpers.AppPaths.LocalFolder, "pending-operations.txt");

        /// <summary>Registry file used by development builds before version 1.0.0.</summary>
        public static string LegacyRegistryFile =>
            Path.Combine(Helpers.AppPaths.LegacyLocalFolder, "pending-operations.txt");

        public string RegistryFile { get; }

        public void RegisterTemporaryFile(string path)
        {
            Change(lines => lines.Add(TempKind + "\t" + path));
        }

        public void RegisterBackup(string backupPath, string destination)
        {
            Change(lines => lines.Add(BackupKind + "\t" + backupPath + "\t" + destination));
        }

        public void Unregister(string path)
        {
            Change(lines => lines.RemoveAll(l => l.Split('\t').Skip(1).FirstOrDefault() == path));
        }

        /// <summary>Cleans up after an interrupted run. Returns messages describing what was done.</summary>
        public List<string> Recover()
        {
            var messages = new List<string>();
            lock (_sync)
            {
                foreach (string line in ReadLines())
                {
                    string[] parts = line.Split('\t');
                    try
                    {
                        if (parts[0] == TempKind && parts.Length >= 2 && File.Exists(parts[1]))
                        {
                            File.Delete(parts[1]);
                            messages.Add("Removed an incomplete ISO file left by an interrupted run: " + parts[1]);
                        }
                        else if (parts[0] == BackupKind && parts.Length >= 3 && File.Exists(parts[1]))
                        {
                            if (File.Exists(parts[2]))
                            {
                                // The new image had already been moved into place; the backup is the old copy.
                                File.Delete(parts[1]);
                                messages.Add("Removed the previous version of an ISO that had already been replaced: " + parts[1]);
                            }
                            else
                            {
                                File.Move(parts[1], parts[2]);
                                messages.Add("Restored an ISO file that was being replaced when the program stopped: " + parts[2]);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        messages.Add("Could not clean up '" + parts.ElementAtOrDefault(1) + "': " + ex.Message);
                        _log.Error("Recovery of '" + line + "' failed", ex);
                    }
                }

                TryDeleteRegistry();
            }

            foreach (string message in messages)
            {
                _log.Warning("Recovery: " + message);
            }

            return messages;
        }

        private void Change(Action<List<string>> change)
        {
            lock (_sync)
            {
                try
                {
                    List<string> lines = ReadLines();
                    change(lines);
                    Directory.CreateDirectory(Path.GetDirectoryName(RegistryFile));
                    if (lines.Count == 0)
                    {
                        TryDeleteRegistry();
                    }
                    else
                    {
                        File.WriteAllLines(RegistryFile, lines, Encoding.UTF8);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Recovery is a safety net; failing to record must not stop the operation itself.
                    _log.Warning("Pending operations file could not be updated: " + ex.Message);
                }
            }
        }

        private List<string> ReadLines()
        {
            return File.Exists(RegistryFile)
                ? File.ReadAllLines(RegistryFile, Encoding.UTF8).Where(l => l.Length > 0).ToList()
                : new List<string>();
        }

        private void TryDeleteRegistry()
        {
            try
            {
                File.Delete(RegistryFile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _log.Warning("Pending operations file could not be deleted: " + ex.Message);
            }
        }
    }
}
