using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Bookmark_App.Models
{
    public class List : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string title { get; set; }
        public byte[] coverImage { get; set; }

        // Use ObservableCollection and raise notifications when the collection changes
        private ObservableCollection<ListItem> _listItems = new ObservableCollection<ListItem>();
        public ObservableCollection<ListItem> listItems
        {
            get => _listItems;
            set
            {
                if (_listItems == value) return;
                if (_listItems != null)
                    _listItems.CollectionChanged -= ListItems_CollectionChanged;

                _listItems = value ?? new ObservableCollection<ListItem>();
                _listItems.CollectionChanged += ListItems_CollectionChanged;
                OnPropertyChanged(nameof(listItems));
                // keep the cached itemCount in sync
                itemCount = _listItems.Count;
            }
        }

        // Backing field so repository can set the initial count cheaply,
        // and collection changes still update it.
        private int _itemCount;
        public int itemCount
        {
            get => _itemCount;
            set
            {
                if (_itemCount == value) return;
                _itemCount = value;
                OnPropertyChanged(nameof(itemCount));
            }
        }

        public List(int id, string title, byte[] coverImage)
        {
            this.id = id;
            this.title = title;
            this.coverImage = coverImage;
            _listItems.CollectionChanged += ListItems_CollectionChanged;
            itemCount = _listItems.Count;
        }
        public List()
        {
            _listItems.CollectionChanged += ListItems_CollectionChanged;
            itemCount = _listItems.Count;
        }

        private void ListItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // keep itemCount in sync whenever the collection changes
            itemCount = _listItems?.Count ?? 0;
        }

        public void AddListItem(ListItem listItem)
        {
            listItems.Add(listItem);
            // collection change handler will update itemCount
        }
        public void RemoveListItem(ListItem listItem) {
            listItems.Remove(listItem);
            // collection change handler will update itemCount
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
