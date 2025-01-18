using Android.Widget;
using BeautifulLyricsAndroid.Entities;
using BeautifulLyricsMobile.Entities;
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Types;
using MauiIcons.Core;
using MauiIcons.Material.Rounded;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using SkiaSharp;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using static Com.Spotify.Protocol.Client.Subscription;
using System.Collections.Generic;
using System.Diagnostics;
using BeautifulLyricsMobile.Models;
using BeautifulLyricsMobile.Controls;
using System.Collections.Concurrent;
using Button = Microsoft.Maui.Controls.Button;
using Bumptech.Glide.Load.Resource.Gif;
using Java.Util.Concurrent;
using Com.Spotify.Protocol.Client;

namespace BeautifulLyricsMobile.Pages;

public delegate void SongChanged(object sender, SongChangedEventArgs e);
public delegate void PlaybackChanged(object sender, PlaybackChangedEventArgs e);
public delegate void TimeStepped(object sender, TimeSteppedEventArgs e);
public delegate void SpotifyConnected(object sender, SpotifyConnectedEventArgs e);

public partial class LyricsView : ContentView
{

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

	// public static event TimeStepped TimeStepped;

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

	private bool songLoaded = false;
	private bool hasLyrics = true;

	public SongViewModel Song { get; set; }
	private ConcurrentBag<Layout> lines = [];
	private static readonly object listLock = new object();

	// Syncing

	private CustomSyncedLyrics LyricsSave;

	private List<LineVocal> Vocals { get; set; }
	private SyllableVocal currentLine;

	private int selectedLineIndex = 0;
	private int selectedWordIndex = 0;
	private int cursorPosition = 0;

	public LyricsView()
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

