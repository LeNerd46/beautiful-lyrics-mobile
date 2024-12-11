#if ANDROID
using Android.Widget;
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

						await SecureStorage.SetAsync("token", MainPage.AccessToken);
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
				MainPage.AccessToken = SecureStorage.GetAsync("token").GetAwaiter().GetResult();

				if (!string.IsNullOrWhiteSpace(MainPage.AccessToken))
				{
					MainPage.Client = new RestClient("https://beautiful-lyrics.socalifornian.live/lyrics/");
					MainPage.Client.AddDefaultHeader("Authorization", $"Bearer {MainPage.AccessToken}");
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

				MainPage.Spotify = new SpotifyClient(config.WithToken(response.AccessToken));

				// SpotifyAppRemote remote;
				// ConnectionParams connectionParams = new ConnectionParams.Builder(clientId).SetRedirectUri("http://localhost:5543/callback").ShowAuthView(true).Build();
				// SpotifyAppRemote.Connect(Platform.CurrentActivity, connectionParams, new ConnectionListener());
			}

			InitializeComponent();
		}
	}
}
