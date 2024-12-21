#if ANDROID
using Android.Widget;
using BeautifulLyricsMobile.Pages;
using Com.Spotify.Android.Appremote.Api;
using Java.Lang;

#endif
using RestSharp;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace BeautifulLyricsMobile
{
	public partial class AppShell : Shell
	{
		private EmbedIOAuthServer server;
		private bool updatedToken = false;

		public AppShell()
		{
			InitializeComponent();

			return;

			string updatedToken = SecureStorage.GetAsync("token").GetAwaiter().GetResult();

			if (!string.IsNullOrWhiteSpace(updatedToken))
			{
				Task.Run(async () =>
				{
					/*while (LyricsView.Remote == null)
					{
						await Task.Delay(1000);
					}*/

					server = new EmbedIOAuthServer(new System.Uri("http://localhost:5543/callback"), 5543);
					await server.Start();

					server.ImplictGrantReceived += async (sender, response) =>
					{
						await server.Stop();

						LyricsView.AccessToken = response.AccessToken;
						LyricsView.Spotify = new SpotifyClient(response.AccessToken);

						LyricsView.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
						LyricsView.Client.AddDefaultHeader("Authorization", $"Bearer {response.AccessToken}");

						await SecureStorage.SetAsync("token", LyricsView.AccessToken);
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
				LyricsView.AccessToken = SecureStorage.GetAsync("token").GetAwaiter().GetResult();

				if (!string.IsNullOrWhiteSpace(LyricsView.AccessToken))
				{
					LyricsView.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
					LyricsView.Client.AddDefaultHeader("Authorization", $"Bearer {LyricsView.AccessToken}");
				}
				// else
				// 	Toast.MakeText(Platform.CurrentActivity, "Error reading token", ToastLength.Long).Show();

				var config = SpotifyClientConfig.CreateDefault();

				string clientId = SecureStorage.GetAsync("spotifyId").GetAwaiter().GetResult();
				string secret = SecureStorage.GetAsync("spotifySecret").GetAwaiter().GetResult();

				if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
					return;

				var request = new ClientCredentialsRequest(clientId, secret);
				var response = new OAuthClient(config).RequestToken(request).GetAwaiter().GetResult();

				LyricsView.Spotify = new SpotifyClient(config.WithToken(response.AccessToken));

				// SpotifyAppRemote remote;
				// ConnectionParams connectionParams = new ConnectionParams.Builder(clientId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
				// SpotifyAppRemote.Connect(Platform.CurrentActivity, connectionParams, new ConnectionListener());
			}

			InitializeComponent();
		}
	}
}
