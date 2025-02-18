using BeautifulLyricsMobile.Models;
using MauiIcons.Core;
using SpotifyAPI.Web;

namespace BeautifulLyricsMobile.Pages;

[QueryProperty(nameof(CollectionId), "id")]
public partial class AlbumPage : ContentPage
{
	public string CollectionId { get; set; }
	public SongCollectionModel Collection { get; set; }

	public AlbumPage()
	{
		InitializeComponent();
		_ = new MauiIcon();

		Collection = new SongCollectionModel();
		BindingContext = Collection;
	}

	private void OnPageLoaded(object sender, EventArgs e)
	{
		Collection.Items.Clear();
		FullAlbum album = LyricsView.Spotify.Albums.Get(CollectionId).GetAwaiter().GetResult();

		Collection.Url = album.Uri;
		Collection.CoverArt = album.Images[0].Url;
		Collection.Title = album.Name;
		Collection.Artist = album.Artists[0].Name;
		Collection.Info = $"Album • {album.ReleaseDate.Split('-')[0]}";

		int index = 0;

		foreach (var track in album.Tracks.Items)
		{
			Collection.Items.Add(new SongMetadata
			{
				Url = track.Uri,
				Title = track.Name,
				Artist = track.Artists[0].Name,
				Album = album.Name,
				Image = album.Images[2].Url,
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