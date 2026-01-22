using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private BaseViewModel currentViewModel;

        private readonly ListService _listService;

        public ListCreationViewModel CreateListViewModel { get; }

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
        public ListItemDetailViewModel? ListItemDetailViewModel { get; set; }

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

        public ObservableCollection<List> Lists { get; } = new();

        public MainViewModel()
        {
            ListService listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            _listService = listService;
            LoadLists();

            CreateListViewModel = new ListCreationViewModel(this);
            ListItemDetailViewModel = new ListItemDetailViewModel(this);

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

            ListItemDetailViewModel.Genre1 = null;
            ListItemDetailViewModel.Genre2 = null;
            ListItemDetailViewModel.Genre3 = null;
            ListItemDetailViewModel.Genre4 = null;

            var itemGenres = ListItemDetailViewModel.CurrentListItem.genres;

            if (itemGenres.Count > 0 && byId.TryGetValue(itemGenres[0].id, out var g1)) ListItemDetailViewModel.Genre1 = ListItemDetailViewModel.Genres[ListItemDetailViewModel.CurrentListItem.genres[0].id - 1];
            if (itemGenres.Count > 1 && byId.TryGetValue(itemGenres[1].id, out var g2)) ListItemDetailViewModel.Genre2 = ListItemDetailViewModel.Genres[ListItemDetailViewModel.CurrentListItem.genres[1].id - 1];
            if (itemGenres.Count > 2 && byId.TryGetValue(itemGenres[2].id, out var g3)) ListItemDetailViewModel.Genre3 = ListItemDetailViewModel.Genres[ListItemDetailViewModel.CurrentListItem.genres[2].id - 1];
            if (itemGenres.Count > 3 && byId.TryGetValue(itemGenres[3].id, out var g4)) ListItemDetailViewModel.Genre4 = ListItemDetailViewModel.Genres[ListItemDetailViewModel.CurrentListItem.genres[3].id - 1];
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
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(currentList.coverImage))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                }
                CreateListViewModel.CoverPreview = bmp;
            }
            IsCreateListOpen = true;
        }
    }
}
