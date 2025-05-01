using BeautifulLyricsMobileV2.Entities;
using BeautifulLyricsMobileV2.PageModels;
using CommunityToolkit.Maui.Alerts;
using MauiIcons.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.Diagnostics;
using System.Net;

namespace BeautifulLyricsMobileV2.Pages;

public partial class LyricsView : ContentView
{
	public LyricsViewModel Song { get; set; }
	private string Token { get; }

	private Task currentTask = Task.CompletedTask;
	private static CancellationTokenSource? cancelSource;

	private static Stopwatch stopwatch = new Stopwatch();
	private static Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups;
	private static bool appeared = false;

	public LyricsView()
	{
		InitializeComponent();
		_ = new MauiIcon();
		//Token = SecureStorage.GetAsync("token").GetAwaiter().GetResult();

#if ANDROID
		SpotifyBroadcastReceiver.SongChanged += async (s, e) =>
		{
			SpotifyPlayerState? player = await TryGetPlayerState();

			if (player?.Track == null)
			{
				Debug.WriteLine("Failed to get current track");
				return;
			}

			await UpdateSong(player.Track);
		};

		SpotifyBroadcastReceiver.PlaybackChanged += (s, e) => Song.IsPlaying = e.IsPlaying;
#endif
		vocalGroups = new Dictionary<FlexLayout, List<ISyncedVocals>>();
	}

	private async Task<SpotifyPlayerState?> TryGetPlayerState(int maxRetries = 3, int delay = 200)
	{
		for (int i = 0; i < maxRetries; i++)
		{
			SpotifyPlayerState player = await Song!.Remote.GetPlayerState();

			if (player?.Track != null)
				return player;

			await Task.Delay(delay);
		}

		return null;
	}

	// Spotify no longer sends a song changed event on app startup for some reason :(
	public void OnAppearing()
	{
		if (appeared) return;

		var task = Task.Run(async () =>
		{
			SpotifyPlayerState? player = await TryGetPlayerState();

			if (player?.Track == null)
			{
				Debug.WriteLine("Failed to get current track");
				return;
			}

			await UpdateSong(player.Track);
		});

		backgroundVisual.Initalize();

		Song.Remote.Resumed += (s, e) =>
		{
			Song.Remote.Connected += async (sender, eC) =>
			{
#if ANDROID
				SpotifyBroadcastReceiver.SongChanged += async (s, e) =>
				{
					SpotifyPlayerState? player = await TryGetPlayerState();

					if (player?.Track == null)
					{
						Debug.WriteLine("Failed to get current track");
						return;
					}

					await UpdateSong(player.Track);
				};
#endif

				SpotifyPlayerState? player = await TryGetPlayerState();
				if (player == null) return;

				if (player.Track.Id != Song.Track.Id)
					await UpdateSong(player.Track, true);
				else
				{
					double position = player.PlaybackPosition / 1000d;

					await Update(cancelSource.Token, vocalGroups, position, 1.0 / 60, true);
					await UpdateProgress(player.PlaybackPosition, stopwatch.ElapsedMilliseconds, vocalGroups, cancelSource.Token);
				}
			};
		};

		appeared = true;
	}

	private async Task UpdateSong(SpotifyTrack track, bool returning = false)
	{
		Song.Track = track;

		SpotifyLibraryState state = await Song.Remote.GetLibraryState(Song.Track.Id);
		Song.Saved = state.IsAdded;

		cancelSource?.Cancel();

		if (!returning)
		{
			try { await currentTask; }
			catch (OperationCanceledException) { }
			catch (WebException) { }
		}

		ResetLyrics();

		currentTask = RenderLyrics(cancelSource.Token);
	}

