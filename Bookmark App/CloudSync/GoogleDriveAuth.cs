using Bookmark_App.DataAccess;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;

namespace Bookmark_App.CloudSync
{
    public static class GoogleDriveAuth
    {
        // Least-privilege scope: only files your app creates/opens.
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private const string AppName = "Bookmark App";
        private const string ClientSecretsFileName = "client_secret.json";

        /// <summary>
        /// Token directory next to the exe: ./_tokens/
        /// WARNING: If your app is installed under Program Files, this may fail due to permissions.
        /// In that case, switch to AppData (recommended).
        /// </summary>
        public static string TokenDirNextToExe =>
            Path.Combine(DbConfig.AppDataRoot, "_tokens");

        public static bool TokenDirHasTokens()
        {
            var dir = TokenDirNextToExe;
            return Directory.Exists(dir) && Directory.EnumerateFiles(dir).Any();
        }

        public static void SignOutLocal()
        {
            var dir = TokenDirNextToExe;
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }

        public static async Task<DriveService> CreateDriveServiceAsync(CancellationToken ct = default)
        {
            var secretsPath = Path.Combine(AppContext.BaseDirectory, ClientSecretsFileName);
            if (!File.Exists(secretsPath))
                throw new FileNotFoundException($"Missing {ClientSecretsFileName} next to the exe.", secretsPath);

            Directory.CreateDirectory(TokenDirNextToExe);

            await using var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // userKey can be constant for single-account support.
            // If you ever want multiple accounts, make this different per account.
            var userKey = "default-user";

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                userKey,
                ct,
                new FileDataStore(TokenDirNextToExe, true) // stores tokens here
            );

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName
            });
        }
    }
}
