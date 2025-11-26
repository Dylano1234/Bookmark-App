using Bookmark_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Models
{
    public class ListItem
    {
        public int id { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public byte[] coverImage { get; set; }
        public ItemStatus status { get; set; }
        public double progressCurrent { get; set; }
        public double progressMax { get; set; }
        public double rating { get; set; }
        public List<Genre> genres { get; set; } = new List<Genre>();
        public ListItem(int id, string title, string url, byte[] coverImage, ItemStatus status, double progressCurrent, double progressMax, double rating, List<Genre> genres)
        {
            this.id = id;
            this.title = title;
            this.url = url;
            this.coverImage = coverImage;
            this.status = status;
            this.progressCurrent = progressCurrent;
            this.progressMax = progressMax;
            this.rating = rating;
            this.genres = genres;
        }

    }
}
