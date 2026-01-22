using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Input;

namespace Bookmark_App.ViewModels
{
    public partial class ListViewModel : BaseViewModel
    {
        public Models.List _list;
        public string Title => _list.title;
        private ItemStatus _status;
        public ObservableCollection<Genre> Genres { get; set; } = new ObservableCollection<Genre>();

        private Genre? _selectedGenreSortOption;
        public Genre? SelectedGenreSortOption
        {
            get => _selectedGenreSortOption;
            set
            {
                if (_selectedGenreSortOption == value) return;
                _selectedGenreSortOption = value;
                OnPropertyChanged(nameof(SelectedGenreSortOption));
                // trigger the command when selection changes
                if (SetSelectedGenreSortOptionCommand?.CanExecute(value) == true)
                    SetSelectedGenreSortOptionCommand.Execute(value);
            }
        }

        public ObservableCollection<string> SortingOptions { get; set; } = new ObservableCollection<string>();

        private string? _selectedSortingOption;
        public string? SelectedSortingOption
        {
            get => _selectedSortingOption;
            set
            {
                if (_selectedSortingOption == value) return;
                _selectedSortingOption = value;
                OnPropertyChanged(nameof(SelectedSortingOption));
                // trigger the command when selection changes
                if (SetSelectedSortingOptionCommand?.CanExecute(value) == true)
                    SetSelectedSortingOptionCommand.Execute(value);
            }
        }
        public string FilteringTitle { get; set; }

        public ItemStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        private int? _currentPage;
        public int? CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }
        private int? _totalPages;
        public int? TotalPages
        {
            get => _totalPages;
            set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged(nameof(TotalPages));
                }
            }
        }
        public int ItemsPerPage { get; set; } = 20;


        // Changed to ObservableCollection so UI updates when items change
        public ObservableCollection<Models.ListItem> Items { get; } = new ObservableCollection<Models.ListItem>();

        private readonly Services.ItemService _itemService = new Services.ItemService(new DataAccess.ItemRepository());
        private readonly Services.GenreService _genreService = new Services.GenreService(new DataAccess.GenreRepository());

        public ICommand SetStatusCommand { get; }
        public ICommand SetSelectedGenreSortOptionCommand { get; }
        public ICommand SetSelectedSortingOptionCommand { get; }
        public ICommand SetFilteringTitleCommand { get; }
        public ICommand OpenCreateListItemCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand IncrementProgressCommand { get; }

        public ListViewModel(Models.List list)
        {
            CurrentPage = 1;
            _list = list;
            SetStatusCommand = new RelayCommand<ItemStatus>(SetStatus);
            OpenCreateListItemCommand = new RelayCommand(OpenCreateListItem);
            SetSelectedGenreSortOptionCommand = new RelayCommand<Genre>(SetSelectedGenreSortOption);
            SetSelectedSortingOptionCommand = new RelayCommand<string>(SetSelectedSortingOption);
            SetFilteringTitleCommand = new RelayCommand<string>(SetFilteringTitle);
            FirstPageCommand = new RelayCommand(FirstPage);
            PreviousPageCommand = new RelayCommand(PreviousPage);
            NextPageCommand = new RelayCommand(NextPage);
            LastPageCommand = new RelayCommand(LastPage);
            IncrementProgressCommand = new RelayCommand<Models.ListItem>(IncrementProgress);

            SortingOptions.Add("Title Ascending");
            SortingOptions.Add("Title Descending");
            SortingOptions.Add("Rating Ascending");
            SortingOptions.Add("Rating Descending");
            SortingOptions.Add("Progress Ascending");
            SortingOptions.Add("Progress Descending");
            SelectedSortingOption = "Title Ascending";

            Status = ItemStatus.All;


            LoadGenres();
            // initial load using current filter/sort values
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }

        private void SetStatus(ItemStatus status)
        {
            ResetCurrentPage();
            Status = status;
            // reload items with current filters
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }

        private void OpenCreateListItem()
        {
            // Implementation for opening the create list item view
        }

        public void LoadItems(List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status, int itemsPerPage, int currentPage)
        {
            Items.Clear();
            foreach (var l in _itemService.GetAllItemsByList(list, genreFilter, sort, titleSearch, status, itemsPerPage, currentPage))
                Items.Add(l);
            LoadPageNumbers();
        }

        private void LoadGenres()
        {
            Genres.Clear();
            Genres.Add(new Genre { id = -1, name = "All Genres" });
            foreach (var g in _genreService.GetAllGenres())
                Genres.Add(g);
            if (SelectedGenreSortOption == null)
            {
                SelectedGenreSortOption = Genres[0]; // Select "All Genres" by default
            }
        }

        private void SetSelectedGenreSortOption(Genre genre)
        {
            ResetCurrentPage();
            SelectedGenreSortOption = genre;
            // reload items with new genre filter
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }

        private void SetSelectedSortingOption(string sortingOption)
        {
            SelectedSortingOption = sortingOption;
            // reload items with new sorting
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }

        private void SetFilteringTitle(string title)
        {
            ResetCurrentPage();
            FilteringTitle = title;
            FilteringTitle = FilteringTitle.Trim();
            // reload items with new title search
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }
        private void FirstPage()
        {
            CurrentPage = 1;
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }
        private void PreviousPage()
        {
            if (CurrentPage == 1)
            {
                return;
            }
            CurrentPage--;
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }
        private void NextPage()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }
            CurrentPage++;
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }
        private void LastPage()
        {
            CurrentPage = TotalPages;
            LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
        }
        private void LoadPageNumbers()
        {
            TotalPages = _itemService.GetItemCount(_list, SelectedGenreSortOption, FilteringTitle, Status) / ItemsPerPage + 1;
        }
        private void ResetCurrentPage()
        {
            CurrentPage = 1;
        }
        private void IncrementProgress(Models.ListItem item)
        {
            if (item.progressCurrent < item.progressMax)
            {
                item.progressCurrent++;
                _itemService.UpdateItem(item);
                LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
            }
            else if (item.progressCurrent == item.progressMax)
            {
                item.progressCurrent++;
                item.progressMax++;
                _itemService.UpdateItem(item);
                LoadItems(_list, SelectedGenreSortOption, SelectedSortingOption, FilteringTitle, Status, ItemsPerPage, (int)CurrentPage);
            }
        }
    }
}
