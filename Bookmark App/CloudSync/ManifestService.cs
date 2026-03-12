using Bookmark_App.Models;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Bookmark_App.CloudSync
{
    public static class ManifestService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
        {
            const int attempts = 200;   // 200 * 50ms = 10 seconds
            const int delayMs = 50;

            for (int i = 1; i <= attempts; i++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await using var stream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete,
                        bufferSize: 1024 * 1024,
                        useAsync: true);

                    using var sha = System.Security.Cryptography.SHA256.Create();
#if NET8_0_OR_GREATER
                    var hash = await sha.ComputeHashAsync(stream, ct);
#else
            var hash = sha.ComputeHash(stream);
#endif
                    return Convert.ToHexString(hash);
                }
                catch (IOException) when (i < attempts)
                {
                    await Task.Delay(delayMs, ct);
                }
            }

            throw new IOException($"Could not open snapshot for hashing after {attempts} attempts: {filePath}");
        }

        public static async Task<SyncManifest> CreateManifestForSnapshotAsync(
            string snapshotFilePath,
            string deviceId,
            int schemaVersion,
            string appVersion,
            CancellationToken ct = default)
        {
            if (!File.Exists(snapshotFilePath))
                throw new FileNotFoundException("Snapshot file not found.", snapshotFilePath);

            var hash = await ComputeSha256Async(snapshotFilePath, ct);

            return new SyncManifest
            {
                ManifestVersion = 1,
                SnapshotSha256 = hash,
                SnapshotCreatedUtc = DateTime.UtcNow,
                DeviceId = deviceId,
                DeviceName = Environment.MachineName,
                SchemaVersion = schemaVersion,
                AppVersion = appVersion
            };
        }

        public static async Task WriteManifestAsync(string manifestPath, SyncManifest manifest, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(manifestPath, json, Encoding.UTF8, ct);
        }

        public static async Task<SyncManifest> ReadManifestAsync(string manifestPath, CancellationToken ct = default)
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = JsonSerializer.Deserialize<SyncManifest>(json);

            if (manifest == null)
                throw new InvalidDataException("Manifest JSON could not be deserialized.");

            return manifest;
        }
    }
}

