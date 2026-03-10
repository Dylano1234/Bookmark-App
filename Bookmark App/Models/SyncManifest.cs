using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Models
{
    public sealed class SyncManifest
    {
        public int ManifestVersion { get; set; } = 1;

        // Identifies the snapshot content you uploaded
        public string SnapshotSha256 { get; set; } = "";

        // When YOU created the snapshot (not Drive’s modifiedTime)
        public DateTime SnapshotCreatedUtc { get; set; }

        // Helps you show “last edited on PC-X”
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = Environment.MachineName;

        // Optional: your DB schema version, app version, etc.
        public int SchemaVersion { get; set; } = 1;
        public string AppVersion { get; set; } = "";
    }
}