	private async Task RenderLyrics(CancellationToken cancel)
	{
		List<double> vocalGroupStartTimes = [];
		List<Layout> lines = [];
		double lyricsEndTime = -1;
		bool staticLyrics = false;

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
					var imageStream = await download.GetStreamAsync(Song.Track.Image, cancel);
					image = SKBitmap.Decode(imageStream);
				}
				catch (WebException webEx) when (webEx.Status == WebExceptionStatus.RequestCanceled)
				{
					throw new OperationCanceledException(webEx.Message, webEx, cancel);
				}
				catch (Exception)
				{
					await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Failed To Get Image").Show());
					return;
				}

				await backgroundGrid.Dispatcher.DispatchAsync(() =>
				{
					backgroundVisual.UpdateImage(image, cancel);
				});
			}, cancel);

			string content = "";

			try
			{
				if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, $"{Song.Track.Id}.json")))
					content = await File.ReadAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, $"{Song.Track.Id}.json"), cancel);
				else
				{
					using HttpClient client = new HttpClient
					{
						BaseAddress = new Uri("https://beautiful-lyrics.socalifornian.live/lyrics/")
					};

					client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Song.Remote.Token);

					HttpResponseMessage response = await client.GetAsync(Song.Track.Id, cancel);

					if (!response.IsSuccessStatusCode)
					{
						await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Failed To Get Lyrics").Show());
						return;
					}

					content = await response.Content.ReadAsStringAsync(cancel);
				}
			}
			catch (TaskCanceledException)
			{
				throw new OperationCanceledException(cancel);
			}

			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.First();

			if (string.IsNullOrWhiteSpace(content.Trim()))
			{
				// No lyrics!
				lyricsContainer.Dispatcher.Dispatch(() =>
				{
					lyricsContainer.Add(new Label
					{
						Text = "This song has no lyrics!",
						HorizontalOptions = LayoutOptions.Center,
						Style = styles["LyricLabel"] as Style
					});
				});

				return;
			}

			// Loading Lyrics
			JObject json = JObject.Parse(content);
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

				int i = 0;
				foreach (var vocalGroup in lyrics.Content) // Maybe make enumerable?
				{
					cancel.ThrowIfCancellationRequested();

					if (vocalGroup is Interlude interlude)
					{
						FlexLayout vocalGroupContainer = [];
						if (interlude.Time.StartTime == 0) vocalGroupContainer.Margin = new Thickness(0, 10, 0, 0);

						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						// Check opposite alignment

						if (i != 0 && i != lyrics.Content.Count - 1 && ((SyllableVocalSet)lyrics.Content[i - 1]).OppositeAligned && ((SyllableVocalSet)lyrics.Content[i + 1]).OppositeAligned)
							vocalGroupContainer.HorizontalOptions = LayoutOptions.End;

						lines.Add(vocalGroupContainer);
					}
					else
					{
						// SyllableVocalSet set = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());

						if (vocalGroup is SyllableVocalSet set)
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
								backgroundVocalGroupContainer.Style = styles[styleName] as Style;
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

					i++;
				}
			}
			else if (type == "Line")
			{
				cancel.ThrowIfCancellationRequested();

				LineSyncedLyrics providerLyrics = JsonConvert.DeserializeObject<LineSyncedLyrics>(content);

				TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
				{
					LineLyrics = providerLyrics
				});

				LineSyncedLyrics lyrics = transformedLyrics.Lyrics.LineLyrics;
				lyricsEndTime = lyrics.EndTime;

				int i = 0;
				foreach (var vocalGroup in lyrics.Content)
				{
					if (vocalGroup is Interlude interlude)
					{
						FlexLayout vocalGroupContainer = [];
						if (interlude.Time.StartTime == 0) vocalGroupContainer.Margin = new Thickness(0, 10, 0, 0);


						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						if (i != 0 && i != lyrics.Content.Count - 1 && ((LineVocal)(lyrics.Content[i - 1])).OppositeAligned && ((LineVocal)(lyrics.Content[i + 1])).OppositeAligned)
							vocalGroupContainer.HorizontalOptions = LayoutOptions.End;

						lines.Add(vocalGroupContainer);
					}
					else
					{
						LineVocal vocal = vocalGroup as LineVocal;

						if (vocal.Type == "Vocal")
						{
							string styleName = vocal.OppositeAligned ? "LyricGroupOppositeAligned" : "LyricGroup";

							FlexLayout vocalGroupContainer = [];
							vocalGroupContainer.Style = styles[styleName] as Style;

							double startTime = vocal.StartTime;

							LineVocals lv = new LineVocals(vocalGroupContainer, vocal, false);
							lv.ActivityChanged += async (s, e) => await ScrollViewer.Dispatcher.DispatchAsync(async () => await ScrollViewer.ScrollToAsync(e, ScrollToPosition.Center, true));

							vocalGroups.Add(vocalGroupContainer, [lv]);
							vocalGroupStartTimes.Add(startTime);

							lines.Add(vocalGroupContainer);
						}
					}

					i++;
				}
			}
			else if (type == "Static")
			{
				cancel.ThrowIfCancellationRequested();

				StaticSyncedLyrics providerLyrics = JsonConvert.DeserializeObject<StaticSyncedLyrics>(content);

				TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
				{
					StaticLyrics = providerLyrics
				});

				StaticSyncedLyrics lyrics = transformedLyrics.Lyrics.StaticLyrics;
				staticLyrics = true;

				foreach (var line in lyrics.Lines)
				{
					FlexLayout layout = new FlexLayout
					{
						Style = styles["LyricGroup"] as Style
					};

					layout.Add(new Label
					{
						Text = !string.IsNullOrWhiteSpace(line.RomanizedText) ? line.RomanizedText : line.Text,
						Style = styles["LineLabel"] as Style
					});

					lines.Add(layout);
				}
			}

			await lyricsContainer.Dispatcher.DispatchAsync(() =>
			{
				lyricsContainer.Clear();
				lines.ForEach(lyricsContainer.Add);
			});
		}
		catch (OperationCanceledException)
		{
			//ResetLyrics();
		}
		catch (WebException webEx) when (webEx.Status == WebExceptionStatus.RequestCanceled)
		{
			throw new OperationCanceledException(webEx.Message, webEx, cancel);
		}

		if (staticLyrics) return;

		SpotifyPlayerState player = await Song.Remote.GetPlayerState();
		Song.IsPlaying = !player.IsPaused;
		stopwatch.Restart();

		await Update(cancel, vocalGroups, player.PlaybackPosition / 1000d, 1.0 / 60, true);

		await UpdateProgress(player.PlaybackPosition, stopwatch.ElapsedMilliseconds, vocalGroups, cancel);
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

	private static long lastUpdatedAt = 0;
	private static double lastTimestamp = 0;

	private async Task UpdateProgress(long initialPosition, double startedSyncAt, Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, CancellationToken cancel)
	{
		try
		{
			int[] syncTimings = [50, 100, 150, 750];
			int syncIndex = 0;
			long nextSyncAt = syncTimings[0];

			while (!cancel.IsCancellationRequested)
			{
				cancel.ThrowIfCancellationRequested();

				long updatedAt = stopwatch.ElapsedMilliseconds;

				if (Song.IsPlaying)
				{
					if (updatedAt >= startedSyncAt + nextSyncAt)
					{
						SpotifyPlayerState player = await Song.Remote.GetPlayerState();

						if (!player.IsPaused)
							Song.Remote.Resume();

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

					await Update(cancel, vocalGroups, syncedTimestamp, deltaTime, Math.Abs(initialPosition / 1000d - lastTimestamp) > 0.8d);

					lastTimestamp = syncedTimestamp;
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

	private void ResetLyrics()
	{
		stopwatch.Restart();
		lastUpdatedAt = 0;

		cancelSource?.Dispose();
		cancelSource = new CancellationTokenSource();

		vocalGroups.Clear();

		lyricsContainer.Dispatcher.Dispatch(async () =>
		{
			// await ScrollViewer.ScrollToAsync(0, 0, true);
			lyricsContainer.Children.Clear();
		});
	}
}