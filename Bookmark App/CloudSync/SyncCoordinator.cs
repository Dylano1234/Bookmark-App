using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.CloudSync
{
    public static class SyncCoordinator
    {
        public static bool AutoSyncEnabled { get; set; } = true;

        // Assigned once by app after login+setup
        public static Action? NotifyDbChanged { get; set; }

        // For manual sync
        public static Func<System.Threading.CancellationToken, System.Threading.Tasks.Task>? SyncNowAsync { get; set; }
    }
}
