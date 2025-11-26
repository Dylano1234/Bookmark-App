using Bookmark_App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Bookmark_App.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private BaseViewModel currentViewModel;

        public ObservableCollection<List> Lists { get; } = new();

        public MainViewModel()
        {
            // Start op Home
            CurrentViewModel = new HomeViewModel();

            // Dummy-data
            Lists.Add(new List { id = 1, title = "Manga", coverImage = "C:/Users/dylan/Downloads/Manga.jpg" });
            Lists.Add(new List { id = 2, title = "TV Shows", coverImage = null });
            Lists.Add(new List { id = 3, title = "Light Novels", coverImage = null });
            Lists.Add(new List { id = 4, title = "Games", coverImage = null });
            Lists.ElementAt(0).AddListItem(new ListItem(1, "One Piece", "https://onepiece.com", null, ItemStatus.InProgress, 1050, 1050, 5.0, new List<Genre>()));
        }

    }
}
