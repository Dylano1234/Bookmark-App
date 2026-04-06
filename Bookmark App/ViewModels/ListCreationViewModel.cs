using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class ListCreationViewModel : ObservableObject
    {
        private ImageSource? _coverPreview;
        private string? _listTitle;

        public string? ListTitle
        {
            get => _listTitle;
            set
            {
                if (SetProperty(ref _listTitle, value))
                {
                    // update command enabled state when title changes
                    (SaveListCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        private bool _isNewList;
        public bool IsNewList
        {
            get => _isNewList;
            set => SetProperty(ref _isNewList, value);
        }
        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }
        private Models.List? _currentList;
        public Models.List? CurrentList
        {
            get => _currentList;
            set => SetProperty(ref _currentList, value);
        }

        public byte[]? CoverImageData { get;  set; }

        public ImageSource? CoverPreview
        {
            get => _coverPreview;
            set => SetProperty(ref _coverPreview, value);
        }

        private string _imageLink;
        public string ImageLink
        {
            get => _imageLink;
            set => SetProperty(ref _imageLink, value);
        }

        public MainViewModel MainViewModel { get; }

        public ICommand SelectImageCommand { get; }
        public ICommand SaveListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand FetchImageCommand { get;  }

        public ListCreationViewModel(MainViewModel mainViewModel)
        {
            SelectImageCommand = new RelayCommand(SelectImage);
            SaveListCommand = new RelayCommand(SaveList);
            DeleteListCommand = new RelayCommand(DeleteList);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            FetchImageCommand = new RelayCommand(FetchImage);
            MainViewModel = mainViewModel;
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

                CoverPreview = bmp;
            }
        }

        private void SaveList()
        {
            if (!CanSaveList())
            {
                return; 
            }
            var listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            if (IsNewList)
            {
                
                listService.CreateList(ListTitle.Trim(), CoverImageData);
            }
            else
            {
                listService.UpdateList(CurrentList, ListTitle.Trim(), CoverImageData);
            }
            ListTitle = string.Empty;
            CoverImageData = null;
            CoverPreview = null;
            MainViewModel.CloseCreateList();
            MainViewModel.LoadLists();
        }
        private bool CanSaveList()
        {
            bool canSave = true;
            string errorMessage = "Please correct the following errors:\n";
            var listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            List<Models.List> AllLists = listService.GetAllLists();
            bool titleExists = AllLists.Any(l =>
                 l.title.Equals(ListTitle, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(ListTitle))
            {
                errorMessage += "- List title cannot be empty.\n";
                canSave = false;
            }
            if(CurrentList != null)
            {
                if (titleExists && ListTitle != CurrentList.title)
                {
                    errorMessage += "- A list with this title already exists. Please choose a different title.\n";
                    canSave = false;
                }
            } 
            else if (titleExists)
            {
                errorMessage += "- A list with this title already exists. Please choose a different title.\n";
                canSave = false;
            }
            

            if (!canSave)
            {
                System.Windows.MessageBox.Show(errorMessage, "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            return canSave;
        }
        private void DeleteList()
        {
            var result = MessageBox.Show(
                    "Are you sure you want to delete this List?\nDoing so will also delete all Items associated with this List.",
                    "Delete List",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            var listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            listService.DeleteList(CurrentList);
            ListTitle = string.Empty;
            CoverImageData = null;
            CoverPreview = null;
            MainViewModel.CloseCreateList();
            MainViewModel.LoadLists();
        }
        private void RemoveImage()
        {
            CoverImageData = null;
            CoverPreview = null;
        }
        private async void FetchImage()
        {
            if (!(ImageLink == null || ImageLink == ""))
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
                        var bmp = new BitmapImage();
                        using (var ms = new MemoryStream(imageData))
                        {
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = ms;
                            bmp.EndInit();
                            bmp.Freeze();
                        }

                        CoverPreview = bmp;
                        if (CurrentList != null)
                            CurrentList.coverImage = CoverImageData;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Image URL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please provide an image URL before submitting.", "No URL Provided", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
