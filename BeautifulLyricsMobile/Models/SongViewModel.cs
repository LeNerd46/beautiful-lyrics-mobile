using MauiIcons.Core;
using MauiIcons.Material.Rounded;
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
	public class SongViewModel : INotifyPropertyChanged
	{
		private string _title;
		private string _artist;
		private string _album;
		private string _image;
		private string _animatedImage;

		private MaterialRoundedIcons _playStatus;

		private double _timestamp;
		private double _duration;

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

		public string Album
		{
			get => _album;
			set
			{
				_album = value;
				OnPropertyChanged();
			}
		}

		public string Image
		{
			get => _image;
			set
			{
				_image = value;
				OnPropertyChanged();
			}
		}

		public string AnimatedImage
		{
			get => _animatedImage;
			set
			{
				_animatedImage = value;
				OnPropertyChanged();
			}
		}

		public MaterialRoundedIcons PlayStatus
		{
			get => _playStatus;
			set
			{
				_playStatus = value;
				OnPropertyChanged();
			}
		}

		public double Timestamp
		{
			get => _timestamp;
			set
			{
				_timestamp = value;
				OnPropertyChanged();
			}
		}

		public double Duration
		{
			get => _duration;
			set
			{
				_duration = value;
				OnPropertyChanged();

				DurationString = TimeSpan.FromMilliseconds(Duration).ToString("mm\\:ss");
			}
		}

		private string _durationString;
		private string _timestampString;

		public string DurationString
		{
			get => _durationString;
			set
			{
				_durationString = value;
				OnPropertyChanged();
			}
		}

		public string TimestampString
		{
			get => _timestampString;
			set
			{
				_timestampString = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<PlayableItem> GridRecommendedItems = [];

		private Timer _timer;

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

        public SongViewModel()
        {
			_timer = new Timer(UpdateProgress, null, 0, 1000);
        }

		private void UpdateProgress(object state)
		{
			Timestamp += updateAmount;
			TimestampString = TimeSpan.FromMilliseconds(Timestamp).ToString("mm\\:ss");
		}

		private int updateAmount = 1000;

		public void ToggleTimer(bool state)
		{
			updateAmount = state ? 1000 : 0;
		}
    }

	public class PlayableItem
	{
		public string Image { get; set; }
		public string Title { get; set; }

		public string Subtitle { get; set; }
	}
}
