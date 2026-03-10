using Bookmark_App.CloudSync;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Bookmark_App.DataAccess
{
    public static class DatabaseSnapshotService
    {
        /// <summary>
        /// Creates a consistent snapshot of the live DB at DbConfig.DatabasePath
        /// and writes it to snapshotPath. Safe to use while the app is running.
        /// </summary>
        /// 
        public static async Task CreateSnapshotAsync(CancellationToken ct = default)
        {
            var snapshotPath = DbConfig.SnapshotPath;
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

            // If you keep failing here due to locks, move snapshot folder to %TEMP% (see below)
            if (File.Exists(snapshotPath))
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Delete(snapshotPath);
            }
                

            await using (var source = new SqliteConnection(DbConfig.ConnectionString))
            {
                await source.OpenAsync(ct);

                await using (var cmd = source.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                await using (var dest = new SqliteConnection($"Data Source={snapshotPath}"))
                {
                    await dest.OpenAsync(ct);

                    // Reduce extra files
                    await using (var cmd = dest.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA journal_mode=DELETE;";
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    source.BackupDatabase(dest);

                    await dest.CloseAsync();
                }

                await source.CloseAsync();
            }
        }

        /// <summary>
        /// Restores the live DB from a downloaded snapshot file.
        /// Writes into a temp DB first, then swaps atomically.
        /// </summary>
        public static async Task RestoreFromSnapshotAsync(string snapshotPath, CancellationToken ct = default)
        {
            const int attempts = 200;   // ~10s total
            const int delayMs = 50;

            if (string.IsNullOrWhiteSpace(snapshotPath))
                throw new ArgumentException("Snapshot path is required.", nameof(snapshotPath));

            if (!File.Exists(snapshotPath))
                throw new FileNotFoundException("Snapshot file not found.", snapshotPath);

            // Restore into a temp db file first
            var livePath = DbConfig.DatabasePath;
            var liveDir = Path.GetDirectoryName(livePath)!;
            Directory.CreateDirectory(liveDir);

            var tempRestoredPath = Path.Combine(
                liveDir,
                $"{Path.GetFileNameWithoutExtension(livePath)}.restoring{Path.GetExtension(livePath)}");

            // If previous restore crashed, clean up
            if (File.Exists(tempRestoredPath))
                File.Delete(tempRestoredPath);

            // Copy snapshot -> tempRestored via BackupDatabase (ensures valid SQLite file)
            await using (var source = new SqliteConnection($"Data Source={snapshotPath}"))
            {
                await source.OpenAsync(ct);

                await using var dest = new SqliteConnection($"Data Source={tempRestoredPath}");
                await dest.OpenAsync(ct);

                source.BackupDatabase(dest);
            }
            

            // Backup old live db in case something goes wrong
            var backupOldPath = Path.Combine(
                liveDir,
                $"{Path.GetFileNameWithoutExtension(livePath)}.old{Path.GetExtension(livePath)}");

            // Clean previous .old
            if (File.Exists(backupOldPath))
                File.Delete(backupOldPath);

            // Move live -> .old (if it exists)
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SqliteConnection.ClearAllPools();

            for (int i = 1; i <= attempts; i++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(livePath))
                        File.Move(livePath, backupOldPath);
                }
                catch (IOException) when (i < attempts)
                {
                    await Task.Delay(delayMs, ct);
                }
            }
            

            // Move temp restored -> live
            File.Move(tempRestoredPath, livePath);
            SyncStateManager.Current.IsLocalDirty = false;
            SyncStateManager.Save();
            // If you want, you can keep .old for recovery, or delete it:
            // File.Delete(backupOldPath);
        }
    }
}