		// Task.Run(RenderLyrics, cancelToken.Token);
#endif
	}

	public async void OnAppearing(SongChangedEventArgs e)
	{
		Song.Title = e.Name;
		Song.Artist = e.Artist;
		Song.Album = e.Album;
		Song.Duration = e.Length;

		await Task.Run(async () =>
		{
			while (Remote == null)
				await Task.Delay(10);

			PlayerState player = await RequestPositionSync();

			Song.Image = $"https://i.scdn.co/image/{player.Track.ImageUri.Raw.Split(':')[2]}";
			Song.Timestamp = player.PlaybackPosition;
		});
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

			if (songLoaded == true)
			{

			}

			skipped = true;
		}

		// await Update(vocalGroups, lyricsEndTime, e.Position, 0, true, false);
	}

	public bool OnBackButtonPressed()
	{
		// cancelToken.Cancel();
		// cancelToken.Dispose();
#if ANDROID
		// SpotifyBroadcastReceiver.SongChanged -= OnSongChanged;
		// SpotifyBroadcastReceiver.PlaybackChanged -= OnPlaybackChanged;
#endif

		stopwatch.Stop();
		// LyricsContainer.Clear();

		return false;
		// return base.OnBackButtonPressed();
	}

	private async void OnSongChanged(object sender, SongChangedEventArgs e)
	{
		if (syncingSong)
		{
			Remote.PlayerApi?.SkipPrevious();
			return;
		}

		//newSong = true;
		CurrentTrackId = e.Id;
		Song.Title = e.Name;
		Song.Artist = e.Artist;
		Song.Album = e.Album;
		Song.Duration = e.Length;

		cancelToken.Cancel();

		while (Remote == null)
			await Task.Delay(10);

		PlayerState player = await RequestPositionSync();

		Song.Image = $"https://i.scdn.co/image/{player.Track.ImageUri.Raw.Split(':')[2]}";
		Song.Timestamp = player.PlaybackPosition;

		if (player.IsPaused)
		{
			Song.ToggleTimer(false);
			pausePlayButton.Icon(MaterialRoundedIcons.PlayArrow);
			isPlaying = false;
		}
		else
		{
			Song.ToggleTimer(true);
			pausePlayButton.Icon(MaterialRoundedIcons.Pause);
			isPlaying = true;
		}

		stopwatch.Reset();
		// await LyricsContainer.Dispatcher.DispatchAsync(LyricsContainer.Children.Clear);
		lock (listLock)
		{
			vocalGroups.Clear();
			vocalGroupStartTimes.Clear();
			lines.Clear();
		}

		lastUpdatedAt = stopwatch.ElapsedMilliseconds;
		cancelToken?.Dispose();
		cancelToken = new CancellationTokenSource();

		// RenderLyrics().GetAwaiter().GetResult();
		await Task.Run(RenderLyrics, cancelToken.Token);
	}

	public async Task RenderLyrics()
	{
		try
		{
			stopwatch.Start();
			songLoaded = false;

			SKBitmap image = null;

			// Background
			// await Task.Run(async () =>
			// {
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
			// });

			gridThing.Dispatcher.Dispatch(() =>
			{
				BlobAnimationView blobs = new BlobAnimationView(image, backgroundCancel)
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					InputTransparent = true,
					ZIndex = -1
				};

				gridThing.Add(blobs);
				gridThing.SetRowSpan(blobs, 3);

				if (gridThing.Children.Where(x => x is BlobAnimationView).Count() == 2)
					gridThing.Remove(gridThing.Children.First(x => x is BlobAnimationView));
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

			if (string.IsNullOrWhiteSpace(content))
			{
				hasLyrics = false;
				lyricsButton.IsEnabled = false;

				nowPlayingFull.IsVisible = true;

				await Task.WhenAll
				(
					nowPlayingLyrics.FadeTo(0, 500, Easing.SpringOut),
					nowPlayingFull.FadeTo(100, 500, Easing.SpringOut)
				);

				nowPlayingLyrics.IsVisible = false;
				ScrollViewer.IsVisible = false;

				return;
			}
			else
			{
				hasLyrics = true;
				lyricsButton.IsEnabled = true;
			}

			LoadLyrics(content);

#if ANDROID
			double startedSyncAt = stopwatch.ElapsedMilliseconds - (local ? 1000 : 800); // - 800;
			double[] syncTimings = [0.05, 0.1, 0.15, 0.75];
			double canSyncNonLocalTimestamp = isPlaying ? syncTimings.Length : 0;

			PlayerState player = await RequestPositionSync();

			if (player.Track.Uri.Split(':')[2] != CurrentTrackId)
			{
				// Track has changed after we loaded the lyrics

				LyricsContainer.Dispatcher.Dispatch(() =>
				{
					lock (listLock)
					{
						LyricsContainer.Children.Clear();
						lines.Clear();
						vocalGroups.Clear();
						vocalGroupStartTimes.Clear();
					}

					cancelToken.Dispose();
					cancelToken = new CancellationTokenSource();
				});

				await Task.Run(RenderLyrics);
				return;
			}

			long before = stopwatch.ElapsedMilliseconds;

			isPlaying = !player.IsPaused;
			IsPlaying = !player.IsPaused;
			timestamp = player.PlaybackPosition + startedSyncAt + (stopwatch.ElapsedMilliseconds - before);

			progressSyncTimer?.Close();
			progressSyncTimer = new System.Timers.Timer(10000);
			progressSyncTimer.Elapsed += OnTimerElapsed;
			// progressSyncTimer.Start();

			newSong = false;

			await Update(cancelToken.Token, vocalGroups, lyricsEndTime, timestamp, ((double)1 / (double)60), true, true);

			songLoaded = true;

			await UpdateProgress(player.PlaybackPosition, startedSyncAt, vocalGroups, player.Track.Duration, cancelToken.Token);
#endif

			// SpotifyBroadcastReceiver.SongChanged += (sender, e) => canSyncNonLocalTimestamp = syncTimings.Length;
			// SpotifyBroadcastReceiver.PlaybackChanged += (sender, e) => canSyncNonLocalTimestamp = isPlaying ? syncTimings.Length : 0;
		}
		catch (Exception ex)
		{
#if ANDROID
			MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show());
#endif
		}
	}

	private void LoadLyrics(string content)
	{
		JObject json = JObject.Parse(content);
		ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.Last();

		// type = content.Split('\"')[7];
		type = json["Type"].ToString();

		if (type == "Syllable")
		{
			addLyricsButton.IsEnabled = false;
			lyricsButton.IsEnabled = true;

			try
			{
				SyllableSyncedLyrics providerLyrics = JsonConvert.DeserializeObject<SyllableSyncedLyrics>(content);

				TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
				{
					SyllableLyrics = providerLyrics
				});

				SyllableSyncedLyrics lyrics = transformedLyrics.Lyrics.SyllableLyrics;

				if (local)
					lyrics.Content = [.. lyrics.Content.Where(x => x is not Interlude)];

				lyricsEndTime = lyrics.EndTime;
				int thing = 0;

				foreach (var vocalGroup in lyrics.Content)
				{
					if (cancelToken.IsCancellationRequested)
					{
						lines.Clear();
						vocalGroups.Clear();
						vocalGroupStartTimes.Clear();

						return;
					}

					if (vocalGroup is Interlude interlude)
					{
						// lines.Add(interlude);

						FlexLayout vocalGroupContainer = [];

						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						lock (listLock)
						{
							lines.Add(vocalGroupContainer);
						}
						// LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Add(vocalGroupContainer));
					}
					else
					{
						SyllableVocalSet set = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());
						// lines.Add(set);

						// Add button

						if (set.Type == "Vocal")
						{
							// string styleName = "IdleLyric";
							string styleName = "LyricGroup";

							if (set.OppositeAligned)
								styleName = "LyricGroupOppositeAligned";
							// styleName = "IdleLyricOppositeAligned";

							VerticalStackLayout topGroup = new VerticalStackLayout();
							topGroup.Spacing = 0;
							FlexLayout vocalGroupContainer = new FlexLayout();
							vocalGroupContainer.Style = styles[styleName] as Style;

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
								backgroundVocalGroupContainer.Style = styles[$"{styleName}"] as Style;
								// topGroup.Dispatcher.Dispatch(() => topGroup.Children.Add(backgroundVocalGroupContainer));
								topGroup.Children.Add(backgroundVocalGroupContainer);

								foreach (var backgroundVocal in set.Background)
								{
									startTime = Math.Min(startTime, backgroundVocal.StartTime);

									lock (listLock)
									{
										vocals.Add(new SyllableVocals(backgroundVocalGroupContainer, backgroundVocal.Syllables, true, false, set.OppositeAligned));
									}
								}
							}

							// Stupid piece of crap won't just accept the List of SyllableVocals, EVEN THOUGH IT INHERITS FROM ISyncedVocals
							lock (listLock)
							{

								List<ISyncedVocals> localVocals = [];
								localVocals.AddRange(vocals);

								vocalGroups.Add(vocalGroupContainer, localVocals);
								vocalGroupStartTimes.Add(startTime);
							}
						}
					}
				}
			}
			finally
			{
				// LyricsContainer.Dispatcher.Dispatch(() => lines.ForEach(LyricsContainer.Add));
				LyricsContainer.Dispatcher.Dispatch(() =>
				{
					lock (listLock)
					{

						var newLines = lines.ToList();

						try
						{
							LyricsContainer.Clear();

							newLines.Reverse();
						}
						finally
						{
							foreach (var line in newLines)
							{
								LyricsContainer.Add(line);
							}
						}
					}
				});
			}
		}
		else
		{
			addLyricsButton.IsEnabled = true;
			lyricsButton.IsEnabled = false;
		}
	}

	private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		syncProgress = true;
	}

	private async Task Update(CancellationToken cancel, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime, double timestamp, double deltaTime, bool skipped = true, bool skippedByVocal = true)
	{
		if (cancel.IsCancellationRequested)
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

				if (vocal is SyllableVocals syllable && syllable.IsActive() && deltaTime > 0 && isPlaying && hasLyrics)
				{
					int index = LyricsContainer.IndexOf(syllable.Container.Parent as IView);

					if (timestamp > syllable.StartTime && index != previousLineIndex)
					{
						// LyricsContainer.Dispatcher.Dispatch(async () => await scroller.ScrollAsync());
						ScrollViewer.Dispatcher.Dispatch(() =>
						{
							if (cancel.IsCancellationRequested) return;

							ScrollViewer.ScrollToAsync(syllable.Container, ScrollToPosition.Center, true);
						});

						previousLineIndex = lineIndex;
						lineIndex = index;
					}
				}
			}
		}
	}

	public static double yTranslation = 0;

	private long lastUpdatedAt = stopwatch.ElapsedMilliseconds;

	private async Task UpdateProgress(long initialPosition, double startedSyncAt, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime, CancellationToken cancel)
	{
		if (cancel.IsCancellationRequested)
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
			await Update(cancel, vocalGroups, lyricsEndTime, timestamp, deltaTime, skipped, false);
		}

		lastUpdatedAt = updatedAt;

		await Defer(async () => await UpdateProgress(position, startedSyncAt, vocalGroups, lyricsEndTime, cancel));
