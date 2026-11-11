using System.Collections.Generic;
using System.Linq;
using HTISOCreator.Helpers;

namespace HTISOCreator.Models
{
    /// <summary>
    /// A reference capacity used to warn when an image will not fit on a target disc. Selecting a
    /// profile never changes the image or its file system, and fitting does not guarantee that a
    /// particular drive, burner or blank disc will accept it.
    /// </summary>
    public sealed class CapacityProfile
    {
        public const string NoLimitId = "none";
        public const string CustomId = "custom";

        private CapacityProfile(string id, string name, long? bytes)
        {
            Id = id;
            Name = name;
            Bytes = bytes;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>Usable data capacity in bytes; null for "no limit" and "custom".</summary>
        public long? Bytes { get; }

        /// <summary>
        /// Standard user-data capacities in 2048-byte sectors: CD-ROM 74 min (333,000) and
        /// 80 min (360,000); DVD±R (2,295,104), DVD±R DL (4,173,824); BD-R single layer (12,219,392).
        /// </summary>
        public static readonly IReadOnlyList<CapacityProfile> All = new[]
        {
            new CapacityProfile(NoLimitId, "No capacity limit", null),
            new CapacityProfile("cd650", "CD 650 MB", 333000L * 2048),
            new CapacityProfile("cd700", "CD 700 MB", 360000L * 2048),
            new CapacityProfile("dvd5", "DVD 4.7 GB", 2295104L * 2048),
            new CapacityProfile("dvd9", "DVD Dual Layer 8.5 GB", 4173824L * 2048),
            new CapacityProfile("bd25", "Blu-ray 25 GB", 12219392L * 2048),
            new CapacityProfile(CustomId, "Custom size", null)
        };

        public static CapacityProfile Find(string id)
        {
            return All.FirstOrDefault(p => p.Id == id) ?? All[0];
        }

        /// <summary>Capacity in bytes for this profile; custom sizes are given in MB (1 MB = 1,048,576 bytes).</summary>
        public long? GetCapacityBytes(int customMegabytes)
        {
            return Id == CustomId ? (long?)customMegabytes * 1024 * 1024 : Bytes;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum CapacityState
    {
        NoLimit,
        SizeUnknown,
        Fits,
        Exceeds
    }

    public sealed class CapacityEvaluation
    {
        public CapacityState State { get; set; }

        public long? ImageBytes { get; set; }

        public long? CapacityBytes { get; set; }

        public double PercentUsed { get; set; }

        public long OverflowBytes { get; set; }

        public string ProfileName { get; set; }

        public static CapacityEvaluation Evaluate(long? imageBytes, long? capacityBytes, string profileName)
        {
            var evaluation = new CapacityEvaluation { ImageBytes = imageBytes, CapacityBytes = capacityBytes, ProfileName = profileName };
            if (!capacityBytes.HasValue)
            {
                evaluation.State = CapacityState.NoLimit;
            }
            else if (!imageBytes.HasValue)
            {
                evaluation.State = CapacityState.SizeUnknown;
            }
            else
            {
                evaluation.PercentUsed = capacityBytes.Value == 0 ? 100 : imageBytes.Value * 100.0 / capacityBytes.Value;
                evaluation.State = imageBytes.Value <= capacityBytes.Value ? CapacityState.Fits : CapacityState.Exceeds;
                evaluation.OverflowBytes = System.Math.Max(0, imageBytes.Value - capacityBytes.Value);
            }

            return evaluation;
        }

        public string Describe()
        {
            switch (State)
            {
                case CapacityState.NoLimit:
                    return "No capacity limit selected.";
                case CapacityState.SizeUnknown:
                    return "Capacity " + FileSizeFormatter.Format(CapacityBytes.Value) + "; image size not known yet.";
                case CapacityState.Fits:
                    return string.Format("Fits: {0:0.#}% of {1} ({2}).", PercentUsed, ProfileName, FileSizeFormatter.Format(CapacityBytes.Value));
                default:
                    return "Too large for " + ProfileName + " by " + FileSizeFormatter.Format(OverflowBytes) + ".";
            }
        }
    }
}
