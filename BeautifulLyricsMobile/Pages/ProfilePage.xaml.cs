using BeautifulLyricsMobile.Models;
using SpotifyAPI.Web;

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

	private void OnPageLoaded(object sender, EventArgs e)
	{
		// var albums = LyricsView.Spotify?.Library.GetAlbums().GetAwaiter().GetResult();
	}
}