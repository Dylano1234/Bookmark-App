using Bookmark_App.CloudSync;
using Bookmark_App.DataAccess;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;

namespace Bookmark_App.ViewModels
{
    public partial class SyncChoiceViewModel : ObservableObject
    {
        public MainViewModel MainViewModel { get; }

        private string _LocalLastSaved;
        public string LocalLastSaved
        {
            get => _LocalLastSaved;
            set => SetProperty(ref _LocalLastSaved, value);
        }
        private string _CloudLastSaved;
        public string CloudLastSaved
        {
            get => _CloudLastSaved;
            set => SetProperty(ref _CloudLastSaved, value);
        }
        private string _CloudSaveDevice;
        public string CloudSaveDevice
        {
            get => _CloudSaveDevice;
            set => SetProperty(ref _CloudSaveDevice, value);
        }
        public ICommand KeepLocalCommand { get; }
        public IAsyncRelayCommand GetCloudCommand { get; }
        public SyncChoiceViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            KeepLocalCommand = new RelayCommand(KeepLocal);
            GetCloudCommand = new AsyncRelayCommand(GetCloud);
        }
        private void KeepLocal()
        {
            var result = MessageBox.Show(
                    "Are you sure you want to keep using this local data?\nDoing so will temporarily disable the automatic cloud sync until you upload your data manually. If you upload this local data to the cloud, the data stored on the cloud will be gone forever.",
                    "Keep local data",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            SyncCoordinator.AutoSyncEnabled = false;
            SyncStateManager.Current.IsAutoSyncEnabled = false;
            SyncStateManager.Save();
            MainViewModel.IsSyncChoiceViewOpen = false;

        }
        private async Task GetCloud()
        {
            var result = MessageBox.Show(
                    "Are you sure you want to overwrite your local data?\nYour local data will be gone forever.",
                    "Download cloud data",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            SyncCoordinator.AutoSyncEnabled = true;
            SyncStateManager.Current.IsAutoSyncEnabled = true;
            SyncStateManager.Save();

            GoogleDriveSyncProvider syncProvider = new GoogleDriveSyncProvider(MainViewModel._drive);
            await syncProvider.DownloadSnapshotAsync(DbConfig.IncomingSnapshotFilePath);
            await DatabaseSnapshotService.RestoreFromSnapshotAsync(DbConfig.IncomingSnapshotFilePath);
            var syncManifest = await ManifestService.ReadManifestAsync(
                   DbConfig.IncomingManifestFilePath);
            var state = SyncStateStore.LoadOrCreate();
            state.LastSyncedSnapshotSha256 = syncManifest.SnapshotSha256;
            state.LastSyncedUtc = syncManifest.SnapshotCreatedUtc;
            SyncStateStore.Save(state);
            SyncStateManager.Reload();
            MainViewModel.LoadLists();
            if (MainViewModel.CurrentViewModel is HomeViewModel HomeVM)
            {
                HomeVM.LoadLists();
            } 
            else if (MainViewModel.CurrentViewModel is ListViewModel ListVM)
            {
                ListVM.LoadItems(ListVM._list, ListVM.SelectedGenreSortOption, ListVM.SelectedSortingOption, ListVM.FilteringTitle, ListVM.Status, ListVM.ItemsPerPage, ListVM.CurrentPage ?? 1);
            }
            MainViewModel.IsSyncChoiceViewOpen = false;
        }
    }
}
