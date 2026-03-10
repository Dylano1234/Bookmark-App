using Bookmark_App.Models;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Bookmark_App.CloudSync
{
    public sealed class GoogleDriveSyncProvider
    {
        private readonly DriveService _drive;
        private static readonly SemaphoreSlim _syncMutex = new(1, 1);

        // Folder name on Drive (visible). Change if you want.
        private const string FolderName = "Bookmark App Sync";

        public GoogleDriveSyncProvider(DriveService drive)
        {
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        }

        /// <summary>
        /// Ensures a Drive folder exists and returns its folderId.
        /// Uses SyncStateStore to persist the folderId.
        /// </summary>
        public async Task<string> EnsureSyncFolderAsync(CancellationToken ct = default)
        {
            var state = SyncStateStore.LoadOrCreate();

            if (!string.IsNullOrWhiteSpace(state.DriveFolderId))
            {
                // Optional: could verify it exists. For personal use, usually not needed.
                return state.DriveFolderId!;
            }

            // Try find existing folder by name
            var list = _drive.Files.List();
            list.Q = $"mimeType='application/vnd.google-apps.folder' and name='{FolderName}' and trashed=false";
            list.Fields = "files(id,name)";
            var found = await list.ExecuteAsync(ct);

            var folder = found.Files?.FirstOrDefault();
            if (folder != null)
            {
                state.DriveFolderId = folder.Id;
                SyncStateStore.Save(state);
                return folder.Id;
            }

            // Create new folder
            var folderMeta = new DriveFile
            {
                Name = FolderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var create = _drive.Files.Create(folderMeta);
            create.Fields = "id";
            var created = await create.ExecuteAsync(ct);

            state.DriveFolderId = created.Id;
            SyncStateStore.Save(state);
            return created.Id;
        }

        public async Task UploadSnapshotAndManifestAsync(
            string localSnapshotPath,
            string localManifestPath,
            CancellationToken ct = default)
        {

            await _syncMutex.WaitAsync(ct);
            if (!File.Exists(localSnapshotPath))
                throw new FileNotFoundException("Snapshot file not found.", localSnapshotPath);
            if (!File.Exists(localManifestPath))
                throw new FileNotFoundException("Manifest file not found.", localManifestPath);

            var folderId = await EnsureSyncFolderAsync(ct);
            var state = SyncStateStore.LoadOrCreate();
            try
            {
                // Upload/update snapshot
                state.DriveSnapshotFileId = await UpsertFileAsync(
                    folderId,
                    driveFileId: state.DriveSnapshotFileId,
                    desiredName: Path.GetFileName(localSnapshotPath),
                    mimeType: "application/octet-stream",
                    localPath: localSnapshotPath,
                    ct: ct);

                // Upload/update manifest
                state.DriveManifestFileId = await UpsertFileAsync(
                    folderId,
                    driveFileId: state.DriveManifestFileId,
                    desiredName: Path.GetFileName(localManifestPath),
                    mimeType: "application/json",
                    localPath: localManifestPath,
                    ct: ct);
                state.IsLocalDirty = false;
                SyncStateStore.Save(state);
                SyncStateManager.Reload();
            }
            catch (TaskCanceledException)
            {
                // Not an error: user cancelled or timeout.
                throw; // IMPORTANT: let ExitSyncViewModel catch it
            }
            catch (Exception ex) when (ct.IsCancellationRequested)
            {
                // Cancellation was requested, but the stack surfaced as HttpRequestException/IOException/etc.
                throw new OperationCanceledException("Cancelled", ex, ct);
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                MessageBox.Show(
                    "Cloud sync failed. Please check your internet connection.",
                    "Cloud sync Failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                "Cloud sync failed: " + ex.Message,
                "Cloud sync Failure",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
            }
            finally { _syncMutex.Release(); }

        }

        /// <summary>
        /// Downloads manifest only (small) to destination path.
        /// Returns the Drive file metadata (incl. modifiedTime) if available.
        /// </summary>
        public async Task<DriveFile?> DownloadManifestAsync(string destinationPath, CancellationToken ct = default)
        {
            var state = SyncStateStore.LoadOrCreate();
            if (string.IsNullOrWhiteSpace(state.DriveManifestFileId))
                return null;

            return await DownloadFileByIdAsync(state.DriveManifestFileId!, destinationPath, ct);
        }

        /// <summary>
        /// Downloads snapshot (big) to destination path.
        /// Returns the Drive file metadata (incl. modifiedTime) if available.
        /// </summary>
        public async Task<DriveFile?> DownloadSnapshotAsync(string destinationPath, CancellationToken ct = default)
        {
            var state = SyncStateStore.LoadOrCreate();
            if (string.IsNullOrWhiteSpace(state.DriveSnapshotFileId))
                return null;

            return await DownloadFileByIdAsync(state.DriveSnapshotFileId!, destinationPath, ct);
        }

        // ---------- Helpers ----------
        private static async Task<FileStream> OpenReadWithRetryAsync(string path, CancellationToken ct)
        {
            const int attempts = 200;   // ~10s total
            const int delayMs = 50;

            for (int i = 1; i <= attempts; i++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete,
                        bufferSize: 1024 * 1024,
                        useAsync: true);
                }
                catch (IOException) when (i < attempts)
                {
                    await Task.Delay(delayMs, ct);
                }
            }

            throw new IOException($"Could not open file for upload after {attempts} attempts: {path}");
        }

        private async Task<string> UpsertFileAsync(
            string folderId,
            string? driveFileId,
            string desiredName,
            string mimeType,
            string localPath,
            CancellationToken ct)
        {
            try
            {
                // If we don't have an id yet, try to find by name in folder (helps when state is lost)
                if (string.IsNullOrWhiteSpace(driveFileId))
                {
                    driveFileId = await TryFindFileIdInFolderAsync(folderId, desiredName, ct);
                }

                await using var stream = await OpenReadWithRetryAsync(localPath, ct);

                if (string.IsNullOrWhiteSpace(driveFileId))
                {
                    var meta = new DriveFile
                    {
                        Name = desiredName,
                        Parents = new[] { folderId }
                    };

                    var create = _drive.Files.Create(meta, stream, mimeType);
                    create.Fields = "id";

                    var progress = await create.UploadAsync(ct).ConfigureAwait(false);
                    if (progress.Status != UploadStatus.Completed)
                    {
                        // If cancellation was requested, propagate cancellation instead of an error
                        if (ct.IsCancellationRequested)
                            throw new OperationCanceledException(ct);

                        throw new IOException($"Drive create failed: {progress.Status}", progress.Exception);
                    }

                    var id = create.ResponseBody?.Id;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        // If cancellation was requested, propagate cancellation instead of an error
                        if (ct.IsCancellationRequested)
                            throw new OperationCanceledException(ct);

                        throw new InvalidOperationException("Drive create returned no file id.");
                    }

                    return id;
                }
                else
                {
                    var meta = new DriveFile
                    {
                        Name = desiredName
                    };

                    var update = _drive.Files.Update(meta, driveFileId, stream, mimeType);
                    update.Fields = "id";

                    var progress = await update.UploadAsync(ct).ConfigureAwait(false);
                    if (progress.Status != UploadStatus.Completed)
                    {
                        // If cancellation was requested, propagate cancellation instead of an error
                        if (ct.IsCancellationRequested)
                            throw new OperationCanceledException(ct);

                        throw new IOException($"Drive update failed: {progress.Status}", progress.Exception);
                    }

                    // Prefer the response body id if present (should match), otherwise return the known id
                    return update.ResponseBody?.Id ?? driveFileId;
                }
            }
            catch (OperationCanceledException)
            {
                // IMPORTANT: never wrap cancellation
                throw;
            }
            catch (Exception ex) when (ct.IsCancellationRequested)
            {
                // Cancellation happened but bubbled as Http/IO/etc.
                throw new OperationCanceledException("Cancelled", ex, ct);
            }
            catch (Exception ex)
            {
                // Real failure
                throw new IOException("Drive update failed.", ex);
            }
        }

        private async Task<string?> TryFindFileIdInFolderAsync(string folderId, string fileName, CancellationToken ct)
        {
            var list = _drive.Files.List();
            list.Q = $"'{folderId}' in parents and name='{fileName}' and trashed=false";
            list.Fields = "files(id,name)";
            var result = await list.ExecuteAsync(ct);
            return result.Files?.FirstOrDefault()?.Id;
        }

        private async Task<DriveFile?> DownloadFileByIdAsync(string fileId, string destinationPath, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            // Fetch metadata (nice for modifiedTime display)
            var metaReq = _drive.Files.Get(fileId);
            metaReq.Fields = "id,name,modifiedTime,size";
            var meta = await metaReq.ExecuteAsync(ct);

            // Download content
            var getReq = _drive.Files.Get(fileId);
            await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await getReq.DownloadAsync(fs, ct);

            return meta;
        }
        private static bool IsNetworkError(Exception ex)
        {
            return ex is HttpRequestException
                || ex is TaskCanceledException
                || ex is TimeoutException
                || ex is IOException
                || (ex.InnerException != null && IsNetworkError(ex.InnerException));
        }
    }
}
