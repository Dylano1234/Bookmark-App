using Bookmark_App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class ListItemDetailViewModel : ObservableObject
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

        private string _imageLink;
        public string ImageLink
        {
            get => _imageLink;
            set => SetProperty(ref _imageLink, value);
        }

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

        private Genre? _genre5;
        public Genre? Genre5
        {
            get => _genre5;
            set => SetProperty(ref _genre5, value);
        }
        private Genre? _genre6;
        public Genre? Genre6
        {
            get => _genre6;
            set => SetProperty(ref _genre6, value);
        }

        public ObservableCollection<Genre> Genres { get; set; } = new ObservableCollection<Genre>();
        private MainViewModel MainViewModel;
        public ICommand SaveListItemCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand DeleteListItemCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand FetchImageCommand { get;  }
        public ICommand GetImageFromClipboardCommand { get;  }

        private readonly Services.ItemService _itemService = new Services.ItemService(new DataAccess.ItemRepository());
        private readonly Services.GenreService _genreService = new Services.GenreService(new DataAccess.GenreRepository());
        public ListItemDetailViewModel(MainViewModel mainViewModel)
        {
            SaveListItemCommand = new RelayCommand(SaveListItem);
            SelectImageCommand = new RelayCommand(SelectImage);
            DeleteListItemCommand = new RelayCommand(DeleteListItem);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            FetchImageCommand = new RelayCommand(FetchImage);
            GetImageFromClipboardCommand = new RelayCommand(GetImageFromClipboard);

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
            if(Genre5 != null)
            {
                if (Genre5.id != -1)
                {
                    CurrentListItem.genres.Add(Genre5);
                }
            }
            if(Genre6 != null)
            {
                if (Genre6.id != -1)
                {
                    CurrentListItem.genres.Add(Genre6);
                }
            }

            if (ItemValidation())
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
            ImageLink = string.Empty;
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
                ImageLink = string.Empty;
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
            Genre5 = null;
            Genre6 = null;
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
        private async void FetchImage()
        {
            if(!(ImageLink == null || ImageLink == ""))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        httpClient.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
                        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                        httpClient.Timeout = TimeSpan.FromSeconds(10);

                        var imageData = await httpClient.GetByteArrayAsync(ImageLink);
                        CoverImageData = imageData;
                        if (CurrentListItem != null)
                            CurrentListItem.coverImage = CoverImageData;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Image URL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please provide an image URL before submitting.", "No URL Provided", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GetImageFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                var bitmapSource = Clipboard.GetImage();

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 100;
                byte[] bit = new byte[0];
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);
                    bit = stream.ToArray();
                    stream.Close();
                }

                CoverImageData = bit;
                if (CurrentListItem != null)
                    CurrentListItem.coverImage = CoverImageData;
            }
            else
            {
                MessageBox.Show("Clipboard does not contain an image.", "No Image in Clipboard", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
