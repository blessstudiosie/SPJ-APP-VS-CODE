using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPJ_APP.Service
{
    public class SyncStatusEventArgs : EventArgs
    {
        public string Message { get; }
        public bool IsSyncing { get; }

        public SyncStatusEventArgs(string message, bool isSyncing)
        {
            Message = message;
            IsSyncing = isSyncing;
        }
    }

    public class BackgroundSyncService
    {
        private static readonly Lazy<BackgroundSyncService> _instance = new(() => new BackgroundSyncService());
        public static BackgroundSyncService Instance => _instance.Value;

        private Timer _timer;
        private bool _isSyncing = false;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);

        public event EventHandler<SyncStatusEventArgs> SyncStatusChanged;

        private BackgroundSyncService() { }

        public void Start()
        {
            // Ensure the timer is only started once.
            if (_timer == null)
            {
                _timer = new Timer(async (state) => await DoSyncAsync(), null, TimeSpan.FromMinutes(1), _syncInterval);
            }
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();
        }

        private async Task DoSyncAsync()
        {
            if (_isSyncing)
            {
                return; // Jangan lakukan apa-apa jika sudah berjalan
            }

            _isSyncing = true;
            OnSyncStatusChanged(new SyncStatusEventArgs("Sinkronisasi otomatis...", true));

            try
            {
                var (summary, _) = await SyncService.SyncAllAsync(); // Ignore conflicts in background sync
                OnSyncStatusChanged(new SyncStatusEventArgs(summary.ToDisplayText(), false));
            }
            catch (Exception ex)
            {
                OnSyncStatusChanged(new SyncStatusEventArgs($"Error sinkronisasi: {ex.Message}", false));
            }
            finally
            {
                _isSyncing = false;
            }
        }

        protected virtual void OnSyncStatusChanged(SyncStatusEventArgs e)
        {
            SyncStatusChanged?.Invoke(this, e);
        }
    }
}
