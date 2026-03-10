using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.CloudSync
{
    public static class SyncStateManager
    {
        public static SyncState Current { get; private set; } = SyncStateStore.LoadOrCreate();

        public static event Action? Changed;

        public static void Reload()
        {
            Current = SyncStateStore.LoadOrCreate();
            Changed?.Invoke();
        }

        public static void Save()
        {
            SyncStateStore.Save(Current);
            Changed?.Invoke();
        }
    }
}
