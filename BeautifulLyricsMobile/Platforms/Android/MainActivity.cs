using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Types;
using Java.Lang;
using RestSharp;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static Com.Spotify.Android.Appremote.Api.IConnector;

namespace BeautifulLyricsMobile
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		SpotifyBroadcastReceiver receiver;
		IntentFilter filter;

		private EmbedIOAuthServer server;
		private bool updatedToken = false;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// while (MainPage.Spotify == null) ;

			SpotifyAppRemote remote;
			ConnectionParams connectionParams = new ConnectionParams.Builder("4d42ec7301a64d57bc1971655116a3b9").SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
			SpotifyAppRemote.Connect(this, connectionParams, new ConnectionListener());

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

				updatedToken = File.Exists(Path.Combine(FileSystem.AppDataDirectory, "token.txt"));

				if (!updatedToken)
				{
					Task.Run(async () =>
					{
						/*while (MainPage.Remote == null)
						{
							await Task.Delay(1000);
						}*/

						server = new EmbedIOAuthServer(new System.Uri("http://localhost:5543/callback"), 5543);
						await server.Start();

						server.ImplictGrantReceived += async (sender, response) =>
						{
							await server.Stop();

							MainPage.AccessToken = response.AccessToken;
							MainPage.Spotify = new SpotifyClient(response.AccessToken);

							MainPage.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
							MainPage.Client.AddDefaultHeader("Authorization", $"Bearer {response.AccessToken}");

							File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "token.txt"), MainPage.AccessToken);
						};

						server.ErrorReceived += async (sender, error, state) =>
						{
							await server.Stop();
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
					MainPage.AccessToken = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, "token.txt"));

					if (!string.IsNullOrWhiteSpace(MainPage.AccessToken))
					{
						MainPage.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
						MainPage.Client.AddDefaultHeader("Authorization", $"Bearer {MainPage.AccessToken}");
					}
					// else
					// 	Toast.MakeText(Platform.CurrentActivity, "Error reading token", ToastLength.Long).Show();

					var config = SpotifyClientConfig.CreateDefault();

					var request = new ClientCredentialsRequest("4d42ec7301a64d57bc1971655116a3b9", "0423d7b832114aa086a2034e2cde0138"); // Use it if you want I guess, it's just a Spotify client
					var response = new OAuthClient(config).RequestToken(request).GetAwaiter().GetResult();

					MainPage.Spotify = new SpotifyClient(config.WithToken(response.AccessToken));

					SpotifyAppRemote remote;
					ConnectionParams connectionParams = new ConnectionParams.Builder("4d42ec7301a64d57bc1971655116a3b9").SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
					SpotifyAppRemote.Connect(this, connectionParams, new ConnectionListener());
				}
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
			base.OnStop();

			SpotifyAppRemote.Disconnect(MainPage.Remote);
		}
	}

	public class SpotifyBroadcastReceiver : BroadcastReceiver
	{
		public static event SongChanged SongChanged;
		public static event PlaybackChanged PlaybackChanged;

		public override void OnReceive(Android.Content.Context? context, Intent? intent)
		{
			long timeSentInMs = intent.GetLongExtra("timeSent", 0L);

			if (intent.Action == "com.spotify.music.metadatachanged")
			{
				string id = intent.GetStringExtra("id");
				string realId = id.Split(':')[2];
				MainPage.CurrentTrackId = realId;

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

				MainPage.IsPlaying = isPlaying;

				PlaybackChanged?.Invoke(this, new PlaybackChangedEventArgs(isPlaying, position));

				// if (isPlaying)
				// 	Toast.MakeText(Platform.CurrentActivity, "Is Playing", ToastLength.Short).Show();
				// else
				// 	Toast.MakeText(Platform.CurrentActivity, "Not Is Playing", ToastLength.Short).Show();
			}
		}
	}

	public class ConnectionListener : Java.Lang.Object, IConnector.IConnectionListener
	{
		public void OnConnected(SpotifyAppRemote? p0)
		{
			MainPage.Remote = p0;
			Toast.MakeText(Platform.CurrentActivity, "Spotify Connected!", ToastLength.Short).Show();
		}

		public void OnFailure(Throwable p0)
		{
			Toast.MakeText(Platform.CurrentActivity, p0.Message, ToastLength.Long).Show();
		}
	}
}
