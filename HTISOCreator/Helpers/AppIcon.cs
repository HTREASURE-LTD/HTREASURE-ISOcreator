using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// The application icon, loaded from the embedded Resources\app.ico (which holds 16-256 px images,
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
