namespace Bookmark_App.CloudSync
{
    public sealed class SyncDebounceScheduler : IDisposable
    {
        private readonly TimeSpan _debounceDelay;
        private readonly TimeSpan _maxWait;

        private readonly Func<CancellationToken, Task> _syncActionAsync;

        private readonly object _gate = new();

        private CancellationTokenSource? _debounceCts;
        private CancellationTokenSource? _maxWaitCts;

        private bool _maxWaitRunning;
        private bool _syncInProgress;
        private bool _pendingChangeWhileSyncing;

        private bool _disposed;

        public SyncDebounceScheduler(
            Func<CancellationToken, Task> syncActionAsync,
            TimeSpan? debounceDelay = null,
            TimeSpan? maxWait = null)
        {
            _syncActionAsync = syncActionAsync ?? throw new ArgumentNullException(nameof(syncActionAsync));
            _debounceDelay = debounceDelay ?? TimeSpan.FromSeconds(20);
            _maxWait = maxWait ?? TimeSpan.FromMinutes(2);
        }

        /// <summary>
        /// Call this after any successful INSERT/UPDATE/DELETE.
        /// Starts/Resets the 20s debounce timer.
        /// Starts the 2m max-wait timer if it wasn't already running.
        /// </summary>
        public void NotifyDbChanged()
        {
            ThrowIfDisposed();

            CancellationTokenSource debounceLocal;
            CancellationTokenSource? maxWaitLocalToStart = null;

            lock (_gate)
            {
                // If a sync is currently running, just remember that new changes happened.
                if (_syncInProgress)
                {
                    _pendingChangeWhileSyncing = true;
                }

                // Reset debounce timer
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = new CancellationTokenSource();
                debounceLocal = _debounceCts;

                // Start max-wait timer only once per "burst"
                if (!_maxWaitRunning)
                {
                    _maxWaitRunning = true;
                    _maxWaitCts?.Cancel();
                    _maxWaitCts?.Dispose();
                    _maxWaitCts = new CancellationTokenSource();
                    maxWaitLocalToStart = _maxWaitCts;
                }
            }

            // Fire-and-forget timers (they call SyncAsync when they win).
            _ = DebounceTimerAsync(debounceLocal.Token);

            if (maxWaitLocalToStart != null)
                _ = MaxWaitTimerAsync(maxWaitLocalToStart.Token);
        }

        /// <summary>Manual "Sync now". Cancels timers and runs sync immediately.</summary>
        public Task SyncNowAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            CancelTimers();
            return TriggerSyncAsync(ct);
        }

        private async Task DebounceTimerAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(_debounceDelay, ct);
                await TriggerSyncAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) { }
        }

        private async Task MaxWaitTimerAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(_maxWait, ct);
                await TriggerSyncAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) { }
        }

        private async Task TriggerSyncAsync(CancellationToken externalCt)
        {
            // Ensure only one sync runs at a time.
            lock (_gate)
            {
                if (_syncInProgress)
                    return;

                _syncInProgress = true;
            }

            try
            {
                // Cancel timers for this burst
                CancelTimers();

                // Run actual sync (snapshot+manifest+upload)
                await _syncActionAsync(externalCt);
            }
            finally
            {
                bool runAgain = false;

                lock (_gate)
                {
                    _syncInProgress = false;

                    // If changes happened while syncing, schedule another burst.
                    if (_pendingChangeWhileSyncing)
                    {
                        _pendingChangeWhileSyncing = false;
                        runAgain = true;
                        _maxWaitRunning = false; // allow a new max-wait window
                    }
                }

                if (runAgain)
                {
                    // Start new burst immediately with a normal Notify call
                    NotifyDbChanged();
                }
                else
                {
                    // Burst completed
                    lock (_gate)
                    {
                        _maxWaitRunning = false;
                    }
                }
            }
        }

        private void CancelTimers()
        {
            lock (_gate)
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = null;

                _maxWaitCts?.Cancel();
                _maxWaitCts?.Dispose();
                _maxWaitCts = null;

                _maxWaitRunning = false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SyncDebounceScheduler));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelTimers();
        }
    }
}
