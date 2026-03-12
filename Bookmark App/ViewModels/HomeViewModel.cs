using Bookmark_App.Models;
using Bookmark_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Bookmark_App.ViewModels
{
    public class HomeViewModel : ObservableObject
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
