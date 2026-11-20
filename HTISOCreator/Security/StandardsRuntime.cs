using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HTISOCreator.Security
{
    /// <summary>
    /// HT ISO Creator's identity for the HTREASURE software standards: application id, channel and platform, the
    /// build settings stamped into the program (HTAppId, HTChannel, HTAdvisoryServiceUrl, HTAllowUnsignedSnapshot),
    /// and the trusted keys compiled into it.
    /// </summary>
    public static class StandardsRuntime
    {
        /// <summary>Version of the HTREASURE software standards this port implements.</summary>
        public const string StandardsVersion = "1.0.0";

        public const string ProductName = "HT ISO Creator";

        private static readonly Lazy<SecurityAdvisoryCenter> SharedCenter = new Lazy<SecurityAdvisoryCenter>(CreateCenter);

        public static string AppId
        {
            get { return Metadata("HTAppId") ?? "ht-iso-creator"; }
        }

        public static string Channel
        {
            get { return Metadata("HTChannel") ?? "stable"; }
        }

        public static string AdvisoryServiceUrl
        {
            get { return Metadata("HTAdvisoryServiceUrl"); }
        }

        public static bool AllowUnsignedSnapshot
        {
            get { return string.Equals(Metadata("HTAllowUnsignedSnapshot"), "true", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>windows-x86, windows-x64 or windows-arm64: the Windows installation the program is running on.</summary>
        public static string Platform
        {
            get
            {
                string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ?? Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? string.Empty;
                if (arch.Equals("ARM64", StringComparison.OrdinalIgnoreCase))
                {
                    return "windows-arm64";
                }

                return Environment.Is64BitOperatingSystem ? "windows-x64" : "windows-x86";
            }
        }

        /// <summary>The product version, for example "1.1.0".</summary>
        public static string Version
        {
            get
            {
                Assembly assembly = typeof(StandardsRuntime).Assembly;
                var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return info != null ? info.InformationalVersion : assembly.GetName().Version.ToString(3);
            }
        }

        public static InstalledApplication Application
        {
            get { return new InstalledApplication(AppId, ProductName, Version, Channel, Platform, Metadata("HTBuildId")); }
        }

        /// <summary>The advisory centre for this installation (bundled snapshot in &lt;program folder&gt;\security).</summary>
        public static SecurityAdvisoryCenter Advisories
        {
            get { return SharedCenter.Value; }
        }

        public static TrustedKeySet LoadTrustedKeys()
        {
            using (Stream s = typeof(StandardsRuntime).Assembly.GetManifestResourceStream("HTREASURE.Standards.trusted-keys.json"))
            {
                if (s == null)
                {
                    throw new InvalidOperationException("The trusted advisory keys are missing from the program.");
                }

                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return TrustedKeySet.Load(ms.ToArray());
                }
            }
        }

        private static SecurityAdvisoryCenter CreateCenter()
        {
            return new SecurityAdvisoryCenter(Application, new SecurityAdvisoryOptions
            {
                BundledDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "security"),
                TrustedKeys = LoadTrustedKeys(),
                AdvisoryServiceUrl = AdvisoryServiceUrl,
                AllowUnsignedBundledSnapshot = AllowUnsignedSnapshot
            });
        }

        private static string Metadata(string key)
        {
            string value = typeof(StandardsRuntime).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(a => a.Key == key).Select(a => a.Value).FirstOrDefault();
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
