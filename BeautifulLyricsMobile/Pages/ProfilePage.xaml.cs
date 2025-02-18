using Android.Media;
using BeautifulLyricsMobile.Models;
using CommunityToolkit.Maui.Alerts;
using Java.Lang.Reflect;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BeautifulLyricsMobile.Pages;

public partial class ProfilePage : ContentPage
{
	public ProfileViewModel Profile { get; set; }

	public ProfilePage()
	{
		InitializeComponent();

		Profile = new ProfileViewModel();
		BindingContext = Profile;
	}

	private async void OnPageLoaded(object sender, EventArgs e)
	{
		var user = LyricsView.Spotify.UserProfile.Current().GetAwaiter().GetResult();
		await Toast.Make($"Welcome, {user.DisplayName}!").Show();

		Profile.Username = user.DisplayName;
		Profile.ProfilePicture = user.Images?.Count > 0 ? user.Images[0].Url : "https://t3.ftcdn.net/jpg/06/33/54/78/360_F_633547842_AugYzexTpMJ9z1YcpTKUBoqBF0CUCk10.jpg";
		Profile.Items.Clear();

		var playlists = await LyricsView.Spotify.Playlists.CurrentUsers();

		if (!File.Exists(Path.Combine(FileSystem.AppDataDirectory, "library.json")))
			File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "library.json"), "");

		List<CollectionMetadata> local = [];
		string[] savedList = JsonConvert.DeserializeObject<string[]>(await File.ReadAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "library.json")));
		savedList ??= [];
		var savedOrderDictionary = savedList.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);

		foreach(var item in playlists.Items)
		{
			local.Add(new CollectionMetadata
			{
				Id = item.Id,
				Type = "playlist",
				Title = item.Name,
				Image = item.Images?.Count > 0 ? item.Images[0].Url : "https://www.svgrepo.com/show/508699/landscape-placeholder.svg",
				Info = item.Owner.DisplayName,
				Navigation = Navigation
			});
		}

		var albums = await LyricsView.Spotify.Library.GetAlbums();

		foreach(var item in albums.Items)
		{
			local.Add(new CollectionMetadata
			{
				Id = item.Album.Id,
				Type = "album",
				Title = item.Album.Name,
				Image = item.Album.Images[0].Url,
				Info = item.Album.Artists[0].Name,
				Navigation = Navigation
			});
		}

		// Try to sort by most recently listened to
		// Spotify doesn't provide a way to see this, so the best we can do is look at the user's most recently played items and sort it based on that

		var recentItems = await LyricsView.Spotify.Player.GetRecentlyPlayed(new PlayerRecentlyPlayedRequest
		{
			Limit = 50
		});

		var playlistInfo = (from item in local
							join recent in recentItems.Items on item.Id equals recent.Context?.Uri.Split(':')[2] into recentGroup
							from recent in recentGroup.DefaultIfEmpty()
							group new { Playlist = item, LastPlayed = recent?.PlayedAt } by item.Id into g
							let mostRecent = g.OrderByDescending(x => x.LastPlayed).FirstOrDefault()
							select new
							{
								Playlist = mostRecent.Playlist,
								LastPlayed = mostRecent.LastPlayed,
								SavedOrderIndex = savedOrderDictionary.TryGetValue(mostRecent.Playlist.Id, out int idx) ? idx : int.MaxValue
							}).ToList();

		var playedPlaylists = playlistInfo.Where(x => x.LastPlayed.HasValue).OrderByDescending(x => x.LastPlayed).Select(x => x.Playlist).ToList();
		var notPlayedPlaylists = playlistInfo.Where(x => !x.LastPlayed.HasValue).OrderBy(x => x.SavedOrderIndex).Select(x => x.Playlist).ToList();

		var finalSort = playedPlaylists.Concat(notPlayedPlaylists).ToList();

		/*List<CollectionMetadata> sortedItems = (from item in local
						 join recent in recentItems.Items on item.Id equals recent.Context?.Uri.Split(':')[2] into recentGroup
						 from recent in recentGroup.DefaultIfEmpty()
						 orderby recent?.PlayedAt descending
						 select item).DistinctBy(x => x.Id).ToList();*/

		finalSort.ForEach(Profile.Items.Add);

		List<string> ids = (from item in finalSort select item.Id).ToList();
		await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "library.json"), JsonConvert.SerializeObject(ids));
	}

	private void TouchScrollView_Touch(object sender, EventArgs e)
	{
		Toast.Make("Touch!").Show();
	}

	private void TouchScrollView_Release(object sender, EventArgs e)
	{
		Toast.Make("Release!").Show();
	}
}