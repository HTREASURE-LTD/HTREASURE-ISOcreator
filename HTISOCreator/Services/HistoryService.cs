using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using HTISOCreator.Models;

namespace HTISOCreator.Services
{
    /// <summary>
    /// Stores ISO creation history as XML in the user's roaming application data folder.
    /// Writes are atomic (temporary file, then replace). A damaged file is kept aside under a
    /// new name, so its data is not lost, and history starts empty.
    /// </summary>
    public sealed class HistoryService
    {
        public const int MaxEntries = 1000;

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<HistoryEntry>), new XmlRootAttribute("History"));
        private readonly object _sync = new object();
        private readonly LoggingService _log;

        public HistoryService()
            : this(DefaultHistoryFile, LoggingService.Instance)
        {
        }

        public HistoryService(string historyFile, LoggingService log)
        {
            HistoryFile = historyFile;
            _log = log;
        }

        public static string DefaultHistoryFile =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HT Software", "HT ISO Creator", "history.xml");

        public string HistoryFile { get; }

        /// <summary>Newest first.</summary>
        public List<HistoryEntry> Load()
        {
            lock (_sync)
            {
                return LoadUnlocked();
            }
        }

        public bool Add(HistoryEntry entry)
        {
            return Modify(list =>
            {
                list.Insert(0, entry);
                if (list.Count > MaxEntries)
                {
                    list.RemoveRange(MaxEntries, list.Count - MaxEntries);
                }
            });
        }

        public bool Update(HistoryEntry entry)
        {
            return Modify(list =>
            {
                int index = list.FindIndex(e => e.Id == entry.Id);
                if (index >= 0)
                {
                    list[index] = entry;
                }
            });
        }

        /// <summary>Records a verification result on the newest entry for that ISO file, if any.</summary>
        public bool RecordVerification(string isoPath, VerificationResult result)
        {
            return Modify(list =>
            {
                HistoryEntry entry = list.FirstOrDefault(e => e.Outcome == CreationOutcome.Succeeded &&
                                                              string.Equals(e.OutputPath, isoPath, StringComparison.OrdinalIgnoreCase));
                entry?.ApplyVerification(result);
            });
        }

        public bool Remove(IEnumerable<string> ids)
        {
            var set = new HashSet<string>(ids);
            return Modify(list => list.RemoveAll(e => set.Contains(e.Id)));
        }

        public bool Clear()
        {
            return Modify(list => list.Clear());
        }

        public static string ExportCsv(IEnumerable<HistoryEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Result,Source folder,ISO file,Volume label,Format,Size (bytes),Files,Folders,Duration (s),Verification,Message");
            foreach (HistoryEntry e in entries)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    e.Outcome.ToString(),
                    Csv(e.SourceFolder),
                    Csv(e.OutputPath),
                    Csv(e.VolumeLabel),
                    Csv(e.FormatName),
                    e.ImageSize.ToString(CultureInfo.InvariantCulture),
                    e.FileCount.ToString(CultureInfo.InvariantCulture),
                    e.DirectoryCount.ToString(CultureInfo.InvariantCulture),
                    e.DurationSeconds.ToString("0.0", CultureInfo.InvariantCulture),
                    e.Verification.ToString(),
                    Csv(e.Message)
                }));
            }

            return sb.ToString();
        }

        private bool Modify(Action<List<HistoryEntry>> change)
        {
            lock (_sync)
            {
                List<HistoryEntry> list = LoadUnlocked();
                change(list);
                return SaveUnlocked(list);
            }
        }

        private List<HistoryEntry> LoadUnlocked()
        {
            if (!File.Exists(HistoryFile))
            {
                return new List<HistoryEntry>();
            }

            try
            {
                using (FileStream stream = File.OpenRead(HistoryFile))
                {
                    var list = (List<HistoryEntry>)Serializer.Deserialize(stream) ?? new List<HistoryEntry>();
                    return list.Where(e => e != null).OrderByDescending(e => e.Timestamp).ToList();
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is XmlException)
            {
                string kept = HistoryFile + ".damaged-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                _log.Error("History file is damaged; it was kept as '" + kept + "' and history starts empty", ex);
                try
                {
                    File.Move(HistoryFile, kept);
                }
                catch (Exception moveError) when (moveError is IOException || moveError is UnauthorizedAccessException)
                {
                    _log.Error("Damaged history file could not be renamed", moveError);
                }

                return new List<HistoryEntry>();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _log.Error("History file could not be read", ex);
                return new List<HistoryEntry>();
            }
        }

        private bool SaveUnlocked(List<HistoryEntry> list)
        {
            string temp = HistoryFile + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFile));
                using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
                {
                    Serializer.Serialize(writer, list);
                }

                if (File.Exists(HistoryFile))
                {
                    File.Replace(temp, HistoryFile, null);
                }
                else
                {
                    File.Move(temp, HistoryFile);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                _log.Error("History could not be saved to '" + HistoryFile + "'", ex);
                try
                {
                    File.Delete(temp);
                }
                catch (Exception)
                {
                    // Best effort cleanup.
                }

                return false;
            }
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
