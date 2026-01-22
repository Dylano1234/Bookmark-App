using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Bookmark_App.Models
{
    public class ListItem : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
        private byte[]? _coverImage;
        public byte[]? coverImage 
        {   
            get => _coverImage;
            set 
            {
                if (_coverImage == value) return;
                _coverImage = value;
                OnPropertyChanged(nameof(coverImage));
            }
        }
        public ItemStatus status { get; set; }

        // use backing fields so we can react to changes
        private double _progressCurrent;
        public double progressCurrent
        {
            get => _progressCurrent;
            set
            {
                if (_progressCurrent == value) return;
                _progressCurrent = value;
                RecomputeProgressText();
                OnPropertyChanged(nameof(progressCurrent));
            }
        }

        private double _progressMax;
        public double progressMax
        {
            get => _progressMax;
            set
            {
                if (_progressMax == value) return;
                _progressMax = value;
                RecomputeProgressText();
                OnPropertyChanged(nameof(progressMax));
            }
        }

        public double? rating { get; set; }

        // ObservableCollection so changes after construction are observed
        private ObservableCollection<Genre> _genres = new ObservableCollection<Genre>();
        public ObservableCollection<Genre> genres
        {
            get => _genres;
            set
            {
                if (_genres == value) return;
                if (_genres != null)
                    _genres.CollectionChanged -= Genres_CollectionChanged;
                _genres = value ?? new ObservableCollection<Genre>();
                _genres.CollectionChanged += Genres_CollectionChanged;
                RecomputeGenresText();
                OnPropertyChanged(nameof(genres));
            }
        }

        private string? _genresText;
        public string? GenresText
        {
            get => _genresText;
            private set
            {
                if (_genresText == value) return;
                _genresText = value;
                OnPropertyChanged(nameof(GenresText));
            }
        }

        private string? _progressText;
        public string? ProgressText
        {
            get => _progressText;
            private set
            {
                if (_progressText == value) return;
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }

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

            // initialize observable collection from provided list and subscribe
            this._genres = new ObservableCollection<Genre>(genres ?? Enumerable.Empty<Genre>());
            this._genres.CollectionChanged += Genres_CollectionChanged;

            RecomputeGenresText();
            RecomputeProgressText();
        }

        public ListItem()
        {
            _genres = new ObservableCollection<Genre>();
            _genres.CollectionChanged += Genres_CollectionChanged;
            RecomputeGenresText();
            RecomputeProgressText();
        }

        private void Genres_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RecomputeGenresText();
        }

        private void RecomputeGenresText()
        {
            if (genres == null || genres.Count == 0)
            {
                GenresText = "No Genres";
                return;
            }

            GenresText = string.Join(", ", genres.Select(g => g?.name ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)));
        }

        private void RecomputeProgressText()
        {
            ProgressText = $"{progressCurrent} / {progressMax}";
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
