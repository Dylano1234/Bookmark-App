using Bookmark_App.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bookmark_App.ViewModels
{
    public partial class CreateListViewModel : BaseViewModel
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

        public byte[]? CoverImageData { get; private set; }

        public ImageSource? CoverPreview
        {
            get => _coverPreview;
            set => SetProperty(ref _coverPreview, value);
        }

        public MainViewModel MainViewModel { get; }

        public ICommand SelectImageCommand { get; }
        public ICommand SaveListCommand { get; }

        public CreateListViewModel(MainViewModel mainViewModel)
        {
            SelectImageCommand = new RelayCommand(SelectImage);
            SaveListCommand = new RelayCommand(SaveList, CanSave);
            MainViewModel = mainViewModel;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ListTitle);
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
            if (string.IsNullOrWhiteSpace(ListTitle))
                return;

            var listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            var list = listService.CreateList(ListTitle.Trim(), CoverImageData);

            MainViewModel.CloseCreateList();
            MainViewModel.LoadLists();
        }
    }
}
