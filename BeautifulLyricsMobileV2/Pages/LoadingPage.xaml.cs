using BeautifulLyricsMobileV2.Services;

namespace BeautifulLyricsMobileV2.Pages;

public partial class LoadingPage : ContentPage
{
	ISpotifyRemoteService Spotify { get; }

	public LoadingPage(ISpotifyRemoteService service)
	{
		InitializeComponent();
		Spotify = service;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		Spotify.Connected += async (s, e) => await Shell.Current.GoToAsync("//MainPage");

		/*MainActivity.Connected += async (s, e) =>
		{
			await Shell.Current.GoToAsync("//MainPage");
		};*/
	}
}