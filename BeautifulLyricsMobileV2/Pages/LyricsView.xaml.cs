#if ANDROID
using Android.Media;
#endif

using BeautifulLyricsMobileV2.Controls;
using BeautifulLyricsMobileV2.Entities;
using BeautifulLyricsMobileV2.PageModels;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;

namespace BeautifulLyricsMobileV2.Pages;

public partial class LyricsView : ContentView
{
	public LyricsViewModel Song { get; set; }
	private string Token { get; }

	private Task currentTask = Task.CompletedTask;
	private CancellationTokenSource? cancelSource;

	private static Stopwatch stopwatch = new Stopwatch();

	private readonly Lock skipLock = new Lock();
	private bool skipPending = false;

	public LyricsView()
	{
		InitializeComponent();
		BindingContext = Song;
		Token = SecureStorage.GetAsync("token").GetAwaiter().GetResult();

		// Spotify no longer sends a song changed event on app startup for some reason :(
		var task = Task.Run(async () =>
		{
			await Task.Delay(3000);
			SpotifyPlayerState player = await Song.Remote.GetPlayerState();

			await UpdateSong(player.Track);
		});

#if ANDROID
		SpotifyBroadcastReceiver.SongChanged += async (s, e) =>
		{
			lock (skipLock) { skipPending = true; }

			await Task.Delay(200);
			bool shouldSwitch;

			lock (skipLock)
			{
				shouldSwitch = skipPending;
				skipPending = false;
			}

			if (shouldSwitch)
			{
				SpotifyPlayerState player = await Song.Remote.GetPlayerState();

				await UpdateSong(player.Track);
			}
		};
#endif
	}

	private async Task UpdateSong(SpotifyTrack track)
	{
		Song.Title = track.Title;
		Song.Artist = track.Artist.Name;
		Song.Album = track.Album.Title;
		Song.Image = $"https://i.scdn.co/image/{track.Image.Split(':')[2]}";
		Song.Duration = (int)track.Duration;
		Song.Id = track.Id;

		cancelSource?.Cancel();
		try { await currentTask; }
		catch (OperationCanceledException) { }

		ResetLyrics();

		//currentTask = Task.Run(async () => await RenderLyrics(cancelSource.Token), cancelSource.Token);
		currentTask = RenderLyrics(cancelSource.Token);
	}

