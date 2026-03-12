using Bookmark_App.CloudSync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Bookmark_App.ViewModels
{
    public partial class ExitSyncViewModel : ObservableObject
    {
        public MainViewModel MainViewModel { get; }

        private CancellationTokenSource? _cts;
        public ICommand CancelCommand { get; }
        public ICommand ExitNowCommand { get; }

        public ExitSyncViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            CancelCommand = new RelayCommand(Cancel);
            ExitNowCommand = new RelayCommand(ExitNow);
        }

        public async void Start()
        {
            // Prevent double-starts
            if (_cts != null) return;

            // Create CTS with timeout 
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            try
            {
                // Run upload
                await SyncCoordinator.SyncNowAsync!.Invoke(_cts.Token);

                // Success → close app
                MainViewModel.IsExitSyncViewOpen = false;
                MainViewModel.RequestShutdown();
            }
            catch (OperationCanceledException)
            {
                // Cancel or timeout → just close overlay and keep app open
                MainViewModel.IsExitSyncViewOpen = false;
            }
            catch (Exception ex)
            {
                // Error → close overlay and keep app open (or show message)
                MainViewModel.IsExitSyncViewOpen = false;
                // TODO: store error message for UI
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        private void Cancel()
        {
            // Abort upload and return to app
            _cts?.Cancel();
            MainViewModel.IsExitSyncViewOpen = false;
        }
        private void ExitNow()
        {
            // Abort upload and exit immediately
            _cts?.Cancel();
            MainViewModel.IsExitSyncViewOpen = false;
            MainViewModel.RequestShutdown();
        }
    }
}
