using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace Bookmark_App.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private BaseViewModel currentViewModel;

        private readonly ListService _listService;

        public CreateListViewModel CreateListViewModel { get; }

        private bool _isCreateListOpen;
        public bool IsCreateListOpen
        {
            get => _isCreateListOpen;
            set => SetProperty(ref _isCreateListOpen, value);
        }

        public ICommand OpenCreateListCommand { get; }
        public ICommand CloseCreateListCommand { get; }

        public ObservableCollection<List> Lists { get; } = new();

        public MainViewModel()
        {
            ListService listService = new ListService(new DataAccess.ListRepository(), new DataAccess.ItemRepository());
            _listService = listService;
            LoadLists();

            CreateListViewModel = new CreateListViewModel(this); // of via DI

            OpenCreateListCommand = new RelayCommand(OpenCreateList);
            CloseCreateListCommand = new RelayCommand(CloseCreateList);

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
    }
}
