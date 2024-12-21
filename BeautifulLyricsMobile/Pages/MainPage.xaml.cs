using RestSharp;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Diagnostics;
using BeautifulLyricsAndroid.Entities;
using Newtonsoft.Json;
using Button = Microsoft.Maui.Controls.Button;
using System.Text;
using CommunityToolkit.Maui.Storage;
using SpotifyAPI.Web.Http;
using SkiaSharp;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;
using BeautifulLyricsMobile.Entities;
using Newtonsoft.Json.Linq;
using BeautifulLyricsMobile.Controls;
using System.Collections.ObjectModel;

using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Types;
using static Com.Spotify.Protocol.Client.Subscription;
using static Com.Spotify.Protocol.Client.CallResult;
using Android.Widget;
using Java.Security;
using BeautifulLyricsMobile.Models;
using MauiIcons.Material.Rounded;
using MauiIcons.Core;

namespace BeautifulLyricsMobile
{
	// public delegate void SongChanged(object sender, SongChangedEventArgs e);
	// public delegate void PlaybackChanged(object sender, PlaybackChangedEventArgs e);
	// public delegate void TimeStepped(object sender, TimeSteppedEventArgs e);

	public partial class MainPage : ContentView
	{/*
#if ANDROID
		public static SpotifyAppRemote Remote { get; set; }
#endif
		internal static string CurrentTrackId { get; set; }
		public static bool IsPlaying { get; set; }

		internal static RestClient Client { get => client; set => client = value; }
		private static RestClient client;

		internal static SpotifyClient Spotify { get => spotify; set => spotify = value; }
		private static SpotifyClient spotify;

		internal static string AccessToken { get => accessToken; set => accessToken = value; }
		private static string accessToken = "";

		public static event TimeStepped TimeStepped;

		private EmbedIOAuthServer server;
		private bool updatedToken = true;

		private static Stopwatch stopwatch = new Stopwatch();
		private LyricsScroller scroller;

		private string type = "None";
		double lyricsEndTime = -1;
		private Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups = [];
		private List<double> vocalGroupStartTimes = [];

		private double timestamp;
		private bool isPlaying;
		private bool syncProgress = true;

		private bool newSong = false;
		private bool local = false;
		private bool skipped = false;
		private long newTimestamp = -1;
		private long resetOffset = 0;

		private Task activeTask = null;
		private System.Timers.Timer progressSyncTimer;
		private CancellationTokenSource cancelToken;
		private CancellationTokenSource backgroundCancel;

		private int lineIndex = 0;
		private int previousLineIndex = -1;

		private bool lyricsDone = false;

		public SongViewModel Song { get; set; }
		private List<Layout> lines = [];

		public MainPage()
		{
			InitializeComponent();
			_ = new MauiIcon();
#if ANDROID

			SpotifyBroadcastReceiver.PlaybackChanged += OnPlaybackChanged;
			SpotifyBroadcastReceiver.SongChanged += OnSongChanged;

			Song = new SongViewModel
			{
				Title = "Title",
				Artist = "Artist",
				Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg"
			};

			BindingContext = Song;

			newSong = true;
			stopwatch.Reset();
			// LyricsContainer.Dispatcher.Dispatch(LyricsContainer.Children.Clear);
			cancelToken = new CancellationTokenSource();
			backgroundCancel = new CancellationTokenSource();
			scroller = new LyricsScroller(ScrollViewer, LyricsContainer);

			Task.Run(RenderLyrics, cancelToken.Token);
#endif
		}

		protected async void OnAppearing()
		{
			PlayerState player = await RequestPositionSync();

			if (player != null)
			{
				Song.Title = player.Track.Name;
				Song.Artist = player.Track.Artist.Name;
				Song.Album = player.Track.Album.Name;
				Song.Duration = player.Track.Duration;
				Song.Timestamp = player.PlaybackPosition;
				isPlaying = !player.IsPaused;

				Song.Image = $"https://i.scdn.co/image/{player.Track.ImageUri.Raw.Split(':')[2]}";
			}
		}

		private async void OnPlaybackChanged(object sender, PlaybackChangedEventArgs e)
		{
			if (e.IsPlaying != isPlaying)
			{
				isPlaying = e.IsPlaying;

				if (!isPlaying)
				{
					stopwatch.Stop();

					Song.ToggleTimer(false);
					pausePlayButton.Icon(MaterialRoundedIcons.PlayArrow);
				}
				else
				{
					stopwatch.Start();

					Song.ToggleTimer(true);
					pausePlayButton.Icon(MaterialRoundedIcons.Pause);
				}
			}
			else
			{
				lineIndex = 0;
				previousLineIndex = -1;
				timestamp = TimeSpan.FromMilliseconds(e.Position).TotalSeconds;
				newTimestamp = e.Position;

				skipped = true;
			}

			// await Update(vocalGroups, lyricsEndTime, e.Position, 0, true, false);
		}

		protected override bool OnBackButtonPressed()
		{
			cancelToken.Cancel();
			cancelToken.Dispose();
#if ANDROID
			// SpotifyBroadcastReceiver.SongChanged -= OnSongChanged;
			SpotifyBroadcastReceiver.PlaybackChanged -= OnPlaybackChanged;
#endif

			stopwatch.Stop();
			// LyricsContainer.Clear();

			return base.OnBackButtonPressed();
		}

		private async void OnSongChanged(object sender, SongChangedEventArgs e)
		{
			//newSong = true;
			CurrentTrackId = e.Id;
			Song.Title = e.Name;
			Song.Artist = e.Artist;
			Song.Duration = e.Length;

			PlayerState playerThing = await RequestPositionSync();

			Song.Timestamp = playerThing.PlaybackPosition;

			if (playerThing != null)
			{
				Song.Image = $"https://i.scdn.co/image/{playerThing.Track.ImageUri.Raw.Split(':')[2]}";

				if (playerThing.IsPaused)
				{
					Song.ToggleTimer(false);
					pausePlayButton.Icon(MaterialRoundedIcons.PlayArrow);
				}
				else
				{
					Song.ToggleTimer(true);
					pausePlayButton.Icon(MaterialRoundedIcons.Pause);
				}
			}

			cancelToken.Cancel();
			stopwatch.Reset();

			LyricsContainer.Dispatcher.Dispatch(LyricsContainer.Children.Clear);

			// RenderLyrics().GetAwaiter().GetResult();
			Task.Run(RenderLyrics);
		}

		public async Task RenderLyrics()
		{
			try
			{
				while (Client == null) ;
#if ANDROID
				while (Remote == null) ;
#endif

				Shell.SetTabBarIsVisible(this, false);

				stopwatch.Start();

				SKBitmap image = null;

				// Background
				await Task.Run(async () =>
				{
					FullTrack track = await Spotify.Tracks.Get(CurrentTrackId);
					using HttpClient download = new HttpClient();

					try
					{
						var imageStream = await download.GetStreamAsync(track.Album.Images[0].Url);

						image = SKBitmap.Decode(imageStream);
					}
					catch (Exception ex)
					{
						// Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
					}
				});

				gridThing.Dispatcher.Dispatch(() =>
				{
					if (gridThing.Children.Any(x => x is BlobAnimationView))
						gridThing.Remove(gridThing.Children.First(x => x is BlobAnimationView));

					BlobAnimationView blobs = new BlobAnimationView(image, backgroundCancel)
					{
						HorizontalOptions = LayoutOptions.FillAndExpand,
						VerticalOptions = LayoutOptions.FillAndExpand,
						InputTransparent = true,
						ZIndex = -1
					};

					gridThing.Add(blobs);
					gridThing.SetRowSpan(blobs, 3);
				});

				string content = "";

				if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, $"{CurrentTrackId}.json")))
				{
					content = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, $"{CurrentTrackId}.json"));
					local = true;
				}
				else
				{
					RestResponse response = await Client.ExecuteAsync(new RestRequest(CurrentTrackId));

					if (!response.IsSuccessful)
					{
						MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(Platform.CurrentActivity, "Failed To Get Lyrics", ToastLength.Long).Show());
						return;
					}

					content = response.Content;
					local = false;
				}

				LoadLyrics(content);

#if ANDROID
				double startedSyncAt = stopwatch.ElapsedMilliseconds - (local ? 1000 : 800); // - 800;
				double[] syncTimings = [0.05, 0.1, 0.15, 0.75];
				double canSyncNonLocalTimestamp = isPlaying ? syncTimings.Length : 0;

				PlayerState player = await RequestPositionSync();
				long before = stopwatch.ElapsedMilliseconds;

				isPlaying = !player.IsPaused;
				IsPlaying = !player.IsPaused;
				timestamp = player.PlaybackPosition + startedSyncAt + (stopwatch.ElapsedMilliseconds - before);

				progressSyncTimer?.Close();
				progressSyncTimer = new System.Timers.Timer(10000);
				progressSyncTimer.Elapsed += OnTimerElapsed;
				// progressSyncTimer.Start();

				newSong = false;

				await Update(vocalGroups, lyricsEndTime, timestamp, ((double)1 / (double)60), true);

				lyricsDone = true;
				foreach (var item in LyricsContainer.Children.Where(x => x is FlexLayout))
				{
					foreach (var child in ((FlexLayout)item).Children)
					{
						if (child is GradientLabel label)
							label.IsVisible = true;
						else
						{
							foreach (var letter in ((HorizontalStackLayout)child).Children)
							{
								if (letter is GradientLabel letterLabel)
									letterLabel.IsVisible = true;
							}
						}
					}
				}

				await UpdateProgress(player.PlaybackPosition, startedSyncAt, vocalGroups, player.Track.Duration);
#endif

				// SpotifyBroadcastReceiver.SongChanged += (sender, e) => canSyncNonLocalTimestamp = syncTimings.Length;
				// SpotifyBroadcastReceiver.PlaybackChanged += (sender, e) => canSyncNonLocalTimestamp = isPlaying ? syncTimings.Length : 0;
			}
			catch (Exception ex)
			{
#if ANDROID
				Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
#endif
			}
		}

		private void LoadLyrics(string content)
		{
			JObject json = JObject.Parse(content);

			// type = content.Split('\"')[7];
			type = json["Type"].ToString();

			if (type == "Syllable")
			{
				SyllableSyncedLyrics providerLyrics = JsonConvert.DeserializeObject<SyllableSyncedLyrics>(content);

				TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
				{
					SyllableLyrics = providerLyrics
				});

				SyllableSyncedLyrics lyrics = transformedLyrics.Lyrics.SyllableLyrics;
				lyricsEndTime = lyrics.EndTime;
				int thing = 0;

				foreach (var vocalGroup in lyrics.Content)
				{
					if (vocalGroup is Interlude interlude)
					{
						// lines.Add(interlude);

						FlexLayout vocalGroupContainer = [];

						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						if (Preferences.Get("showInterludes", true))
							lines.Add(vocalGroupContainer);
						// LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Add(vocalGroupContainer));
					}
					else
					{
						SyllableVocalSet set = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());
						// lines.Add(set);

						// Add button

						if (set.Type == "Vocal")
						{
							string styleName = "IdleLyric";

							if (set.OppositeAligned)
								styleName = "IdleLyricOppositeAligned";

							VerticalStackLayout topGroup = new VerticalStackLayout();
							FlexLayout vocalGroupContainer = new FlexLayout();
							vocalGroupContainer.Style = Application.Current.Resources.MergedDictionaries.Last()[styleName] as Style;

							// topGroup.Dispatcher.Dispatch(() => topGroup.Children.Add(vocalGroupContainer));
							topGroup.Children.Add(vocalGroupContainer);
							// LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Add(topGroup));
							lines.Add(topGroup);

							List<SyllableVocals> vocals = [];
							double startTime = set.Lead.StartTime;
							vocals.Add(new SyllableVocals(vocalGroupContainer, set.Lead.Syllables, false, false, set.OppositeAligned));

							if (set.Background?.Count > 0)
							{
								FlexLayout backgroundVocalGroupContainer = new FlexLayout();
								backgroundVocalGroupContainer.Style = Application.Current.Resources.MergedDictionaries.Last()[$"Background{styleName}"] as Style;
								// topGroup.Dispatcher.Dispatch(() => topGroup.Children.Add(backgroundVocalGroupContainer));
								topGroup.Children.Add(backgroundVocalGroupContainer);

								foreach (var backgroundVocal in set.Background)
								{
									startTime = Math.Min(startTime, backgroundVocal.StartTime);
									vocals.Add(new SyllableVocals(backgroundVocalGroupContainer, backgroundVocal.Syllables, true, false, set.OppositeAligned));
								}
							}

							// Stupid piece of crap won't just accept the List of SyllableVocals, EVEN THOUGH IT INHERITS FROM ISyncedVocals
							List<ISyncedVocals> localVocals = [];
							localVocals.AddRange(vocals);

							vocalGroups.Add(vocalGroupContainer, localVocals);
							vocalGroupStartTimes.Add(startTime);
						}
					}
				}

				LyricsContainer.Dispatcher.Dispatch(() => lines.ForEach(LyricsContainer.Add));
			}
		}

		private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
		{
			syncProgress = true;
		}

		private async Task Update(Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime, double timestamp, double deltaTime, bool skipped = true, bool skippedByVocal = true)
		{
			if (newSong)
				return;

			double timestampToUse = timestamp;

			if (skipped)
			{
				timestampToUse = newTimestamp;
				this.skipped = false;
			}

			foreach (var vocalGroup in vocalGroups.Values)
			{
				foreach (var vocal in vocalGroup)
				{
					// timestampLabel.Dispatcher.Dispatch(() => timestampLabel.Text = $"Time: {timestamp}\nDelta Time: {deltaTime}");
					vocal.Animate(timestampToUse, deltaTime, skipped);

					// if(vocal is SyllableVocals syllable && syllable.IsActive())
					// 	ScrollViewer.Dispatcher.Dispatch(() => ScrollViewer.ScrollToAsync(syllable.Container, ScrollToPosition.Center, true));

					if (vocal is SyllableVocals syllable && syllable.IsActive() && deltaTime > 0)
					{
						int index = LyricsContainer.IndexOf(syllable.Container.Parent as IView);

						if (timestamp > syllable.StartTime && index != previousLineIndex)
						{
							// LyricsContainer.Dispatcher.Dispatch(async () => await scroller.ScrollAsync());
							ScrollViewer.Dispatcher.Dispatch(() => ScrollViewer.ScrollToAsync(syllable.Container, ScrollToPosition.Center, true));

							previousLineIndex = lineIndex;
							lineIndex = index;
						}
					}
				}
			}
		}

		public static double yTranslation = 0;

		private long lastUpdatedAt = stopwatch.ElapsedMilliseconds;

		private async Task UpdateProgress(long initialPosition, double startedSyncAt, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime)
		{
			if (newSong)
				return;

#if ANDROID
			double deltaTime = -1;
			long position = initialPosition;
			long updatedAt;

			position = initialPosition;
			updatedAt = stopwatch.ElapsedMilliseconds;
			// deltaTime = (updatedAt - lastUpdatedAt) / 1000;
			// deltaTime = (updatedAt - lastUpdatedAt).TotalMilliseconds / 1000;
			deltaTime = (stopwatch.Elapsed - TimeSpan.FromMilliseconds(lastUpdatedAt)).TotalSeconds;

			double newTimestamp = 0;
			double fireDeltaTime = deltaTime;

			if (skipped)
			{
				position = this.newTimestamp;
				resetOffset = stopwatch.ElapsedMilliseconds - 2500;
			}

			double syncedTimestamp = (position / 1000) + (startedSyncAt == 0 ? 0 : (updatedAt - startedSyncAt) / 1000) - (resetOffset / 1000);

			if (syncedTimestamp >= lyricsEndTime)
				return;

			// startedSyncAt = -1; idk why I did this
			// position = -1;

			if (isPlaying)
			{
				if (syncedTimestamp == 0 || Math.Abs(syncedTimestamp - timestamp) < 0.075)
				{
					newTimestamp = timestamp + deltaTime;
					fireDeltaTime = deltaTime;
				}
				else
					newTimestamp = syncedTimestamp;
			}
			else if (syncedTimestamp != 0 && Math.Abs(syncedTimestamp - timestamp) > 0.05)
			{
				newTimestamp = syncedTimestamp;
				fireDeltaTime = 0;
			}

			if (newTimestamp != 0 && isPlaying)
			{
				timestamp = newTimestamp;
				await Update(vocalGroups, lyricsEndTime, timestamp, deltaTime, skipped, false);
			}

			lastUpdatedAt = updatedAt;

			await Defer(async () => await UpdateProgress(position, startedSyncAt, vocalGroups, lyricsEndTime));
#endif
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

			if (backgroundCancel != null && !backgroundCancel.IsCancellationRequested)
				backgroundCancel.Cancel();
		}

#if ANDROID
		public static async Task<PlayerState> RequestPositionSync()
		{
			PlayerStateCallback callback = new PlayerStateCallback();
			Remote.PlayerApi?.SubscribeToPlayerState()?.SetEventCallback(callback);

			while (callback.PlayerState is null)
			{
				await Task.Delay(10);
			}

			return callback.PlayerState;
		}
#endif

		private async Task Defer(Func<Task> callback)
		{
			// await Task.Yield();
			// callback?.Invoke();

			// await Task.Delay(16).ContinueWith(_ => callback());
			await Task.Delay(5).ContinueWith(_ => callback());
		}

		private void OnPausePlay(object sender, EventArgs e)
		{
			if (isPlaying)
			{
#if ANDROID
				Remote.PlayerApi?.Pause();
#endif
			}
			else
			{
#if ANDROID
				Remote.PlayerApi?.Resume();
#endif
			}
		}

		private void SkipPrevious(object sender, EventArgs e)
		{
#if ANDROID
			Remote.PlayerApi?.SkipPrevious();
#endif
		}

		private void SkipNext(object sender, EventArgs e)
		{
#if ANDROID
			Remote.PlayerApi?.SkipNext();
#endif
		}

		private void SwitchToPlayerView(object sender, EventArgs e)
		{
			nowPlayingLyrics.IsVisible = false;
			nowPlayingFull.IsVisible = true;

			ScrollViewer.IsVisible = false;
		}

		private void SwitchToLyricsView(object sender, EventArgs e)
		{
			nowPlayingLyrics.IsVisible = true;
			nowPlayingFull.IsVisible = false;

			ScrollViewer.IsVisible = true;
		}*/
	}

	/*public class SongChangedEventArgs(string id, string name, string artist, string album, int length) : EventArgs
	{
		public string Id { get; set; } = id;

		public string Name { get; set; } = name;
		public string Artist { get; set; } = artist;
		public string Album { get; set; } = album;

		public int Length { get; set; } = length;
	}

	public class PlaybackChangedEventArgs(bool isPlaying, int position) : EventArgs
	{
		public bool IsPlaying { get; set; } = isPlaying;

		/// <summary>
		/// THe position of the song in milliseconds
		/// </summary>
		public int Position { get; set; } = position;
	}

	public class TimeSteppedEventArgs(double deltaTime, bool skipped = true) : EventArgs
	{
		public double DeltaTime { get; set; } = deltaTime;
		public bool Skipped { get; set; } = skipped;
	}

#if ANDROID
	public class PlayerStateCallback : Java.Lang.Object, IEventCallback
	{
		public PlayerState PlayerState { get; set; }

		public void OnEvent(Java.Lang.Object? p0)
		{
			if (p0 is PlayerState playerState)
			{
				PlayerState = playerState;
			}
		}
	}
#endif

	public class SongInformation
	{
		public string Title { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }
		public string Image { get; set; }
	}*/
}
