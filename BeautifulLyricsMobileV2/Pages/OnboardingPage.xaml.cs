using CommunityToolkit.Maui.Alerts;
using BeautifulLyricsMobileV2.PageModels;
using BeautifulLyricsMobileV2.Services;
using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using Newtonsoft.Json;
using Image = Microsoft.Maui.Controls.Image;

#if ANDROID
using Com.Spotify.Android.Appremote.Api;
using static Com.Spotify.Protocol.IWampClient;
using Android.Content;
using Android.Content.PM;
#endif

namespace BeautifulLyricsMobileV2.Pages;

public partial class OnboardingPage : ContentPage
{
	RadialGradientBrush firstPage;
	RadialGradientBrush secondPage;
	RadialGradientBrush thirdPage;
	RadialGradientBrush fourthPage;

	private readonly Color[] firstPageColors;
	private readonly Color[] secondPageColors;
	private readonly Color[] thirdPageColors;
	private readonly Color[] fourthPageColors;

	private readonly OnboardingModel onboarding;
	private string spotifyId;

	private bool spotifyConnected;
	ISpotifyRemoteService spotify;

	public OnboardingPage()
	{
		InitializeComponent();

		firstPage = Resources["AnimatedBrush"] as RadialGradientBrush;
		firstPageColors = firstPage.GradientStops.Select(x => x.Color).ToArray();

		secondPage = Resources["SpotifyPageBrush"] as RadialGradientBrush;
		secondPageColors = secondPage.GradientStops.Select(x => x.Color).ToArray();

		thirdPage = Resources["SpotifyWebPageBrush"] as RadialGradientBrush;
		thirdPageColors = thirdPage.GradientStops.Select(x => x.Color).ToArray();

		fourthPage = Resources["LastPageBrush"] as RadialGradientBrush;
		fourthPageColors = fourthPage.GradientStops.Select(x => x.Color).ToArray();

		onboarding = new OnboardingModel();
		BindingContext = onboarding;

		onboarding.Background = firstPage;
		AnimateBrushCenter(true);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		Window.Resumed += OnWindowResumed;
	}

	protected override void OnDisappearing()
	{
		Window.Resumed -= OnWindowResumed;

		base.OnDisappearing();
	}

	private void OnWindowResumed(object? sender, EventArgs e)
	{
		if (spotifyConnected)
		{
			onboardingView.ScrollTo(2);
			onboardingView.IsSwipeEnabled = false;
		}
	}

	void AnimateBrushCenter(bool forward)
	{
		double startX = forward ? 0.98 : 1.52;
		double endX = forward ? 1.52 : 0.98;
		double startY = forward ? 0.38 : 0.52;
		double endY = forward ? 0.52 : 0.38;

		var xAnim = new Animation(v => onboarding.Background.Center = new Point(v, onboarding.Background.Center.Y), startX, endX);
		var yAnim = new Animation(v => onboarding.Background.Center = new Point(onboarding.Background.Center.X, v), startY, endY);

		var parent = new Animation
		{
			{ 0, 1, xAnim },
			{ 0, 1, yAnim }
		};

		parent.Commit(
			owner: this,
			name: "BrushShift",
			length: 20000,
			easing: Easing.SinInOut,
			finished: (v, c) => AnimateBrushCenter(!forward)
		);
	}

