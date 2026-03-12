using System.IO;

namespace Bookmark_App.DataAccess
{
    public static class DbConfig
    {
        public static string DatabasePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bookmark.db");

        public static string ConnectionString => $"Data Source={DatabasePath}";

        public static string SnapshotDirectory
        {
            get
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshot");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string SnapshotPath =>
            Path.Combine(SnapshotDirectory, "bookmark.snapshot.db");
        public static string ManifestFilePath =>
            Path.Combine(SnapshotDirectory, "bookmark.manifest.json");

        public static string CloudIncomingDirectory
        {
            get
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloud-incoming");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public static string IncomingSnapshotFilePath =>
            Path.Combine(CloudIncomingDirectory, "bookmark.snapshot.db");

        public static string IncomingManifestFilePath =>
            Path.Combine(CloudIncomingDirectory, "bookmark.manifest.json");
    }
}
