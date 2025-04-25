using BeautifulLyricsMobileV2.Entities;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.PageModels
{
    public partial class SongMoreOptionsModel : ObservableObject
    {
		[ObservableProperty]
		private SpotifyTrack track;

		[RelayCommand]
		public async Task OpenInSpotify()
		{
			if (await Launcher.CanOpenAsync(Track.Uri))
				await Launcher.OpenAsync(Track.Uri);
			else
				await Toast.Make($"Failed to open {Track.Title} in Spotify").Show();
		}
	}
}
