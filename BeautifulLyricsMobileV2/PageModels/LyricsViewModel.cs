using BeautifulLyricsMobileV2.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BeautifulLyricsMobileV2.PageModels
{
	public class LyricsViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public ISpotifyRemoteService Remote { get; set; }

		public LyricsViewModel(ISpotifyRemoteService remote) 
		{
			Remote = remote;
		}

		public LyricsViewModel()
		{
			Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg";
		}

		private string _id;

		public string Id
		{
			get => _id;
			set 
			{ 
				_id = value;
				OnPropertyChanged();
			}
		}

		private string _title;

		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				OnPropertyChanged();
			}
		}

		private string _album;

		public string Album
		{
			get => _album;
			set
			{
				_album = value;
				OnPropertyChanged();
			}
		}

		private string _artist;

		public string Artist
		{
			get => _artist;
			set
			{
				_artist = value;
				OnPropertyChanged();
			}
		}

		private int _duration;

		public int Duration
		{
			get => _duration;
			set
			{
				_duration = value;
				OnPropertyChanged();
			}
		}

		private bool _isPlaying;

		public bool IsPlaying
		{
			get => _isPlaying;
			set
			{
				_isPlaying = value;
				OnPropertyChanged();
			}
		}

		private string _image;

		public string Image
		{
			get => _image;
			set
			{
				_image = value;
				OnPropertyChanged();
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
