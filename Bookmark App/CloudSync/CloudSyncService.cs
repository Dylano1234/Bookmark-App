using Bookmark_App.DataAccess;
using Google.Apis.Drive.v3;
using System.IO;

namespace Bookmark_App.CloudSync
{
    public sealed class CloudSyncService
    {
        private readonly DriveService _drive;

        public CloudSyncService(DriveService drive)
        {
            _drive = drive;
        }

        public async Task UploadCurrentDbAsync(CancellationToken ct)
        {
            try
            {
                // 1) Create snapshot
                await DatabaseSnapshotService.CreateSnapshotAsync(ct);

                if (!System.IO.File.Exists(DbConfig.SnapshotPath))
                    throw new InvalidOperationException($"Snapshot did not get created at: {DbConfig.SnapshotPath}");

                // 2) Create + write manifest
                var state = SyncStateStore.LoadOrCreate();

                var manifest = await ManifestService.CreateManifestForSnapshotAsync(
                    DbConfig.SnapshotPath,
                    deviceId: state.DeviceId,
                    schemaVersion: 1,
                    appVersion: "1.0.0",
                    ct: ct);

                await ManifestService.WriteManifestAsync(DbConfig.ManifestFilePath, manifest, ct);

                // 3) Upload snapshot + manifest
                var provider = new GoogleDriveSyncProvider(_drive);
                await provider.UploadSnapshotAndManifestAsync(DbConfig.SnapshotPath, DbConfig.ManifestFilePath, ct);

                // 4) Update local sync state
                var newState = SyncStateStore.LoadOrCreate();
                newState.LastSyncedSnapshotSha256 = manifest.SnapshotSha256;
                newState.LastSyncedUtc = DateTime.UtcNow;
                SyncStateStore.Save(newState);

                // 5) Cleanup local outgoing files (optional)
                //SafeDelete(DbConfig.SnapshotPath);
                //SafeDelete(DbConfig.ManifestFilePath);
            }
            catch (Exception ex) when (ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancelled", ex, ct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Windows.MessageBox.Show(ex.ToString(), "Sync error");
                throw;
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* ignore */ }
        }
    }
}
