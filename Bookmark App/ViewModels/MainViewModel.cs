using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;

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

        public ICommand OpenCreateListCommand { get; }
        public ICommand CloseCreateListCommand { get; }
        public ICommand OpenListCommand { get; }
        public ICommand OpenHomeCommand { get; }
        public ICommand OpenUrlCommand { get; }

        public ObservableCollection<List> Lists { get; } = new();

        public MainViewModel()
        {
            ListService listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            _listService = listService;
            LoadLists();

            CreateListViewModel = new ListCreationViewModel(this);

            OpenCreateListCommand = new RelayCommand(OpenCreateList);
            CloseCreateListCommand = new RelayCommand(CloseCreateList);
            OpenListCommand = new RelayCommand<List>(OpenList);
            OpenHomeCommand = new RelayCommand(OpenHome);
            OpenUrlCommand = new RelayCommand<string>(OpenUrl);

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
            IsCreateListOpen = true;
        }

        public void CloseCreateList()
        {
            IsCreateListOpen = false;
            if (currentViewModel is HomeViewModel homeVM)
            {
                homeVM.LoadLists();
            }
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
    }
}
