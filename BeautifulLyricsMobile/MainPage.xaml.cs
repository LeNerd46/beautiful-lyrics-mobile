using RestSharp;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Diagnostics;
using Android.Widget;
using BeautifulLyricsAndroid.Entities;
using Newtonsoft.Json;
using Java.Security;
using Button = Microsoft.Maui.Controls.Button;
using System.Text;
using CommunityToolkit.Maui.Storage;
using SpotifyAPI.Web.Http;
using SkiaSharp;
using Bumptech.Glide.Load.Resource.Bitmap;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;
using BeautifulLyricsMobile.Entities;
using static Com.Spotify.Protocol.Client.CallResult;
using Newtonsoft.Json.Linq;



#if ANDROID
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Types;
using static Com.Spotify.Protocol.Client.Subscription;
#endif

namespace BeautifulLyricsMobile
{
	public delegate void SongChanged(object sender, SongChangedEventArgs e);
	public delegate void PlaybackChanged(object sender, PlaybackChangedEventArgs e);
	public delegate void TimeStepped(object sender, TimeSteppedEventArgs e);

	public partial class MainPage : ContentPage
	{
#if ANDROID
		internal static SpotifyAppRemote Remote { get; set; }
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

		private string type = "None";
		double lyricsEndTime = -1;
		private Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups = [];
		private List<double> vocalGroupStartTimes = [];

		private double timestamp;
		private bool isPlaying;
		private bool syncProgress = true;

		private bool newSong = false;
		private bool local = false;

		private Task activeTask = null;
		private System.Timers.Timer progressSyncTimer;
		private CancellationTokenSource cancelToken;

		public MainPage()
		{
			/*updatedToken = File.Exists(Path.Combine(FileSystem.AppDataDirectory, "token.txt"));

			if (!updatedToken)
			{
				Task.Run(async () =>
				{
					server = new EmbedIOAuthServer(new System.Uri("http://localhost:5543/callback"), 5543);
					await server.Start();

					server.ImplictGrantReceived += async (sender, response) =>
					{
						await server.Stop();

						accessToken = response.AccessToken;
						spotify = new SpotifyClient(response.AccessToken);

						client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
						client.AddDefaultHeader("Authorization", $"Bearer {response.AccessToken}");

						File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "token.txt"), AccessToken);
					};

					server.ErrorReceived += async (sender, error, state) =>
					{
						await server.Stop();

						await DisplayAlert("Error", $"{error}\nState: {state}", "OK");
					};

					var request = new LoginRequest(server.BaseUri, "4d42ec7301a64d57bc1971655116a3b9", LoginRequest.ResponseType.Token)
					{
						Scope = new List<string> { Scopes.UserReadPrivate }
					};

					await Launcher.OpenAsync(request.ToUri());
				});
			}
			else
			{
				accessToken = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, "token.txt"));

				if (!string.IsNullOrWhiteSpace(accessToken))
				{
					client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
					client.AddDefaultHeader("Authorization", $"Bearer {AccessToken}");
				}
				else
					Toast.MakeText(Platform.CurrentActivity, "Error reading token", ToastLength.Long).Show();

				var config = SpotifyClientConfig.CreateDefault();

				var request = new ClientCredentialsRequest("4d42ec7301a64d57bc1971655116a3b9", "0423d7b832114aa086a2034e2cde0138");
				var response = new OAuthClient(config).RequestToken(request).GetAwaiter().GetResult();

				spotify = new SpotifyClient(config.WithToken(response.AccessToken));
			}*/

			InitializeComponent();

			// activeTask = Task.Run(RenderLyrics);

#if ANDROID
			/*SpotifyBroadcastReceiver.SongChanged += async (sender, e) =>
			{
				if (activeTask?.IsCompleted == true)
					activeTask.Dispose();

				newSong = true;
				CurrentTrackId = e.Id;

				stopwatch.Reset();
				LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Clear());

				activeTask = Task.Run(RenderLyrics);
			};*/

			// Task.Run(RenderLyrics);
			SpotifyBroadcastReceiver.SongChanged += OnSongChanged;

			newSong = true;
			stopwatch.Reset();
			LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Clear());
			cancelToken = new CancellationTokenSource();
			Task.Run(RenderLyrics, cancelToken.Token);
#endif
		}

		protected override bool OnBackButtonPressed()
		{
			SpotifyBroadcastReceiver.SongChanged -= OnSongChanged;

			stopwatch.Stop();
			cancelToken.Cancel();

			return base.OnBackButtonPressed();
		}

		private void OnSongChanged(object sender, SongChangedEventArgs e)
		{
			newSong = true;
			CurrentTrackId = e.Id;

			cancelToken.Cancel();

			stopwatch.Reset();
			LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Clear());

