using BeautifulLyricsMobile.Pages;
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
	public class ProfileViewModel : INotifyPropertyChanged
	{
		private string username;

		public string Username
		{
			get => username;
			set
			{
				username = value;
				OnPropertyChanged();
			}
		}

		private string profilePicture;

		public string ProfilePicture
		{
			get => profilePicture;
			set
			{
				profilePicture = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<CollectionMetadata> Items { get; set; } = [];

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class CollectionMetadata
	{
		public string Id { get; set; }
		public string Type { get; set; }

		public string Title { get; set; }
		public string Image { get; set; }
		public string Info { get; set; }

		public CollectionMetadata Item { get; set; }
		public INavigation Navigation { get; set; }

		public CollectionMetadata()
		{
			Item = this;

			ItemSelectedCommand = new Command<CollectionMetadata>(async (item) =>
			{
				if (item.Type == "artist")
				{

				}
				else if (item.Type == "album")
					await Navigation.PushAsync(new AlbumPage
					{
						CollectionId = item.Id
					}, true);
				//await Shell.Current.GoToAsync($"//AlbumPage?id={item.Id}");
				else
					await Navigation.PushAsync(new PlaylistPage
					{
						CollectionId = item.Id
					}, true);
					//await Shell.Current.GoToAsync($"//PlaylistPage?id={item.Id}");
			});
		}

		public Command<CollectionMetadata> ItemSelectedCommand { get; set; }
	}
}
