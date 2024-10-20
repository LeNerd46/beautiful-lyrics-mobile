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
			updatedToken = File.Exists(Path.Combine(FileSystem.AppDataDirectory, "token.txt"));

			if (!updatedToken)
			{
				Task.Run(async () =>
				{
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
			}

			InitializeComponent();
		}
	}
}
