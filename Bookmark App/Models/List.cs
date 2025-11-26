using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Models
{
    public class List
    {
        public int id { get; set; }
        public string title { get; set; }
        public string coverImage { get; set; }
        public List<ListItem> listItems { get; set; } = new List<ListItem>();
        public int itemCount => listItems.Count;

        public List(int id, string title, string coverImage)
        {
            this.id = id;
            this.title = title;
            this.coverImage = coverImage;
        }
        public List()
        {
            
        }

        public void AddListItem(ListItem listItem)
        {
            listItems.Add(listItem);
        }
        public void RemoveListItem(ListItem listItem) {
            listItems.Remove(listItem);
        }
        public int ItemCount()
        {
            return listItems.Count;
        }
    }
}
