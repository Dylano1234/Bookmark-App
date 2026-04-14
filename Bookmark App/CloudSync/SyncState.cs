namespace Bookmark_App.CloudSync
{
    public sealed class SyncState
    {
        // Stable per-install ID
        public string DeviceId { get; set; } = "";

        // Last snapshot that THIS device successfully synced
        public string? LastSyncedSnapshotSha256 { get; set; }
        public DateTime? LastSyncedUtc { get; set; }

        // Google Drive bookkeeping
        public string? DriveSnapshotFileId { get; set; }
        public string? DriveManifestFileId { get; set; }

        // Local state
        public bool IsLocalDirty { get; set; } // True if there are local changes that haven't been synced yet
        public bool IsAutoSyncEnabled { get; set; }

    }
}
