using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Bookmark_App.Models;

namespace Bookmark_App.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        public ObservableCollection<List> Lists { get; } = new();
        public HomeViewModel()
        {
            // Dummy-data
            Lists.Add(new List { id = 1, title = "Manga", coverImage = "C:/Users/dylan/Downloads/Manga.jpg" });
            Lists.Add(new List { id = 2, title = "TV Shows", coverImage = null });
            Lists.Add(new List { id = 3, title = "Light Novels", coverImage = null });
            Lists.Add(new List { id = 4, title = "Games", coverImage = null });
            Lists.ElementAt(0).AddListItem(new ListItem(1, "One Piece", "https://onepiece.com", null, ItemStatus.InProgress, 1050, 1050, 5.0, new List<Genre>()));
        }
    }
}
