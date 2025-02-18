using BeautifulLyricsMobile.Models;
using MauiIcons.Core;
using SpotifyAPI.Web;

namespace BeautifulLyricsMobile.Pages;

[QueryProperty(nameof(CollectionId), "id")]
public partial class PlaylistPage : ContentPage
{
	public string CollectionId { get; set; }
	public SongCollectionModel Collection { get; set; }

	public PlaylistPage()
	{
		InitializeComponent();
		_ = new MauiIcon();

		Collection = new SongCollectionModel();
		BindingContext = Collection;
	}

	private void OnPageLoaded(object sender, EventArgs e)
	{
		Collection.Items.Clear();
		FullPlaylist playlist = LyricsView.Spotify.Playlists.Get(CollectionId).GetAwaiter().GetResult();

		Collection.Url = playlist.Uri;
		Collection.CoverArt = playlist.Images[0].Url;
		Collection.Title = playlist.Name;
		Collection.Artist = playlist.Owner.DisplayName;
		Collection.Info = playlist.Description;

		int index = 0;

		foreach (var item in playlist.Tracks.Items)
		{
			FullTrack track = item.Track as FullTrack;

			Collection.Items.Add(new SongMetadata
			{
				Url = track.Uri,
				Title = track.Name,
				Artist = track.Artists[0].Name,
				Album = track.Album.Name,
				Image = track.Album.Images[0].Url,
				CollectionUrl = Collection.Url,
				Index = index
			});

			index++;
		}
	}

	/*protected override bool OnBackButtonPressed()
	{
		Shell.Current.GoToAsync("//Home");
		return true;
	}*/
}