#endif
	}

	protected void OnDisappearing()
	{
		// base.OnDisappearing();

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
		await Task.Delay(32).ContinueWith(_ => callback());
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

	private async void SwitchToPlayerView(object sender, EventArgs e)
	{
		nowPlayingFull.IsVisible = true;

		await Task.WhenAll
		(
			nowPlayingFull.FadeTo(100, 500, Easing.SpringOut),
			nowPlayingLyrics.FadeTo(0, 500, Easing.SpringOut)
		);

		nowPlayingLyrics.IsVisible = false;
		ScrollViewer.IsVisible = false;
	}

	private async void SwitchToLyricsView(object sender, EventArgs e)
	{
		nowPlayingLyrics.IsVisible = true;

		await Task.WhenAll
		(
			nowPlayingLyrics.FadeTo(100, 500, Easing.SpringOut),
			nowPlayingFull.FadeTo(0, 500, Easing.SpringOut)
		);

		nowPlayingFull.IsVisible = false;
		ScrollViewer.IsVisible = true;
	}

	private async void AddLyrics(object sender, EventArgs e)
	{
		nowPlayingLyrics.IsVisible = true;

		await Task.WhenAll
		(
			nowPlayingLyrics.FadeTo(100, 500, Easing.SpringOut),
			nowPlayingFull.FadeTo(0, 500, Easing.SpringOut)
		);

		nowPlayingFull.IsVisible = false;
		ScrollViewer.IsVisible = true;

		await Task.Run(RenderSyncLyrics);
	}

	// Splitting
	private async Task RenderSyncLyrics()
	{
		// This feels wrong
		LyricsSave = new CustomSyncedLyrics([]);
		var response = await Client.ExecuteAsync(new RestRequest(CurrentTrackId));
		var styles = Application.Current.Resources.MergedDictionaries.Last();

		if (!response.IsSuccessful)
		{
			MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(Platform.CurrentActivity, "Failed To Get Lyrics", ToastLength.Long).Show());
			return;
		}

		List<LineVocal> lines = [];
		List<Layout> lineGroups = [];

		LineSyncedLyrics lineVocals = JsonConvert.DeserializeObject<LineSyncedLyrics>(response.Content);

		TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
		{
			LineLyrics = lineVocals
		});

		LineSyncedLyrics lyrics = transformedLyrics.Lyrics.LineLyrics;
		Vocals = lyrics.Content.Where(x => x is LineVocal).Select(x => x as LineVocal).ToList();

		WordPopup popup = new WordPopup
		{
			ZIndex = 2
		};

		foreach (var item in lyrics.Content)
		{
			if (item is LineVocal vocal)
			{
				FlexLayout lineGroup = new FlexLayout()
				{
					Style = styles[vocal.OppositeAligned ? "LyricGroupOppositeAligned" : "LyricGroup"] as Style,
					InputTransparent = false
				};

				SyllableVocalSet set = new SyllableVocalSet
				{
					Type = "Vocal",
					OppositeAligned = vocal.OppositeAligned,
					Lead = new SyllableVocal
					{
						Syllables = []
					}
				};

				string leadVocals = vocal.Text;
				string? backgroundVocals = null;

				if (vocal.Text.Contains('('))
				{
					set.Background = [];

					var split = leadVocals.Split('(');
					var splitAfer = leadVocals.Split(')');

					if (split.Length == 2)
					{
						string lineBefore = split[1];
						backgroundVocals = lineBefore.Split(')')[0];

						string outputBefore = split[0].Trim();
						string outputAfter = splitAfer[1];

						leadVocals = $"{outputBefore} {outputAfter}".Trim();
					}
					else
					{
						// This only supports up to two sets of parentheses, Look What You Made Me Do by Taylor Swift has three, same with Say Don't Go (on MuxicMatch at least)

						// (Hey! I don't knwo about you (I don't know about you) but I'm feeling 22

						string first = split[1];
						string second = first.Split(')')[0]; // Hey!
						string third = split[2];
						string fourth = third.Split(')')[0]; // I don't know about you

						backgroundVocals = $"{second} {fourth}"; // Hey! I don't know about you

						string leadFirst = split[0].Trim(); // ""
						string leadSecond = splitAfer[1]; // I don't know about you (I don't know about you
						string leadThird = splitAfer[2]; // but I'm feeling 22

						leadVocals = $"{leadFirst} {second.Split('(')[0]} {third}".Trim();
					}
				}

				foreach (var word in leadVocals.Split(' '))
				{
					var button = new Button
					{
						Text = word,
						BackgroundColor = Colors.Transparent,
						TextColor = Colors.White,
						Style = styles["LyricLabel"] as Style,
						HorizontalOptions = LayoutOptions.Start
					};

					button.Clicked += (sender, e) =>
					{
						Button self = sender as Button;

						selectedLineIndex = LyricsContainer.IndexOf(LyricsContainer.Children.FirstOrDefault(x => x is FlexLayout flex && flex.Children.Any(x => x as Button == self)));
						selectedWordIndex = ((FlexLayout)LyricsContainer.Children[selectedLineIndex]).IndexOf(self);
						selectedLineIndex--;

						popup.SetWord(self.Text);

						popup.Canceled += (s, e) =>
						{
							selectedLineIndex = 0;
							selectedWordIndex = 0;

							gridThing.Remove(popup);
						};

						popup.Finished += (s, e) =>
						{
							SyllableVocalSet set = LyricsSave.Lines[selectedLineIndex];
							SyllableMetadata metadata = set.Lead.Syllables[selectedWordIndex]; // If you split multiple words in the same line, it breaks because you just added a ton of syllables
							string word = metadata.Text;

							List<SyllableMetadata> parts = [];
							int start = 0;

							foreach (var index in popup.splits)
							{
								parts.Add(new SyllableMetadata
								{
									Text = word[start..index],
									IsPartOfWord = true
								});
							}

							parts.Add(new SyllableMetadata
							{
								Text = word[start..],
								IsPartOfWord = false
							});

							set.Lead.Syllables.RemoveAt(selectedWordIndex);
							set.Lead.Syllables.InsertRange(selectedWordIndex, parts);

							selectedLineIndex = 0;
							selectedWordIndex = 0;

							gridThing.Remove(popup);
						};

						gridThing.Add(popup);
						//WordPopup thingPopup = new WordPopup();

						// gridThing.Add(thingPopup);
						gridThing.SetRow(popup, 1);

						// WordPopup.IsVisible = true;
						// WordPopup.IsEnabled = true;
					};

					lineGroup.Add(button);

					set.Lead.Syllables.Add(new SyllableMetadata
					{
						Text = word,
						IsPartOfWord = false
					});
				}

				if (backgroundVocals != null)
				{
					foreach (var word in backgroundVocals.Split(' '))
					{
						set.Background.Add(new SyllableVocal
						{
							Syllables = [new SyllableMetadata
								{
									Text = word,
									IsPartOfWord = false
								}]
						});
					}
				}

				LyricsSave.Lines.Add(set);
				lineGroups.Add(lineGroup);
			}
		}

		await LyricsContainer.Dispatcher.DispatchAsync(() => lineGroups.ForEach(LyricsContainer.Add));

		Button finish = new Button
		{
			Text = "Finish",
			VerticalOptions = LayoutOptions.End
		};

		finish.Clicked += async (sender, e) =>
		{
			foreach (var line in LyricsSave.Lines)
			{
				foreach (var word in line.Lead.Syllables)
				{
					if (word.Splits?.Count > 0)
					{
						string text = word.Text;

						List<SyllableMetadata> parts = [];
						int start = 0;

						foreach (var index in popup.splits)
						{
							parts.Add(new SyllableMetadata
							{
								Text = text[start..index],
								IsPartOfWord = true
							});

							start = index;
						}

						parts.Add(new SyllableMetadata
						{
							Text = text[start..],
							IsPartOfWord = false
						});

						int wordIndex = line.Lead.Syllables.IndexOf(word);
						line.Lead.Syllables.RemoveAt(wordIndex);
						line.Lead.Syllables.InsertRange(wordIndex, parts);
					}
				}
			}

			await LyricsContainer.Dispatcher.DispatchAsync(() =>
			{
				LyricsContainer.Children.Clear();
				gridThing.Remove(finish);
			});

			await Task.Run(RenderLyricsAdvancedSync);
		};

		gridThing.Dispatcher.Dispatch(() =>
		{
			gridThing.Add(finish);
			gridThing.SetRow(finish, 2);
		});
		// LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Add(finish));

		MainThread.BeginInvokeOnMainThread(() => CommunityToolkit.Maui.Alerts.Toast.Make("This does not work right now, please press Finish", CommunityToolkit.Maui.Core.ToastDuration.Long).Show());
	}

	int lineCount = 0;
	bool started = false;
	int wordIndex = 0;
	int index = 0;
	double startTime = 0;
	bool syncingSong = false;

	// Tapping
	private async Task RenderLyricsAdvancedSync()
	{
		syncingSong = true;
		lineCount = LyricsSave.Lines.Count;
		var styles = Application.Current.Resources.MergedDictionaries.Last();
		List<Layout> lines = [];

		foreach (var item in LyricsSave.Lines)
		{
			if (item is SyllableVocalSet vocal)
			{
				FlexLayout lineGroup = new FlexLayout
				{
					Style = styles[vocal.OppositeAligned ? "LyricGroupOppositeAligned" : "LyricGroup"] as Style,
					InputTransparent = true
				};

				foreach (var word in vocal.Lead.Syllables)
				{
					lineGroup.Add(new Label
					{
						Text = word.Text,
						Style = word.IsPartOfWord ? styles["LyricEmphasizedLabel"] as Style : styles["LyricLabel"] as Style,
						InputTransparent = true,
						TextColor = new Color(224, 224, 224, 0.5f)
					});
				}

				lines.Add(lineGroup);
			}
		}

		await LyricsContainer.Dispatcher.DispatchAsync(() => lines.ForEach(LyricsContainer.Add));

		started = true;

		stopwatch.Reset();

#if ANDROID
		Remote.PlayerApi?.Resume();
		Remote.PlayerApi?.SeekTo(0);
#endif

		stopwatch.Start();
	}

	private void OnScreenDown(object sender, EventArgs e)
	{
		if (!started) return;

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;

		if (wordIndex == 0)
		{
			foreach (var word in container.Children)
			{
				if (word is Label labelWord)
					labelWord.TextColor = new Color(224, 224, 224);
				// labelWord.TextColor = Colors.White.WithAlpha(1f);
			}
		}


		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		if (index == 0)
			startTime = seconds;

		SyllableVocal current = LyricsSave.Lines[lineIndex].Lead;

		if (wordIndex == 0)
			current.StartTime = seconds;

		current.Syllables[wordIndex].StartTime = seconds;

		label.TextColor = Colors.White;
		label.ScaleTo(1.05f, 250, Easing.SpringOut);
		label.TranslateTo(0, 1, 250, Easing.SpringOut);
		label.Shadow = new Shadow
		{
			Brush = Brush.White,
			Opacity = 0.4f
		};
	}

	private void OnScreenRelease(object sender, EventArgs e)
	{
		if (!started) return;

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;

		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		LyricsSave.Lines[lineIndex].Lead.Syllables[wordIndex].EndTime = seconds;

		label.ScaleTo(1, 250, Easing.SpringOut);
		label.TranslateTo(0, 0, 250, Easing.SpringOut);
		label.Shadow = null;

		wordIndex++;
		index++; // We're literally only using this in one spot

		// We've reached the last word in the line
		if (wordIndex == container.Children.Count)
		{
			wordIndex = 0;
			lineIndex++;

			// We've reached the end of the song
			if (lineIndex == lineCount)
			{
				try
				{
					var song = new SyllableSyncedLyrics
					{
						StartTime = startTime,
						EndTime = seconds,
						Content = [.. LyricsSave.Lines]
					};

					song.Content = [.. song.Content.Where(x => x is not Interlude)];

					File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, $"{CurrentTrackId}.json"), JsonConvert.SerializeObject(song));

					CommunityToolkit.Maui.Alerts.Toast.Make("Lyrics Saved!").Show();

					LyricsContainer.Children.Clear();

					syncingSong = false;
					Remote.PlayerApi?.SeekTo(0);
					Task.Run(RenderLyrics);
				}
				catch (Exception ex)
				{
					CommunityToolkit.Maui.Alerts.Toast.Make(ex.Message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
				}
			}
			else
			{
				ScrollViewer.ScrollToAsync(LyricsContainer.Children[lineIndex] as FlexLayout, ScrollToPosition.Center, true);

				foreach (var word in container.Children)
				{
					if (word is Label labelWord)
						labelWord.TextColor = new Color(224, 224, 224, 0.75f);
					//labelWord.TextColor = Colors.White.WithAlpha(0.75f);
				}
			}
		}
	}

	private async void MoreOptionButton(object sender, EventArgs e)
	{
		SongMoreOptionsSheet sheet = new SongMoreOptionsSheet();

		sheet.Delete += (s, e) =>
		{
			string path = Path.Combine(FileSystem.AppDataDirectory, $"{CurrentTrackId}.json");

			if (File.Exists(path))
			{
				File.Delete(path);
				CommunityToolkit.Maui.Alerts.Toast.Make("Song Deleted!").Show();
			}
			else
				CommunityToolkit.Maui.Alerts.Toast.Make("No Local Lyrics Found!").Show();
		};

		sheet.Queue += (s, e) =>
		{
			Remote?.PlayerApi?.Queue($"spotify:track:{CurrentTrackId}");
		};

		await sheet.ShowAsync();
	}
}

public class SongChangedEventArgs(string id, string name, string artist, string album, int length) : EventArgs
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

public class SpotifyConnectedEventArgs(SpotifyAppRemote remote) : EventArgs
{
	public SpotifyAppRemote Remote { get; set; } = remote;
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
}
