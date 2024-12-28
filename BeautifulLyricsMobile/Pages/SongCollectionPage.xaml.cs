using BeautifulLyricsMobile.Models;
using Java.Lang;
using SpotifyAPI.Web;

namespace BeautifulLyricsMobile.Pages;

[QueryProperty(nameof(CollectionId), "id")]
[QueryProperty(nameof(Type), "type")]
public partial class SongCollectionPage : ContentPage
{
    public string CollectionId { get; set; }
    public string Type { get; set; }

    public SongCollectionModel Collection { get; set; }

    public SongCollectionPage()
	{
		InitializeComponent();
        Collection = new SongCollectionModel();
        BindingContext = Collection;
	}

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        if(Type == "album")
        {
            FullAlbum album = await LyricsView.Spotify.Albums.Get(CollectionId);
            Collection.CoverArt = album.Images[1].Url;
            Collection.Title = album.Name;
            Collection.Artist = album.Artists[0].Name;
            Collection.Info = $"Album • {album.ReleaseDate.Split('-')[0]}";

            foreach(var track in album.Tracks.Items)
            {
                Collection.Items.Add(new SongMetadata
                {
                    Url = track.Uri,
                    Title = track.Name,
                    Artist = track.Artists[0].Name,
                    Album = album.Name,
                    Image = album.Images[2].Url
                });
            }
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Shell.Current.GoToAsync("//Home");
        return true;
    }
}