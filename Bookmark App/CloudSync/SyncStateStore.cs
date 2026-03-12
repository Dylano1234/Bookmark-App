using System.IO;
using System.Text;
using System.Text.Json;

namespace Bookmark_App.CloudSync
{
    public static class SyncStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static string StateFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sync_state.json");

        /// <summary>
        /// Loads sync state or creates a new one on first run.
        /// </summary>
        public static SyncState LoadOrCreate()
        {
            if (File.Exists(StateFilePath))
            {
                var json = File.ReadAllText(StateFilePath, Encoding.UTF8);
                var state = JsonSerializer.Deserialize<SyncState>(json);

                if (state != null && !string.IsNullOrWhiteSpace(state.DeviceId))
                    return state;
            }

            // First run / corrupted file
            var newState = new SyncState
            {
                DeviceId = Guid.NewGuid().ToString("N")
            };

            Save(newState);
            return newState;
        }

        public static void Save(SyncState state)
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(StateFilePath, json, Encoding.UTF8);
        }

        public static void Clear()
        {
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
        }
    }
}
