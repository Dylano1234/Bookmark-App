namespace Bookmark_App.Models
{
    public class Genre
    {
        public String name { get; set; }
        public int id { get; set; }

        public Genre(int id, String name)
        {
            this.id = id;
            this.name = name;
        }
        public Genre()
        {
        }

        public override string ToString()
        {
            return name;
        }
    }
}
