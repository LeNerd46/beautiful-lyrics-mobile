#if ANDROID
using Android.Widget;
using Com.Spotify.Protocol.Types;
#endif
using BeautifulLyricsMobile.Models;
using MauiIcons.Core;
using MauiIcons.Material.Rounded;

namespace BeautifulLyricsMobile.Pages;

public partial class HomePage : ContentPage
{
	public SongViewModel Song { get; set; }

	private bool isPlaying = false;

	public HomePage()
	{
		InitializeComponent();
		_ = new MauiIcon();

		Song = new SongViewModel
		{
			Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
			PlayStatus = MaterialRoundedIcons.Pause
		};

		BindingContext = Song;

		if (!MainPage.IsPlaying)
		{
			pauseButton.IsEnabled = true;
			playButton.IsEnabled = false;

			pauseButton.IsVisible = true;
			playButton.IsVisible = false;

			fullPauseButton.IsVisible = true;
			fullPlayButton.IsVisible = false;

			Song.ToggleTimer(true);
		}
		else
		{
			pauseButton.IsEnabled = false;
			playButton.IsEnabled = true;

			pauseButton.IsVisible = false;
			playButton.IsVisible = true;

			fullPauseButton.IsVisible = false;
			fullPlayButton.IsVisible = true;

			Song.ToggleTimer(false);
		}

#if ANDROID
		SpotifyBroadcastReceiver.SongChanged += (sender, song) =>
		{
			Song.Title = song.Name;
			Song.Artist = song.Artist;
			Song.Album = song.Album;

			Song.Duration = song.Length;

			var imageTask = Task.Run(async () =>
			{
				while (MainPage.Remote == null)
					await Task.Delay(10);

				PlayerState player = await MainPage.RequestPositionSync();

				if (player != null)
				{
					Song.Image = $"https://i.scdn.co/image/{player.Track.ImageUri.Raw.Split(':')[2]}";
					Song.Timestamp = player.PlaybackPosition;
				}
			});
		};

		SpotifyBroadcastReceiver.PlaybackChanged += (sender, player) =>
		{
			if (player.IsPlaying)
			{
				Song.PlayStatus = MaterialRoundedIcons.Pause;
				Song.Timestamp = player.Position;

				if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = true;
					playButton.IsVisible = false;

					pauseButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = true;
					fullPlayButton.IsVisible = false;
				}

				// pauseIcon.IsVisible = true;
				// playIcon.IsVisible = false;
			}
			else
			{
				Song.PlayStatus = MaterialRoundedIcons.PlayArrow;

				if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = false;
					playButton.IsVisible = true;

					playButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = false;
					fullPlayButton.IsVisible = true;
				}

				// pauseIcon.IsVisible = false;
				// playIcon.IsVisible = true;
			}
		};
#endif
	}

	protected override bool OnBackButtonPressed()
	{
		if (fullSongCard.IsVisible)
		{
			fullSongCard.IsVisible = false;
			return true;
		}

		return base.OnBackButtonPressed();
	}

	private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		fullSongCard.IsVisible = true;
	}

	private void TogglePause(object sender, EventArgs e)
	{
		isPlaying = !isPlaying;
		Song.ToggleTimer(isPlaying);

#if ANDROID
		if (isPlaying)
			MainPage.Remote.PlayerApi.Resume();
		else
			MainPage.Remote.PlayerApi.Pause();
#endif
	}

	private void FullCardCollapse(object sender, EventArgs e)
	{
		fullSongCard.IsVisible = false;
	}

	private void SkipNext(object sender, EventArgs e)
	{
#if ANDROID
		MainPage.Remote.PlayerApi.SkipNext();
#endif
	}

	private void SkipPrevious(object sender, EventArgs e)
	{
#if ANDROID
		MainPage.Remote.PlayerApi.SkipPrevious();
#endif
	}

	private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		// if (MainPage.Remote != null && Math.Abs(e.NewValue - e.OldValue) > 1000)
		// 	MainPage.Remote.PlayerApi.SeekTo((long)e.NewValue);
	}

	private async void LyricsView(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new MainPage());
	}

	private async void AddLyrics(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new LyricsEditor());
	}

	private void DeleteSong(object sender, EventArgs e)
	{
#if ANDROID
		string path = Path.Combine(FileSystem.CacheDirectory, $"{MainPage.CurrentTrackId}.json");

		if (File.Exists(path))
			File.Delete(path);
		else
			Toast.MakeText(Platform.CurrentActivity, "No lyrics found!", ToastLength.Short).Show();
#endif
	}
}