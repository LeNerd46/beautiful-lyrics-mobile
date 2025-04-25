using Android.AdServices.Topics;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using BeautifulLyricsMobileV2.Platforms.Android.PlatformServices;
using BeautifulLyricsMobileV2.Services;
using Com.Spotify.Android.Appremote.Api;
using CommunityToolkit.Maui.Alerts;
using Java.Lang;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Runtime.CompilerServices;

namespace BeautifulLyricsMobileV2
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		ISpotifyRemoteService Remote;
		SpotifyBroadcastReceiver receiver;
		IntentFilter filter;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Remote = IPlatformApplication.Current.Services.GetRequiredService<ISpotifyRemoteService>();

			receiver = new SpotifyBroadcastReceiver();
			filter = new IntentFilter();

			filter.AddAction("com.spotify.music.playbackstatechanged");
			filter.AddAction("com.spotify.music.metadatachanged");
		}

		protected override void OnResume()
		{
			base.OnResume();
			RegisterReceiver(receiver, filter, ReceiverFlags.Exported);
			
			// if (!Preferences.Get("Onboarding", false))
			// 	return;

			try
			{
				string spotifyId = SecureStorage.GetAsync("spotifyId").GetAwaiter().GetResult();
				if (string.IsNullOrWhiteSpace(spotifyId)) return;

				ConnectionListener listener = new ConnectionListener();

				listener.Connected += (s, e) =>
				{
					Remote.SetRemoteClient(e.Remote);

					//Connected?.Invoke(this, EventArgs.Empty);
					Remote.InvokeConnected();
				};

				listener.Failed += (s, e) =>
				{
					//Preferences.Set("Onboarding", false);
					Toast.Make(e.ErrorMessage, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
				};

				SpotifyAppRemote remote;
				ConnectionParams connectionParams = new ConnectionParams.Builder(spotifyId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
				SpotifyAppRemote.Connect(this, connectionParams, listener);

				// SpotifyClient client;

				/*if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, "creds.json")))
					client = GetToken(clientId);
				else
					CreateToken(clientId).Wait();*/
			}
			catch (System.Exception ex)
			{
				Toast.Make(ex.Message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
			}
		}

		protected override void OnPause()
		{
			try
			{
				UnregisterReceiver(receiver);
			}
			catch(System.Exception) { }

			base.OnPause();
		}

		protected override void OnStop()
		{
			if (Remote?.Client != null)
				SpotifyAppRemote.Disconnect(Remote.Client as SpotifyAppRemote);

			base.OnStop();
		}

		private SpotifyClient GetToken(string id)
		{
			var json = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, "creds.json"));
			var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

			var auth = new PKCEAuthenticator(id, token!);
			SecureStorage.SetAsync("token", token.AccessToken);
			auth.TokenRefreshed += (sender, token) => File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, "creds.json"), JsonConvert.SerializeObject(token));

			var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(auth);

			// Create HttpClient

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
				Scope = [Scopes.UserReadPrivate, Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative, Scopes.UserLibraryRead, Scopes.UserReadRecentlyPlayed, Scopes.UserLibraryModify]
			};

			await Launcher.OpenAsync(request.ToUri());
		}
	}

	public class SpotifyBroadcastReceiver : BroadcastReceiver
	{
		public static event EventHandler<SongChangedEventArgs> SongChanged;
		public static event EventHandler<PlaybackChangedEventArgs> PlaybackChanged;

		public override void OnReceive(Android.Content.Context? context, Intent? intent)
		{
			long timeSentInMs = intent.GetLongExtra("timeSent", 0L);

			if (intent.Action == "com.spotify.music.metadatachanged")
			{
				string id = intent.GetStringExtra("id");
				string realId = id.Split(':')[2];

				string title = intent.GetStringExtra("track");
				string artist = intent.GetStringExtra("artist");
				string album = intent.GetStringExtra("album");
				int length = intent.GetIntExtra("length", 0);

				SongChanged?.Invoke(this, new SongChangedEventArgs(realId, title, artist, album, length));
			}
			else if (intent.Action == "com.spotify.music.playbackstatechanged")
			{
				bool isPlaying = intent.GetBooleanExtra("playing", false);
				int position = intent.GetIntExtra("playbackPosition", 0);

				PlaybackChanged?.Invoke(this, new PlaybackChangedEventArgs(isPlaying, position));
			}
		}
	}

	public class ConnectionListener : Java.Lang.Object, IConnector.IConnectionListener
	{
		public event EventHandler<AndroidSpotifyConnectedEventArgs> Connected;
		public event EventHandler<AndroidSpotifyConnectionFailureEventArgs> Failed;

		public void OnConnected(SpotifyAppRemote? p0)
		{
			Connected?.Invoke(this, new AndroidSpotifyConnectedEventArgs
			{
				Remote = p0
			});
		}

		public void OnFailure(Throwable? p0)
		{
			Failed?.Invoke(this, new AndroidSpotifyConnectionFailureEventArgs
			{
				Exception = p0.InnerException,
				ErrorMessage = p0.Message
			});
		}
	}

	public class AndroidSpotifyConnectedEventArgs : EventArgs
	{
		public SpotifyAppRemote Remote { get; set; }
	}

	// I love naming these
	public class AndroidSpotifyConnectionFailureEventArgs : EventArgs
	{
		public System.Exception Exception { get; set; }
		public string ErrorMessage { get; set; }
	}

	public class SongChangedEventArgs(string id, string title, string artist, string album, int length) : EventArgs
	{
		public string Id { get; set; } = id;

		public string Title { get; set; } = title;
		public string Artist { get; set; } = artist;
		public string Album { get; set; } = album;
		public int Length { get; set; } = length;
	}

	public class PlaybackChangedEventArgs(bool isPlaying, int position) : EventArgs
	{
		public bool IsPlaying { get; set; } = isPlaying;

		/// <summary>
		/// The position of the song in millisecondsa
		/// </summary>
		public int Position { get; set; } = position;
	}
}
