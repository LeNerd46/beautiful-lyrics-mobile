using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using BeautifulLyricsMobile.Pages;
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Types;
using Java.Lang;
using Newtonsoft.Json;
using RestSharp;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Http;
using static BeautifulLyricsMobile.Pages.HomePage;
using static Com.Spotify.Android.Appremote.Api.IConnector;

namespace BeautifulLyricsMobile
{
	[Activity(Theme = "@style/Maui.SplashTheme", ResizeableActivity = true, MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, WindowSoftInputMode = SoftInput.AdjustResize)]
	public class MainActivity : MauiAppCompatActivity
	{
		SpotifyBroadcastReceiver receiver;
		IntentFilter filter;

		private EmbedIOAuthServer server;
		private bool updatedToken = false;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			string clientId = SecureStorage.GetAsync("spotifyId").GetAwaiter().GetResult();
			string firstTime = SecureStorage.GetAsync("first").GetAwaiter().GetResult();

			if (!string.IsNullOrWhiteSpace(clientId))
			{
				if (string.IsNullOrWhiteSpace(firstTime) || firstTime != "false")
				{
					var spotifyIntent = PackageManager.GetLaunchIntentForPackage("com.spotify.music");

					if (spotifyIntent != null)
					{
						spotifyIntent.AddFlags(Android.Content.ActivityFlags.NewTask);
						StartActivity(spotifyIntent);
					}
				}

				SpotifyAppRemote remote;
				ConnectionParams connectionParams = new ConnectionParams.Builder(clientId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
				SpotifyAppRemote.Connect(Platform.CurrentActivity, connectionParams, new ConnectionListener());
			}

			receiver = new SpotifyBroadcastReceiver();
			filter = new IntentFilter();

			filter.AddAction("com.spotify.music.playbackstatechanged");
			filter.AddAction("com.spotify.music.metadatachanged");
			filter.AddAction("com.spotify.music.queuechanged");
		}

		protected override void OnResume()
		{
			base.OnResume();

			try
			{
				RegisterReceiver(receiver, filter, ReceiverFlags.Exported);
				// Toast.MakeText(Platform.CurrentActivity, "Receiver Conntected!", ToastLength.Short).Show();

				// while (LyricsView.Remote == null) ;

				string clientId = SecureStorage.GetAsync("spotifyId").GetAwaiter().GetResult();

				if (string.IsNullOrWhiteSpace(clientId)) return;

				// if (LyricsView.Remote == null)
				// {
					SpotifyAppRemote remote;
					ConnectionParams connectionParams = new ConnectionParams.Builder(clientId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
					SpotifyAppRemote.Connect(Platform.CurrentActivity, connectionParams, new ConnectionListener());
				// }

				if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, "creds.json")))
					LyricsView.Spotify = GetToken(clientId);
				else
					CreateToken(clientId!).Wait();
			}
			catch (System.Exception ex)
			{
				Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Short).Show();
			}
		}

		protected override void OnPause()
		{
			UnregisterReceiver(receiver);
			base.OnPause();
		}

		protected override void OnStop()
		{
			SpotifyAppRemote.Disconnect(LyricsView.Remote);
			LyricsView.Remote = null;
			LyricsView.cancelToken.Cancel();

			base.OnStop();
		}

		private SpotifyClient GetToken(string id)
		{
			var json = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, "creds.json"));
			var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

			var auth = new PKCEAuthenticator(id, token!);
			auth.TokenRefreshed += (sender, token) => File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "creds.json"), JsonConvert.SerializeObject(token));

			var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(auth);

			LyricsView.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
			LyricsView.Client.AddDefaultHeader("Authorization", $"Bearer {token!.RefreshToken}");

			return new SpotifyClient(config);
		}

		private async Task CreateToken(string id)
		{
			var (verifier, challenge) = PKCEUtil.GenerateCodes();

			EmbedIOAuthServer server = new EmbedIOAuthServer(new System.Uri("http://localhost:5543/callback"), 5543);

			await server.Start();

			server.AuthorizationCodeReceived += async (sender, response) =>
			{
				await server.Stop();
				var token = await new OAuthClient().RequestToken(new PKCETokenRequest(id, response.Code, server.BaseUri, verifier));

				await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "creds.json"), JsonConvert.SerializeObject(token));
				GetToken(id);
			};

			var request = new LoginRequest(server.BaseUri, id, LoginRequest.ResponseType.Code)
			{
				CodeChallenge = challenge,
				CodeChallengeMethod = "S256",
				Scope = [Scopes.UserReadPrivate, Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative, Scopes.UserLibraryRead, Scopes.UserReadRecentlyPlayed]
			};

			await Launcher.OpenAsync(request.ToUri());
		}
	}

	public class ConnectionListener : Java.Lang.Object, IConnector.IConnectionListener
	{
		public void OnConnected(SpotifyAppRemote? p0)
		{
			LyricsView.Remote = p0;
			Toast.MakeText(Platform.CurrentActivity, "Spotify Connected!", ToastLength.Short).Show();
			SecureStorage.SetAsync("first", "false");
			SpotifyBroadcastReceiver.InvokeSpotifyConnected(p0);
		}

		public void OnFailure(Java.Lang.Throwable p0)
		{
			Toast.MakeText(Platform.CurrentActivity, p0.Message, ToastLength.Long).Show();
		}
	}

	public class SpotifyBroadcastReceiver : BroadcastReceiver
	{
		public static event SongChanged SongChanged;
		public static event PlaybackChanged PlaybackChanged;
		public static event SpotifyConnected SpotifyConnected;

		public static void InvokeSpotifyConnected(SpotifyAppRemote remote)
		{
			SpotifyConnected?.Invoke(null, new SpotifyConnectedEventArgs(remote));
		}

		public override void OnReceive(Android.Content.Context? context, Intent? intent)
		{
			long timeSentInMs = intent.GetLongExtra("timeSent", 0L);

			if (intent.Action == "com.spotify.music.metadatachanged")
			{
				string id = intent.GetStringExtra("id");
				string realId = id.Split(':')[2];
				LyricsView.CurrentTrackId = realId;

				string title = intent.GetStringExtra("track");
				string artist = intent.GetStringExtra("artist");
				string album = intent.GetStringExtra("album");
				int length = intent.GetIntExtra("length", 0);

				SongChanged?.Invoke(this, new SongChangedEventArgs(realId, title, artist, album, length));

				// Toast.MakeText(Platform.CurrentActivity, $"Now Playing: {title}", ToastLength.Short).Show();
			}
			else if (intent.Action == "com.spotify.music.playbackstatechanged")
			{
				bool isPlaying = intent.GetBooleanExtra("playing", false);
				int position = intent.GetIntExtra("playbackPosition", 0);

				LyricsView.IsPlaying = isPlaying;

				PlaybackChanged?.Invoke(this, new PlaybackChangedEventArgs(isPlaying, position));

				// if (isPlaying)
				// 	Toast.MakeText(Platform.CurrentActivity, "Is Playing", ToastLength.Short).Show();
				// else
				// 	Toast.MakeText(Platform.CurrentActivity, "Not Is Playing", ToastLength.Short).Show();
			}
		}
	}
}
