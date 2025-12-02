using Bookmark_App.Models;
using Bookmark_App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Bookmark_App.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly ListService _listService;

        public ObservableCollection<List> Lists { get; } = new();

        public HomeViewModel(ListService listService)
        {
            _listService = listService;
            LoadLists();
        }

        public void LoadLists()
        {
            Lists.Clear();
            foreach (var l in _listService.GetAllLists())
                Lists.Add(l);
        }
    }
}
