using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class ListCreationViewModel : ObservableObject
    {
        private Byte[]? _coverPreview;
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

        public byte[]? CoverPreview
        {
            get => _coverPreview;
            set => SetProperty(ref _coverPreview, value);
        }

        public MainViewModel MainViewModel { get; }

        public ICommand SelectImageCommand { get; }
        public ICommand SaveListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand GetImageFromClipboardCommand { get; }

        public ListCreationViewModel(MainViewModel mainViewModel)
        {
            SelectImageCommand = new RelayCommand(SelectImage);
            SaveListCommand = new RelayCommand(SaveList);
            DeleteListCommand = new RelayCommand(DeleteList);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            GetImageFromClipboardCommand = new RelayCommand(GetImageFromClipboard);
            MainViewModel = mainViewModel;
        }

        private void SelectImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Choose a cover image",
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dlg.FileName);
                CoverImageData = bytes;
                CoverPreview = bytes;
            }
        }

        private void SaveList()
        {
            var listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            
            ValidationResult result;
            if (IsNewList)
            {
                result = listService.CreateList(ListTitle.Trim(), CoverImageData, out var createdList);
            }
            else
            {
                result = listService.UpdateList(CurrentList, ListTitle.Trim(), CoverImageData);
            }

            if (!result.IsSuccess)
            {
                System.Windows.MessageBox.Show($"Please correct the following errors:\n{result.ErrorMessage}", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            ListTitle = string.Empty;
            CoverImageData = null;
            CoverPreview = null;
            MainViewModel.CloseCreateList();
            MainViewModel.LoadLists();
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
                CoverPreview = bit;
                CoverImageData = bit;
                if (CurrentList != null)
                    CurrentList.coverImage = CoverImageData;
            }
            else
            {
                MessageBox.Show("Clipboard does not contain an image.", "No Image in Clipboard", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
