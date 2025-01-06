using AndroidX.Annotations;
using BeautifulLyricsMobile.Pages;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Models
{
    public class SongCollectionModel : INotifyPropertyChanged
    {
        public ObservableCollection<SongMetadata> Items { get; set; } = [];
        public string Url { get; set; }

        private string _title;
        private string _artist;
        private string _info;
        private string _coverArt;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Artist
        {
            get => _artist;
            set
            {
                _artist = value;
                OnPropertyChanged();
            }
        }

        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                OnPropertyChanged();
            }
        }

        public string CoverArt
        {
            get => _coverArt;
            set
            {
                _coverArt = value;
                OnPropertyChanged();
            }
        }

        public Command PlayCollectionCommand { get; set; }

		public SongCollectionModel()
		{
            PlayCollectionCommand = new Command(() =>
            {
                LyricsView.Remote?.PlayerApi?.Play(Url);
            });
		}

		public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SongMetadata
    {
        public string Url { get; set; }
        public string Image { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

		public string CollectionUrl { get; set; }
		public int Index { get; set; }

		public Command PlaySongCommand { get; set; }

        public SongMetadata()
        {
            PlaySongCommand = new Command(() =>
            {
                // LyricsView.Remote?.PlayerApi?.Play(CollectionUrl);
                LyricsView.Remote?.PlayerApi?.SkipToIndex(CollectionUrl, Index);
            });
        }
    }
}
