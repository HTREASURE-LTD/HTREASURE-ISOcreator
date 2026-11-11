using System.IO;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// Decides whether items dragged from Explorer can be used as the source folder.
    /// </summary>
    public static class DropValidator
    {
        /// <summary>
        /// Returns the folder to use, or null with <paramref name="error"/> set to a user-facing reason.
        /// </summary>
        public static string GetSourceFolder(string[] droppedPaths, out string error)
        {
            error = null;
            if (droppedPaths == null || droppedPaths.Length == 0)
            {
                error = "Nothing usable was dropped. Drag a folder from File Explorer.";
                return null;
            }

            if (droppedPaths.Length > 1)
            {
                error = "Drop a single folder. " + droppedPaths.Length + " items were dropped; to include several items, put them in one folder first.";
                return null;
            }

            string path = droppedPaths[0];
            if (Directory.Exists(path))
            {
                return path;
            }

            error = File.Exists(path)
                ? "'" + Path.GetFileName(path) + "' is a file. Drop the folder that contains it, or a folder holding the files you want in the image."
                : "The dropped item could not be found.";
            return null;
        }
    }
}
