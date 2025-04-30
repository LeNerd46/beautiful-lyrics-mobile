using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls;
using BeautifulLyricsMobileV2.Services;
using BeautifulLyricsMobileV2.PageModels;

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

	private readonly Color[] firstPageColors;
	private readonly Color[] secondPageColors;
	private readonly Color[] thirdPageColors;

	private readonly OnboardingModel onboarding;
	private string spotifyId;

	private bool spotifyConnected;

	public OnboardingPage()
	{
		InitializeComponent();

		firstPage = Resources["AnimatedBrush"] as RadialGradientBrush;
		firstPageColors = firstPage.GradientStops.Select(x => x.Color).ToArray();

		secondPage = Resources["SpotifyPageBrush"] as RadialGradientBrush;
		secondPageColors = secondPage.GradientStops.Select(x => x.Color).ToArray();

		thirdPage = Resources["LastPageBrush"] as RadialGradientBrush;
		thirdPageColors = thirdPage.GradientStops.Select(x => x.Color).ToArray();

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
		if(spotifyConnected)
		{
			onboardingView.ScrollTo(3);
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

	private void ConnectSpotify()
	{
		if (DeviceInfo.Platform == DevicePlatform.Android)
		{
#if ANDROID
			try
			{
				ISpotifyRemoteService spotify = IPlatformApplication.Current.Services.GetRequiredService<ISpotifyRemoteService>();

				ConnectionListener listener = new ConnectionListener();

				listener.Connected += (s, e) =>
				{
					spotify.SetRemoteClient(e.Remote);
					spotify.InvokeConnected();

					onboarding.Items.Add(new OnboardingItem("Beautiful Lyrics", "You're all set to use Beautiful Lyrics!", "", "Get Started", 2, false));
					spotifyConnected = true;
				};

				listener.Failed += (s, e) => Toast.Make(e.ErrorMessage, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();

				SpotifyAppRemote remote;
				ConnectionParams connectionParams = new ConnectionParams.Builder(spotifyId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();

				var spotifyIntent = Platform.CurrentActivity.PackageManager.GetLaunchIntentForPackage("com.spotify.music");
				spotifyIntent?.AddFlags(ActivityFlags.ReorderToFront);
				Platform.CurrentActivity.StartActivity(spotifyIntent);

				SpotifyAppRemote.Connect(Platform.CurrentActivity, connectionParams, listener);

				SecureStorage.SetAsync("spotifyId", spotifyId);
			}
			catch (System.Exception ex)
			{
				Toast.Make(ex.Message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
			}
#endif
		}
	}

	private void ProgressOnboarding(object sender, EventArgs e)
	{
		switch (onboardingView.Position)
		{
			case 0:
				onboardingView.ScrollTo(onboardingView.Position + 1);
				break;

			case 1:

				if (string.IsNullOrWhiteSpace(spotifyId))
				{
					Toast.Make("Please enter your Spotify client ID").Show();
					break;
				}

				ConnectSpotify();
				break;

			case 2:
				Preferences.Set("Onboarding", true);

				Application.Current?.OpenWindow(new Window(new AppShell()));
				Application.Current?.CloseWindow(GetParentWindow());
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
		}
	}
}