			// RenderLyrics().GetAwaiter().GetResult();
			Task.Run(RenderLyrics, cancelToken.Token);
		}

		public async Task RenderLyrics()
		{
			try
			{
				while (Client == null) ;
				while (Remote == null) ;

				stopwatch.Start();

				// Background
				Task.Run(async () =>
				{
					FullTrack track = await Spotify.Tracks.Get(CurrentTrackId);
					using HttpClient download = new HttpClient();

					try
					{
						var imageStream = await download.GetStreamAsync(track.Album.Images[0].Url);

						SKBitmap skImage = SKBitmap.Decode(imageStream);
						SKSurface surface = SKSurface.Create(new SKImageInfo(track.Album.Images[0].Width * 3, track.Album.Images[0].Height * 3));
						SKCanvas canvas = surface.Canvas;

						SKImageFilter filter = SKImageFilter.CreateBlur(50, 50);

						SKPaint paint = new SKPaint
						{
							ImageFilter = filter
						};

						canvas.Scale(3);
						canvas.DrawBitmap(skImage, new SKPoint(0, 0), paint);

						using var thingImage = surface.Snapshot();
						using var data = thingImage.Encode(SKEncodedImageFormat.Jpeg, 100);

						await MainContentPage.Dispatcher.DispatchAsync(async () => MainContentPage.BackgroundImageSource = ImageSource.FromStream(() => data.AsStream()));
					}
					catch (Exception ex)
					{
						Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
					}
				});

				// await setBackgroundTask;

				string content = "";

				if(File.Exists(Path.Combine(FileSystem.CacheDirectory, $"{CurrentTrackId}.json")))
				{
					content = File.ReadAllText(Path.Combine(FileSystem.CacheDirectory, $"{CurrentTrackId}.json"));
					local = true;
				}
				else
				{
					RestResponse response = await Client.ExecuteAsync(new RestRequest(CurrentTrackId));
					content = response.Content;
					local = false;
				}

				LoadLyrics(content);

#if ANDROID
				double startedSyncAt = stopwatch.ElapsedMilliseconds - (local ? 1000 : 800); // - 800;
				double[] syncTimings = [0.05, 0.1, 0.15, 0.75];
				double canSyncNonLocalTimestamp = isPlaying ? syncTimings.Length : 0;

				long before = stopwatch.ElapsedMilliseconds;

				PlayerState player = await RequestPositionSync();

				isPlaying = !player.IsPaused;
				timestamp = player.PlaybackPosition + startedSyncAt + (stopwatch.ElapsedMilliseconds - before);

				progressSyncTimer?.Close();
				progressSyncTimer = new System.Timers.Timer(10000);
				progressSyncTimer.Elapsed += OnTimerElapsed;
				progressSyncTimer.Start();

				newSong = false;

				await Update(vocalGroups, lyricsEndTime, timestamp, ((double)1 / (double)60), true);
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

						FlexLayout vocalGroupContainer = new FlexLayout();

						vocalGroups.Add(vocalGroupContainer, [new InterludeVisual(vocalGroupContainer, interlude)]);
						vocalGroupStartTimes.Add(interlude.Time.StartTime);

						LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Add(vocalGroupContainer));
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

							topGroup.Dispatcher.Dispatch(() => topGroup.Children.Add(vocalGroupContainer));
							LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Children.Add(topGroup));

							List<SyllableVocals> vocals = [];
							double startTime = set.Lead.StartTime;
							vocals.Add(new SyllableVocals(vocalGroupContainer, set.Lead.Syllables, false, false, set.OppositeAligned));

							if (set.Background?.Count > 0)
							{
								FlexLayout backgroundVocalGroupContainer = new FlexLayout();
								backgroundVocalGroupContainer.Style = Application.Current.Resources.MergedDictionaries.Last()[$"Background{styleName}"] as Style;
								topGroup.Dispatcher.Dispatch(() => topGroup.Children.Add(backgroundVocalGroupContainer));

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
			}
		}

		private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
		{
			syncProgress = true;
		}

		private async Task Update(/* Scroller */ Dictionary<FlexLayout, List<ISyncedVocals>> vocalGroups, double lyricsEndTime, double timestamp, double deltaTime, bool skipped = true, bool skippedByVocal = true)
		{
			if (newSong)
				return;

			foreach (var vocalGroup in vocalGroups.Values)
			{
				foreach (var vocal in vocalGroup)
				{
					// timestampLabel.Dispatcher.Dispatch(() => timestampLabel.Text = $"Time: {timestamp}\nDelta Time: {deltaTime}");
					vocal.Animate(timestamp, deltaTime, skipped);

					// if(vocal is SyllableVocals syllable && syllable.IsActive())
					// 	ScrollViewer.Dispatcher.Dispatch(() => ScrollViewer.ScrollToAsync(syllable.Container, ScrollToPosition.Center, true));
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
			bool proceed = false;
			double deltaTime = -1;
			long position = initialPosition;
			long updatedAt;

			if (syncProgress)
			{
				PlayerState player = await RequestPositionSync();
				position = player.PlaybackPosition;

				updatedAt = stopwatch.ElapsedMilliseconds;
				// deltaTime = (updatedAt - lastUpdatedAt) / 1000;
				// deltaTime = (updatedAt - lastUpdatedAt).TotalMilliseconds / 1000;
				deltaTime = (stopwatch.Elapsed - TimeSpan.FromMilliseconds(lastUpdatedAt)).TotalSeconds;

				proceed = player.Track is Track;
				syncProgress = false;
			}
			else
			{
				position = initialPosition;
				updatedAt = stopwatch.ElapsedMilliseconds;
				// deltaTime = (updatedAt - lastUpdatedAt) / 1000;
				// deltaTime = (updatedAt - lastUpdatedAt).TotalMilliseconds / 1000;
				deltaTime = (stopwatch.Elapsed - TimeSpan.FromMilliseconds(lastUpdatedAt)).TotalSeconds;

				proceed = true;
			}

			if (proceed)
			{
				double newTimestamp = 0;
				double fireDeltaTime = deltaTime;

				double syncedTimestamp = (position / 1000) + (startedSyncAt == 0 ? 0 : (updatedAt - startedSyncAt) / 1000);

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

				if (newTimestamp != 0)
				{
					timestamp = newTimestamp;
					await Update(vocalGroups, lyricsEndTime, timestamp, deltaTime, false, false);
				}
			}

			lastUpdatedAt = updatedAt;

			await Defer(async () => await UpdateProgress(initialPosition, startedSyncAt, vocalGroups, lyricsEndTime));
#endif
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
}
