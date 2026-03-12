using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace Bookmark_App.ViewModels
{
    public partial class ListItemDetailViewModel : BaseViewModel
    {
        private bool _isNewItem;
        public bool IsNewItem
        {
            get => _isNewItem;
            set => SetProperty(ref _isNewItem, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string? _headerTitle;
        public string HeaderTitle
        {
            get => _headerTitle;
            set => SetProperty(ref _headerTitle, value);
        }

        private ListItem? _currentListItem;
        public ListItem? CurrentListItem
        {
            get => _currentListItem;
            set => SetProperty(ref _currentListItem, value);
        }
        private int? _currentListid;
        public int? CurrentListid
        {
            get => _currentListid;
            set => SetProperty(ref _currentListid, value);
        }
        public byte[]? CoverImageData { get; private set; }
        public ObservableCollection<ItemStatus> Statuses { get; } = new ObservableCollection<ItemStatus>
        {
            ItemStatus.InProgress,
            ItemStatus.Completed,
            ItemStatus.OnHold,
            ItemStatus.Dropped,
            ItemStatus.Planning
        };

        // Make these nullable and raise change notifications so the ComboBoxes update when set from code
        private Genre? _genre1;
        public Genre? Genre1
        {
            get => _genre1;
            set => SetProperty(ref _genre1, value);
        }

        private Genre? _genre2;
        public Genre? Genre2
        {
            get => _genre2;
            set => SetProperty(ref _genre2, value);
        }

        private Genre? _genre3;
        public Genre? Genre3
        {
            get => _genre3;
            set => SetProperty(ref _genre3, value);
        }

        private Genre? _genre4;
        public Genre? Genre4
        {
            get => _genre4;
            set => SetProperty(ref _genre4, value);
        }

        public ObservableCollection<Genre> Genres { get; set; } = new ObservableCollection<Genre>();
        private MainViewModel MainViewModel;
        public ICommand SaveListItemCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand DeleteListItemCommand { get; }
        public ICommand RemoveImageCommand { get; }

        private readonly Services.ItemService _itemService = new Services.ItemService(new DataAccess.ItemRepository());
        private readonly Services.GenreService _genreService = new Services.GenreService(new DataAccess.GenreRepository());
        public ListItemDetailViewModel(MainViewModel mainViewModel)
        {
            SaveListItemCommand = new RelayCommand(SaveListItem);
            SelectImageCommand = new RelayCommand(SelectImage);
            DeleteListItemCommand = new RelayCommand(DeleteListItem);
            RemoveImageCommand = new RelayCommand(RemoveImage);

            MainViewModel = mainViewModel;

            LoadGenres();
        }
        private void SaveListItem()
        {
            if (CurrentListItem == null) return;
            

            CurrentListItem.genres.Clear();
            if (Genre1 != null) 
            { 
                if (Genre1.id != -1)
                {
                    CurrentListItem.genres.Add(Genre1);
                }
            }
            if (Genre2 != null)
            {
                if (Genre2.id != -1)
                {
                    CurrentListItem.genres.Add(Genre2);
                }
            }
            if (Genre3 != null)
            {
                if (Genre3.id != -1)
                {
                    CurrentListItem.genres.Add(Genre3);
                }
            }
            if (Genre4 != null)
            {
                if (Genre4.id != -1)
                {
                    CurrentListItem.genres.Add(Genre4);
                }
            }

            if(ItemValidation())
            {
                return;
            }

            if (IsEditMode && !IsNewItem)
            {
                _itemService.UpdateItem(CurrentListItem);
                MainViewModel.CloseListItemDetailView();

            }
            else if (IsEditMode && IsNewItem)
            {
                _itemService.AddItem(CurrentListItem, (int)CurrentListid);
                MainViewModel.CloseListItemDetailView();
            }
            ResetGenres();
        }
        private void DeleteListItem()
        {
            if (!IsNewItem && CurrentListItem != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this item?",
                    "Delete Item",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                _itemService.DeleteItem(CurrentListItem);
                MainViewModel.CloseListItemDetailView();
            }
        }
        private void SelectImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Kies een omslagafbeelding",
                Filter = "Afbeeldingen|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dlg.FileName);
                CoverImageData = bytes;

                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                }

                if (CurrentListItem != null)
                    CurrentListItem.coverImage = CoverImageData;
            }
        }
        private void LoadGenres()
        {
            Genres.Clear();
            foreach (var g in _genreService.GetAllGenres())
                Genres.Add(g);

            Genres.Add(new Genre { id = -1, name = "None" }); // Add a default "None" option
        }
        public void ResetGenres()
        {
            Genre1 = null;
            Genre2 = null;
            Genre3 = null;
            Genre4 = null;
        }
        private bool ItemValidation()
        {
            bool hasErrors = false;
            string errorMessage = "Please correct the following errors:\n";

            // Ignore the "None" option which is represented by id == -1 when checking for duplicates.
            bool hasDuplicate = CurrentListItem.genres
                .Where(g => g != null && g.id != -1)
                .GroupBy(g => g.id)
                .Any(group => group.Count() > 1);

            if (string.IsNullOrWhiteSpace(CurrentListItem.title))
            {
                hasErrors = true;
                errorMessage += "- Title is required.\n";
            }
            if (CurrentListItem.rating != 0 &&
               (CurrentListItem.rating < 1 || CurrentListItem.rating > 10))
            {
                hasErrors = true;
                errorMessage += "- Rating must either be 0 or between 1 and 10.\n";
            }
            if (hasDuplicate)
            {
                hasErrors = true;
                errorMessage += "- Duplicate genres selected.\n";
            }

            if (hasErrors)
            {
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return hasErrors;
        }
        private void RemoveImage() 
        { 
            CoverImageData = null;
            if (CurrentListItem != null)
            {
                CurrentListItem.coverImage = null;
            }
        }
    }
}
