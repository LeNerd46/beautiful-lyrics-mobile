using BeautifulLyricsMobileV2.Services;
using CommunityToolkit.Maui.Alerts;
using System.Threading.Tasks;

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

		if (Spotify.Client != null)
		{
			if (Spotify.IsConnected)
				await Shell.Current.GoToAsync("//MainPage");
			else if (await Spotify.Connect())
				await Shell.Current.GoToAsync("//MainPage");
			else
				await Toast.Make("Failed to connect to Spotify!").Show();

			return;
		}

		var tcs = new TaskCompletionSource<bool>();

		Spotify.Connected += async (s, e) => tcs.TrySetResult(true);

		var first = await Task.WhenAny(tcs.Task, Task.Delay(1000));

		if (first == tcs.Task && tcs.Task.Result)
			await Shell.Current.GoToAsync("//MainPage");
		else
		{
			Preferences.Set("Onboarding", false);

			if (Application.Current?.Windows[0].Page != null)
				Application.Current.Windows[0].Page = new OnboardingPage();
		}
	}
}