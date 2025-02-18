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
using System.Windows.Input;

namespace BeautifulLyricsMobile.Models
{
	class SearchViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ICommand Search => new Command<string>(async (string query) =>
		{
			var response = await LyricsView.Spotify.Search.Item(new SearchRequest(SearchRequest.Types.All, query));

			List<SearchResult> results = response.Tracks.Items.Select(x => new SearchResult
			{
				Title = x.Name,
				Artist = x.Artists[0].Name,
				ImageUrl = x.Album.Images.Last().Url,
				Url = $"spotify:track:{x.Id}",
				Type = typeof(FullTrack)
			}).ToList();

			results.AddRange(response.Artists.Items.Select(x => new SearchResult
			{
				Title = x.Name,
				Artist = "Artist",
				ImageUrl = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
				Url = $"spotify:artist:{x.Id}",
				Type = typeof(FullArtist)
			}));

			results.AddRange(response.Albums.Items.Select(x => new SearchResult
			{
				Title = x.Name,
				Artist = $"Album - {x.Artists[0].Name}",
				ImageUrl = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
				Url = $"spotify:album:{x.Id}",
				Type = typeof(FullAlbum)
			}));

			// results.AddRange(response.Artists.Items.Select(x => x as IPlayableItem));
			// results.AddRange(response.Albums.Items.Select(x => x as IPlayableItem));

			// List<string> results = response.Tracks.Items.Select(x => x.Name).ToList();
			// results.AddRange(response.Artists.Items.Select(x => x.Name));
			// results.AddRange(response.Albums.Items.Select(x => x.Name));

			// SearchResults = results;
			results.ForEach(SearchResults.Add);
		});

		public ObservableCollection<SearchResult> SearchResults { get; set; } = [];
		/*private List<SearchResult> searchResults = [];
		public List<SearchResult> SearchResults
		{
			get => searchResults;

			set
			{
				searchResults = value;
				NotifyPropertyChanged();
			}
		}*/
	}

	class SearchResult
	{
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }

        public string Url { get; set; }

        public Type Type { get; set; }
    }
}
