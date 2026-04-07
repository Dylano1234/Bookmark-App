using Bookmark_App.CloudSync;
using Bookmark_App.DataAccess;
using Bookmark_App.Models;
using Bookmark_App.Services;
using Bookmark_App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Apis.Drive.v3;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject currentViewModel;

        private readonly ListService _listService;
        public DriveService? _drive;
        private SyncDebounceScheduler? _syncScheduler;

        public ListCreationViewModel CreateListViewModel { get; }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        private bool _isCreateListOpen;
        public bool IsCreateListOpen
        {
            get => _isCreateListOpen;
            set => SetProperty(ref _isCreateListOpen, value);
        }
        private bool _isListItemDetailViewOpen;
        public bool IsListItemDetailViewOpen
        {
            get => _isListItemDetailViewOpen;
            set => SetProperty(ref _isListItemDetailViewOpen, value);
        }
        private bool _isSyncChoiceViewOpen;
        public bool IsSyncChoiceViewOpen
        {
            get => _isSyncChoiceViewOpen;
            set => SetProperty(ref _isSyncChoiceViewOpen, value);
        }

        private string _syncStatusText;
        public string SyncStatusText
        {
            get => _syncStatusText;
            set => SetProperty(ref _syncStatusText, value);
        }
        private Brush _syncStatusColor;
        public Brush SyncStatusColor
        {
            get => _syncStatusColor;
            set => SetProperty(ref _syncStatusColor, value);
        }
        private bool _isExitSyncViewOpen;
        public bool IsExitSyncViewOpen
        {
            get => _isExitSyncViewOpen;
            set => SetProperty(ref _isExitSyncViewOpen, value);
        }
        public ListItemDetailViewModel? ListItemDetailViewModel { get; set; }
        public SyncChoiceViewModel? SyncChoiceViewModel { get; set; }
        public ExitSyncViewModel? ExitSyncViewModel { get; set; }

        public ICommand OpenCreateListCommand { get; }
        public ICommand CloseCreateListCommand { get; }
        public ICommand OpenListItemDetailViewNewCommand { get; }
        public ICommand OpenListItemDetailViewEditCommand { get; }
        public ICommand OpenListItemDetailViewCommand { get; }
        public ICommand CloseListItemDetailViewCommand { get; }
        public ICommand OpenListCommand { get; }
        public ICommand OpenHomeCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand OpenEditListCommand { get; }
        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand SignInCommand { get; }
        public IAsyncRelayCommand SignOutCommand { get; }
        public ICommand OpenSyncChoiceCommand { get; }
        public ICommand CloseSyncChoiceCommand { get; }
        public ICommand SyncNowCommand { get; }
        public ICommand OpenExitSyncViewCommand { get; }
        public ICommand CloseExitSyncViewCommand { get; }

        public ObservableCollection<List> Lists { get; } = new();

        public MainViewModel()
        {
            ListService listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            _listService = listService;
            LoadLists();

            CreateListViewModel = new ListCreationViewModel(this);
            ListItemDetailViewModel = new ListItemDetailViewModel(this);
            SyncChoiceViewModel = new SyncChoiceViewModel(this);
            ExitSyncViewModel = new ExitSyncViewModel(this);

            OpenCreateListCommand = new RelayCommand(OpenCreateList);
            CloseCreateListCommand = new RelayCommand(CloseCreateList);
            OpenListCommand = new RelayCommand<List>(OpenList);
            OpenHomeCommand = new RelayCommand(OpenHome);
            OpenUrlCommand = new RelayCommand<string>(OpenUrl);
            OpenListItemDetailViewNewCommand = new RelayCommand(OpenListItemDetailViewNew);
            OpenListItemDetailViewEditCommand = new RelayCommand(OpenListItemDetailViewEdit);
            OpenListItemDetailViewCommand = new RelayCommand<ListItem>(OpenListItemDetailView);
            CloseListItemDetailViewCommand = new RelayCommand(CloseListItemDetailView);
            OpenEditListCommand = new RelayCommand<List>(OpenEditList);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync);
            SignInCommand = new AsyncRelayCommand(SignInAsync);
            SignOutCommand = new AsyncRelayCommand(SignOutAsync);
            OpenSyncChoiceCommand = new RelayCommand(OpenSyncChoice);
            CloseSyncChoiceCommand = new RelayCommand(CloseSyncChoice);
            SyncNowCommand = new RelayCommand(SyncNow);
            OpenExitSyncViewCommand = new RelayCommand(OpenExitSyncView);
            CloseExitSyncViewCommand = new RelayCommand(CloseExitSyncView);

            IsLoggedIn = GoogleDriveAuth.TokenDirHasTokens();

            SyncStateManager.Changed += () =>
            {
                Application.Current.Dispatcher.Invoke(UpdateSyncIndicator);
            };

            UpdateSyncIndicator();

            // Start op Home

            CurrentViewModel = new HomeViewModel(listService);
        }

        public void LoadLists()
        {
            Lists.Clear();
            foreach (var l in _listService.GetAllLists())
                Lists.Add(l);
        }
        private void OpenCreateList()
        {
            CreateListViewModel.IsNewList = true;
            CreateListViewModel.WindowTitle = "Create New List";
            CreateListViewModel.CurrentList = null;
            IsCreateListOpen = true;
        }

        public void CloseCreateList()
        {
            IsCreateListOpen = false;
            if (currentViewModel is HomeViewModel homeVM)
            {
                homeVM.LoadLists();
            }
            CreateListViewModel.CurrentList = null;
            CreateListViewModel.CoverImageData = null;
            CreateListViewModel.CoverPreview = null;
            CreateListViewModel.ListTitle = string.Empty;
        }
        private void OpenList(List list)
        {
            if (list == null) return;
            CurrentViewModel = new ListViewModel(list);
        }
        private void OpenHome()
        {
            CurrentViewModel = new HomeViewModel(_listService);
        }
        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }
        private void OpenListItemDetailViewNew()
        {
            IsListItemDetailViewOpen = true;
            ListItemDetailViewModel.HeaderTitle = "Create New Item";
            ListItemDetailViewModel.IsNewItem = true;
            ListItemDetailViewModel.IsEditMode = true;
            ListItemDetailViewModel.CurrentListItem = new ListItem();
            if (currentViewModel is ListViewModel listVM)
            {
                ListItemDetailViewModel.CurrentListid = listVM._list.id;
            }

            var byId = ListItemDetailViewModel.Genres.ToDictionary(g => g.id);
            var noneGenre = ListItemDetailViewModel.Genres.FirstOrDefault(g => g.id == -1) ?? new Genre { id = -1, name = "None" };
            var itemGenres = ListItemDetailViewModel.CurrentListItem.genres;
            // Assign genres using helper
            AssignGenreProperty(1, itemGenres, byId, noneGenre);
            AssignGenreProperty(2, itemGenres, byId, noneGenre);
            AssignGenreProperty(3, itemGenres, byId, noneGenre);
            AssignGenreProperty(4, itemGenres, byId, noneGenre);
            AssignGenreProperty(5, itemGenres, byId, noneGenre);
            AssignGenreProperty(6, itemGenres, byId, noneGenre);
        }
        private void OpenListItemDetailViewEdit()
        {
            IsListItemDetailViewOpen = true;
            ListItemDetailViewModel.HeaderTitle = "Edit Item";
            ListItemDetailViewModel.IsEditMode = true;
            ListItemDetailViewModel.IsNewItem = false;
            
            if (currentViewModel is ListViewModel listVM)
            {
                ListItemDetailViewModel.CurrentListid = listVM._list.id;
            }

            var byId = ListItemDetailViewModel.Genres.ToDictionary(g => g.id);
            var noneGenre = ListItemDetailViewModel.Genres.FirstOrDefault(g => g.id == -1) ?? new Genre { id = -1, name = "None" };
            var itemGenres = ListItemDetailViewModel.CurrentListItem.genres;

            // Assign genres using helper
            AssignGenreProperty(1, itemGenres, byId, noneGenre);
            AssignGenreProperty(2, itemGenres, byId, noneGenre);
            AssignGenreProperty(3, itemGenres, byId, noneGenre);
            AssignGenreProperty(4, itemGenres, byId, noneGenre);
            AssignGenreProperty(5, itemGenres, byId, noneGenre);
            AssignGenreProperty(6, itemGenres, byId, noneGenre);
        }

        private void AssignGenreProperty(int index, ObservableCollection<Genre> itemGenres, Dictionary<int, Genre> byId, Genre noneGenre)
        {
            var genre = itemGenres.Count > index - 1 && byId.TryGetValue(itemGenres[index - 1].id, out var g) 
                ? g 
                : noneGenre;

            switch (index)
            {
                case 1: ListItemDetailViewModel.Genre1 = genre; break;
                case 2: ListItemDetailViewModel.Genre2 = genre; break;
                case 3: ListItemDetailViewModel.Genre3 = genre; break;
                case 4: ListItemDetailViewModel.Genre4 = genre; break;
                case 5: ListItemDetailViewModel.Genre5 = genre; break;
                case 6: ListItemDetailViewModel.Genre6 = genre; break;
            }
        }
        private void OpenListItemDetailView(ListItem selectedListItem)
        {
            IsListItemDetailViewOpen = true;
            ListItemDetailViewModel.HeaderTitle = "Item Details";
            ListItemDetailViewModel.IsEditMode = false;
            ListItemDetailViewModel.IsNewItem = false;
            ListItemDetailViewModel.CurrentListItem = selectedListItem;
            if(currentViewModel is ListViewModel listVM)
            {
                ListItemDetailViewModel.CurrentListid = listVM._list.id;
            }

        }
        public void CloseListItemDetailView()
        {
            IsListItemDetailViewOpen = false;
            if (currentViewModel is ListViewModel listVM)
            {
                listVM.LoadItems(listVM._list, listVM.SelectedGenreSortOption, listVM.SelectedSortingOption, listVM.FilteringTitle, listVM.Status, listVM.ItemsPerPage, (int)listVM.CurrentPage);
            }
            ListItemDetailViewModel.ResetGenres();
        }
        private void OpenEditList(Models.List currentList)
        {
            CreateListViewModel.IsNewList = false;
            CreateListViewModel.WindowTitle = "Edit List";
            CreateListViewModel.CurrentList = currentList;
            CreateListViewModel.ListTitle = currentList.title;
            CreateListViewModel.CoverImageData = currentList.coverImage;
            if (currentList.coverImage != null)
            {
                CreateListViewModel.CoverPreview = currentList.coverImage;
            }
            IsCreateListOpen = true;
        }
        private async Task InitializeAsync()
        {
            // Don't trigger OAuth if user never signed in before
            if (!GoogleDriveAuth.TokenDirHasTokens())
            {
                IsLoggedIn = false;
                return;
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                _drive = await GoogleDriveAuth.CreateDriveServiceAsync(cts.Token);
                SetupSyncScheduler(_drive);

                IsLoggedIn = true;
            }
            catch (OperationCanceledException)
            {
                IsLoggedIn = false;
            }
            catch (Exception)
            {
                // Tokens exist but aren't valid anymore (revoked, etc.)
                IsLoggedIn = false;
            }
            if (IsLoggedIn)
            {
                await CheckSyncConfilct();
            }
        }
        private async Task SignInAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                _drive = await GoogleDriveAuth.CreateDriveServiceAsync(cts.Token);

                SetupSyncScheduler(_drive);
                MessageBox.Show(
                    "Successfully logged in.",
                    "Succesful Login",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                IsLoggedIn = true;

                if (IsLoggedIn)
                {
                    await CheckSyncConfilct();
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    "Login timed out or was cancelled. Close the browser window and try again.",
                    "Login cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed Login",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        private async Task SignOutAsync()
        {
            try
            {
                GoogleDriveAuth.SignOutLocal();
                IsLoggedIn = false;

                SyncCoordinator.NotifyDbChanged = null;
                SyncCoordinator.SyncNowAsync = null;
                _syncScheduler?.Dispose();
                _syncScheduler = null;
                SyncStateManager.Current.IsAutoSyncEnabled = false;
                SyncStateManager.Save();

                MessageBox.Show(
                    "Successfully logged out.",
                    "Succesful Logout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed Logout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SetupSyncScheduler(DriveService drive)
        {
            var cloudSync = new CloudSyncService(drive);

            _syncScheduler = new SyncDebounceScheduler(
                syncActionAsync: cloudSync.UploadCurrentDbAsync,
                debounceDelay: TimeSpan.FromSeconds(20),
                maxWait: TimeSpan.FromMinutes(2));


            SyncCoordinator.NotifyDbChanged = _syncScheduler.NotifyDbChanged;
            SyncCoordinator.SyncNowAsync = _syncScheduler.SyncNowAsync;
        }
        private void OpenSyncChoice()
        {
            IsSyncChoiceViewOpen = true;
        }
        private void CloseSyncChoice()
        {
            IsSyncChoiceViewOpen = false;
        }
        private void SyncNow()
        {
            var result = MessageBox.Show(
                    "Are you sure you want to upload to the cloud?\nIf you do, the data stored on the cloud will be overwritten and gone forever.",
                    "Keep local data",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            SyncCoordinator.SyncNowAsync?.Invoke(CancellationToken.None);
            SyncCoordinator.AutoSyncEnabled = true;
            SyncStateManager.Current.IsAutoSyncEnabled = true;
            SyncStateManager.Save();
        }
        private void OpenExitSyncView()
        {
            IsExitSyncViewOpen = true;
        }
        private void CloseExitSyncView()
        {
            IsExitSyncViewOpen = false;
        }
        private async Task CheckSyncConfilct()
        {
            try
            {
                var syncProvider = new GoogleDriveSyncProvider(_drive);

                await syncProvider.DownloadManifestAsync(DbConfig.IncomingManifestFilePath);

                var syncManifest = await ManifestService.ReadManifestAsync(
                    DbConfig.IncomingManifestFilePath);

                if (syncManifest == null) return;

                var state = SyncStateStore.LoadOrCreate();

                if (!string.Equals(syncManifest.SnapshotSha256, state.LastSyncedSnapshotSha256, StringComparison.OrdinalIgnoreCase) && state.IsLocalDirty) // Local has unsynced changes and cloud has different data than last sync -> conflict
                {
                    SyncChoiceViewModel.LocalLastSaved = state.LastSyncedUtc.HasValue
                                                        ? state.LastSyncedUtc.Value.ToLocalTime().ToString("g")
                                                        : "Never";
                    SyncChoiceViewModel.CloudLastSaved = syncManifest.SnapshotCreatedUtc.ToLocalTime().ToString("g");
                    SyncChoiceViewModel.CloudSaveDevice = $"{syncManifest.DeviceName} ({syncManifest.DeviceId.Substring(0, 8)})";
                    OpenSyncChoice();
                } 
                else if (string.Equals(syncManifest.SnapshotSha256, state.LastSyncedSnapshotSha256, StringComparison.OrdinalIgnoreCase) && state.IsLocalDirty) // Local has unsynced changes and cloud has no new data since last sync -> just upload and overwrite cloud
                { 
                    SyncCoordinator.SyncNowAsync?.Invoke(CancellationToken.None);
                    SyncCoordinator.AutoSyncEnabled = true;
                }
                else if (!string.Equals(syncManifest.SnapshotSha256, state.LastSyncedSnapshotSha256, StringComparison.OrdinalIgnoreCase) && !state.IsLocalDirty) // Cloud has new data and local has no unsynced changes -> just sync and overwrite local
                {
                    SyncCoordinator.AutoSyncEnabled = true;
                    SyncStateManager.Current.IsAutoSyncEnabled = true;
                    SyncStateManager.Save();

                    SyncStatusText = "Fetching new data";
                    SyncStatusColor = Brushes.Gold;

                    await syncProvider.DownloadSnapshotAsync(DbConfig.IncomingSnapshotFilePath);
                    await DatabaseSnapshotService.RestoreFromSnapshotAsync(DbConfig.IncomingSnapshotFilePath);
                    state = SyncStateStore.LoadOrCreate();
                    state.LastSyncedSnapshotSha256 = syncManifest.SnapshotSha256;
                    state.LastSyncedUtc = syncManifest.SnapshotCreatedUtc;
                    SyncStateStore.Save(state);
                    SyncStateManager.Reload();
                    LoadLists();
                    if (CurrentViewModel is HomeViewModel HomeVM)
                    {
                        HomeVM.LoadLists();
                    }
                    else if (CurrentViewModel is ListViewModel ListVM)
                    {
                        ListVM.LoadItems(ListVM._list, ListVM.SelectedGenreSortOption, ListVM.SelectedSortingOption, ListVM.FilteringTitle, ListVM.Status, ListVM.ItemsPerPage, ListVM.CurrentPage ?? 1);
                    }
                }
                else // No differences between cloud and local, just enable auto sync for future changes
                {
                    SyncCoordinator.AutoSyncEnabled = true;
                    SyncStateManager.Current.IsAutoSyncEnabled = true;
                    SyncStateManager.Save();
                }
            }
            catch (Exception ex)
            {
                // log/show a status, but don't hang startup
            }
        }
        private void UpdateSyncIndicator()
        {
            var state = SyncStateManager.Current;

            if (!state.IsAutoSyncEnabled)
            {
                SyncStatusText = "Sync paused";
                SyncStatusColor = Brushes.Orange;
            }
            else if (state.IsLocalDirty)
            {
                SyncStatusText = "Pending upload";
                SyncStatusColor = Brushes.Gold;
            }
            else
            {
                SyncStatusText = "Up to date";
                SyncStatusColor = Brushes.Green;
            }

            OnPropertyChanged(nameof(SyncStatusText));
            OnPropertyChanged(nameof(SyncStatusColor));
        }

        public void BeginExitSync()
        {
            // If sync is blocked (keep local / cloud ahead), just exit immediately
            var state = SyncStateManager.Current;
            if (!IsLoggedIn || !state.IsAutoSyncEnabled || !state.IsLocalDirty)
            {
                RequestShutdown();
                return;
            }

            IsExitSyncViewOpen = true;
            ExitSyncViewModel.Start(); // starts upload with timeout
        }

        public void RequestShutdown()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow w)
                    w.AllowCloseAndShutdown();
            });
        }
    }
}
