using BeautifulLyricsMobileV2.Services;
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

		var tcs = new TaskCompletionSource<bool>();

		Spotify.Connected += async (s, e) => tcs.TrySetResult(true);

		var first = await Task.WhenAny(tcs.Task, Task.Delay(1000));

		if (first == tcs.Task && tcs.Task.Result)
			await Shell.Current.GoToAsync("//MainPage");
		else
		{
			Preferences.Set("Onboarding", false);

			Window window = new Window(new OnboardingPage());
			Application.Current?.OpenWindow(window);

			Window shell = Application.Current?.Windows.FirstOrDefault(x => x.Page is AppShell);
			if (shell != null) Application.Current?.CloseWindow(shell);
		}
	}
}