	private async Task RenderLyrics(CancellationToken cancel)
	{
		Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups = [];
		List<double> vocalGroupStartTimes = [];
		List<Layout> lines = [];
		double lyricsEndTime = -1;

		cancel.ThrowIfCancellationRequested();

		try
		{
			// Background
			_ = Task.Run(async () =>
			{
				SKBitmap image = null;

				try
				{
					using HttpClient download = new HttpClient();
					var imageStream = await download.GetStreamAsync(Song.Image);
					image = SKBitmap.Decode(imageStream);
				}
				catch(Exception)
				{
					await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Failed To Get Image").Show());
					return;
				}

				await backgroundGrid.Dispatcher.DispatchAsync(() =>
				{
					// Maybe make it just switch the image instead of removing it and making a new one?
					if(backgroundGrid.Children.Any(x => x is BackgroundAnimationView))
					{
						BackgroundAnimationView backgroundToRemove = backgroundGrid.Children.FirstOrDefault(x => x is BackgroundAnimationView) as BackgroundAnimationView;
						backgroundToRemove?.Dispose();
						backgroundGrid.Remove(backgroundToRemove);
					}

					BackgroundAnimationView background = new BackgroundAnimationView(image, cancel)
					{
						HorizontalOptions = LayoutOptions.FillAndExpand,
						VerticalOptions = LayoutOptions.FillAndExpand,
						ZIndex = -1
					};

					backgroundGrid.Add(background);
					backgroundGrid.SetRowSpan(background, 3);
				});
			});

			string content = "";

			if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, $"{Song.Id}.json")))
				content = await File.ReadAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, $"{Song.Id}.json"), cancel);
			else
			{
				using HttpClient client = new HttpClient
				{
					BaseAddress = new Uri("https://beautiful-lyrics.socalifornian.live/lyrics/")
				};

				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

				HttpResponseMessage response = await client.GetAsync(Song.Id, cancel);

				if (!response.IsSuccessStatusCode)
				{
					await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Failed To Get Lyrics").Show());
					return;
				}

				content = await response.Content.ReadAsStringAsync(cancel);
			}

			// Loading Lyrics

			JObject json = JObject.Parse(content);
			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.First();

			string type = json["Type"].ToString();

			if (type == "Syllable")
			{
				cancel.ThrowIfCancellationRequested();

				SyllableSyncedLyrics providerLyrics = JsonConvert.DeserializeObject<SyllableSyncedLyrics>(content);

				TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
				{
					SyllableLyrics = providerLyrics
				});

				SyllableSyncedLyrics lyrics = transformedLyrics.Lyrics.SyllableLyrics;
				lyricsEndTime = lyrics.EndTime;

				foreach (var vocalGroup in lyrics.Content) // Maybe make enumerable?
				{
					cancel.ThrowIfCancellationRequested();

					if (vocalGroup is Interlude interlude)
					{
						FlexLayout vocalGroupContainer = [];

						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						lines.Add(vocalGroupContainer);
					}
					else
					{
						SyllableVocalSet set = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());

						if (set.Type == "Vocal")
						{
							string styleName = set.OppositeAligned ? "LyricGroupOppositeAligned" : "LyricGroup";

							VerticalStackLayout topGroup = [];
							FlexLayout vocalGroupContainer = [];
							vocalGroupContainer.Style = styles[styleName] as Style;

							topGroup.Children.Add(vocalGroupContainer);
							lines.Add(topGroup);

							List<SyllableVocals> vocals = [];
							double startTime = set.Lead.StartTime;

							SyllableVocals sv = new SyllableVocals(vocalGroupContainer, set.Lead.Syllables, false, false, set.OppositeAligned);
							sv.ActivityChanged += async (s, e) => await ScrollViewer.Dispatcher.DispatchAsync(async () => await ScrollViewer.ScrollToAsync(e, ScrollToPosition.Center, true));

							vocals.Add(sv);

							if (set.Background?.Count > 0)
							{
								FlexLayout backgroundVocalGroupContainer = [];
								backgroundVocalGroupContainer.Style = styles[styleName] as Style; // Not background style?
								topGroup.Children.Add(backgroundVocalGroupContainer);

								foreach (var backgroundVocal in set.Background)
								{
									startTime = Math.Min(startTime, backgroundVocal.StartTime);
									vocals.Add(new SyllableVocals(backgroundVocalGroupContainer, backgroundVocal.Syllables, true, false, set.OppositeAligned));
								}
							}

							vocalGroups.Add(vocalGroupContainer, [.. vocals]);
							vocalGroupStartTimes.Add(startTime);
						}
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			//ResetLyrics();
		}
		finally
		{
			await lyricsContainer.Dispatcher.DispatchAsync(() =>
			{
				lyricsContainer.Clear();
				lines.ForEach(lyricsContainer.Add);
			});
		}

		SpotifyPlayerState player = await Song.Remote.GetPlayerState();
		Song.IsPlaying = !player.IsPaused;
		stopwatch.Restart();

		await Update(cancel, vocalGroups, player.PlaybackPosition / 1000d, 1.0 / 60, true);

		await UpdateProgress(player.PlaybackPosition, stopwatch.ElapsedMilliseconds, vocalGroups, lyricsEndTime, cancel);
	}

	private async Task Update(CancellationToken cancel, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double timestamp, double deltaTime, bool skipped = true)
	{
		try
		{
			cancel.ThrowIfCancellationRequested();

			foreach (var vocalGroup in vocalGroups.Values.ToList())
			{
				foreach (var vocal in vocalGroup)
				{
					cancel.ThrowIfCancellationRequested();
					vocal.Animate(timestamp, deltaTime, skipped);
				}
			}
		}
		catch (OperationCanceledException)
		{
			//ResetLyrics();
		}
	}

	private long lastUpdatedAt = 0;

	private async Task UpdateProgress(long initialPosition, double startedSyncAt, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime, CancellationToken cancel)
	{
		try
		{
			int[] syncTimings = [50, 100, 150, 750];
			int syncIndex = 0;
			long nextSyncAt = syncTimings[0];

			while (!cancel.IsCancellationRequested)
			{
				long updatedAt = stopwatch.ElapsedMilliseconds;

				if (Song.IsPlaying)
				{
					if (updatedAt >= startedSyncAt + nextSyncAt)
					{
						SpotifyPlayerState player = await Song.Remote.GetPlayerState();
						long spotifyTimestamp = player.PlaybackPosition;

						initialPosition = spotifyTimestamp;
						startedSyncAt = updatedAt;

						syncIndex++;

						if (syncIndex < syncTimings.Length)
							nextSyncAt = syncTimings[syncIndex];
						else
							nextSyncAt = 33;
					}

					double syncedTimestamp = (initialPosition + (updatedAt - startedSyncAt)) / 1000d;
					double deltaTime = (updatedAt - lastUpdatedAt) / 1000d;

					await Update(cancel, vocalGroups, syncedTimestamp, deltaTime, false);
				}

				lastUpdatedAt = updatedAt;
				await Task.Delay(16, cancel);
			}

			//ResetLyrics();
		}
		catch (OperationCanceledException)
		{
			//ResetLyrics();
		}
	}

	// 33 is 30 FPS
	// 16 is 60 FPS
	private async Task Defer(Func<Task> callback) => await Task.Delay(16).ContinueWith(_ => callback());

	private void ResetLyrics()
	{
		stopwatch.Restart();
		lastUpdatedAt = 0;

		cancelSource?.Dispose();
		cancelSource = new CancellationTokenSource();

		// lyricsContainer.Children.Clear();
	}
}