	private async Task ConnectSpotify()
	{
		if (DeviceInfo.Platform == DevicePlatform.Android)
		{
#if ANDROID
			try
			{
				if(!SpotifyAppRemote.IsSpotifyInstalled(Platform.CurrentActivity))
				{
					await Toast.Make("Couldn't find Spotify installation. Please ensure Spotify is installed").Show();
					return;
				}

				spotify = IPlatformApplication.Current.Services.GetRequiredService<ISpotifyRemoteService>();

				bool success = await spotify.Connect(true, spotifyId);

				if (success)
				{
					onboarding.Items.Add(new OnboardingItem("Beautiful Lyrics", "Now let's connect the Web API", "", "Connect", 2, false));
					spotifyConnected = true;
				}
				else
					await Toast.Make("Failed to connect to Spotify!").Show();

				await SecureStorage.SetAsync("spotifyId", spotifyId);
			}
			catch (Exception ex)
			{
				await Toast.Make(ex.Message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
			}
#endif
		}
	}

	private async void ProgressOnboarding(object sender, EventArgs e)
	{
		switch (onboardingView.Position)
		{
			case 0:
				onboardingView.ScrollTo(onboardingView.Position + 1);
				break;

			case 1:

				if (string.IsNullOrWhiteSpace(spotifyId))
				{
					await Toast.Make("Please enter your Spotify client ID").Show();
					break;
				}

				await ConnectSpotify();
				break;

			case 2:
				using (HttpClient client = new HttpClient() { BaseAddress = new Uri("https://beautifullyrics.lenerd.tech/api/") })
				{
					try
					{
						HttpResponseMessage responseMessage = await client.GetAsync("spotify/login");
						if (!responseMessage.IsSuccessStatusCode)
						{
							await Toast.Make($"Failed to get login URI - {responseMessage.StatusCode}").Show();
							break;
						}

						JObject json = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
						await SecureStorage.SetAsync("state", json["state"].ToString());

						await WebAuthenticator.Default.AuthenticateAsync(new Uri(json["loginUri"].ToString()), new Uri("beautifullyrics://"));
					}
					catch(TaskCanceledException) 
					{
						string state = await SecureStorage.GetAsync("state");
						if (string.IsNullOrWhiteSpace(state))
						{
							await Toast.Make("Invalid state").Show();
							break;
						}

						HttpResponseMessage responseMessage = await client.GetAsync($"spotify/auth?state={state}");

						if(!responseMessage.IsSuccessStatusCode)
						{
							await Toast.Make($"Failed to authenticate - {responseMessage.StatusCode}").Show();
							break;
						}

						string auth = await responseMessage.Content.ReadAsStringAsync();
						PKCETokenResponse token = JsonConvert.DeserializeObject<PKCETokenResponse>(auth);
						if (token == null)
						{
							await Toast.Make("Failed to authenticate").Show();
							break;
						}

						spotify.Token = token.AccessToken;
						var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new PKCEAuthenticator(spotifyId, token));
						spotify.WebClient = new SpotifyClient(config);
						File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "creds.json"), auth);

						onboarding.Items.Add(new OnboardingItem("Beautiful Lyrics", "You're all set to use Beautiful Lyrics!", "", "Get Started", 2, false));
						onboardingView.ScrollTo(3);
					}
				}
				break;

			case 3:
				Preferences.Set("Onboarding", true);

				if (Application.Current?.Windows[0].Page != null)
					Application.Current.Windows[0].Page = new AppShell();

				break;
		}
	}

	private void OnSpotifyEntryTextChanged(object sender, TextChangedEventArgs e) => spotifyId = e.NewTextValue.Trim();

	private async void HelpButtonTapped(object sender, TappedEventArgs e)
	{
		Image button = sender as Image;

		await button.ScaleTo(0.8, 150, Easing.CubicIn);
		await button.ScaleTo(1, 150, Easing.CubicOut);

		await Launcher.OpenAsync("https://github.com/LeNerd46/beautiful-lyrics-mobile/blob/main/setup.md");
	}

	private void AnimateGradientStop(int index, Color startColor, Color endColor, uint length = 1000)
	{
		var animation = new Animation(v =>
		{
			var newColor = Color.FromRgba(
				startColor.Red + (endColor.Red - startColor.Red) * v,
				startColor.Green + (endColor.Green - startColor.Green) * v,
				startColor.Blue + (endColor.Blue - startColor.Blue) * v,
				startColor.Alpha + (endColor.Alpha - startColor.Alpha) * v);
			onboarding.Background.GradientStops[index].Color = newColor;
		});

		animation.Commit(this, $"GradientStop_{index}", length: length, easing: Easing.CubicOut);
	}

	private void OnboardingViewChanged(object sender, PositionChangedEventArgs e)
	{
		this.AbortAnimation("GradientStop_0");
		this.AbortAnimation("GradientStop_1");
		this.AbortAnimation("GradientStop_2");
		this.AbortAnimation("GradientStop_3");

		switch (e.CurrentPosition)
		{
			case 0:
				for (int i = 0; i < onboarding.Background.GradientStops.Count; i++)
				{
					Color startColor = onboarding.Background.GradientStops[i].Color;
					Color endColor = firstPageColors[i];
					AnimateGradientStop(i, startColor, endColor);
				}
				break;

			case 1:
				for (int i = 0; i < onboarding.Background.GradientStops.Count; i++)
				{
					Color startColor = onboarding.Background.GradientStops[i].Color;
					Color endColor = secondPageColors[i];
					AnimateGradientStop(i, startColor, endColor);
				}
				break;

			case 2:
				for (int i = 0; i < onboarding.Background.GradientStops.Count; i++)
				{
					Color startColor = onboarding.Background.GradientStops[i].Color;
					Color endColor = thirdPageColors[i];
					AnimateGradientStop(i, startColor, endColor);
				}
				break;

			case 3:
				for (int i = 0; i < onboarding.Background.GradientStops.Count; i++)
				{
					Color startColor = onboarding.Background.GradientStops[i].Color;
					Color endColor = fourthPageColors[i];
					AnimateGradientStop(i, startColor, endColor);
				}
				break;
		}
	}
}