using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Models
{
	public class MoreOptionsModel : INotifyPropertyChanged
	{
		private string _url;

		public string Url
		{
			get => _url;
			set
			{
				_url = value;
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

		public MoreOptionsModel() { }

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
