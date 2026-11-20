using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// The application icon: HT ISO Creator's product icon from the HTREASURE brand kit (branding\Icons\HT-iso-maker.ico,
    /// embedded; it holds 16-256 px images,
    /// so title bars stay sharp at every display scale).
    /// </summary>
    public static class AppIcon
    {
        private static readonly Lazy<Icon> LazyIcon = new Lazy<Icon>(Load);

        public static Icon Icon => LazyIcon.Value;

        private static Icon Load()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HTISOCreator.app.ico"))
            {
                return stream == null ? SystemIcons.Application : new Icon(stream);
            }
        }
    }
}
