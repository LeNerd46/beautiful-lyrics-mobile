using Swan;
using System.Diagnostics;

namespace BeautifulLyricsMobile
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			MainPage = new AppShell();
		}

		protected override async void OnStart()
		{
			base.OnStart();

			string id = await SecureStorage.GetAsync("spotifyId");
			string secret = await SecureStorage.GetAsync("spotifySecret");
			bool needsOnboarding = string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret);

			if (needsOnboarding)
				await Shell.Current.GoToAsync("//Onboarding");
			else
				await Shell.Current.GoToAsync("//Home");
		}
	}
}
