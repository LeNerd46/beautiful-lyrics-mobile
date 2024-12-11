using BeautifulLyricsMobile.Pages;
using Com.Spotify.Android.Appremote.Api;
using Microsoft.Maui.Controls;
using RestSharp;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Models
{
	internal class OnboardingViewModel : INotifyPropertyChanged
	{
		private EmbedIOAuthServer server;
		public event PropertyChangedEventHandler PropertyChanged;

		public Command SkipCommand { get; set; }
		public Command NextCommand { get; set; }
		public Command GetStartedCommand { get; set; }

		public List<OnboardingPage> Source { get; set; }

		private bool enableMainPage = false;
		public bool EnableMainPage
		{
			get => enableMainPage;
			set
			{
				enableMainPage = value;
				OnPropertyChanged();
			}
		}

		private bool activeSkipPage = true;
		public bool ActiveSkipPage
		{
			get => activeSkipPage;
			set
			{
				activeSkipPage = value;
				OnPropertyChanged();
			}
		}

		private int position = 0;
		public int Position
		{
			get => position;
			set
			{
				position = value;
				OnPropertyChanged();
			}
		}

		private bool spotifyClientPage = false;
		public bool SpotifyClientPage
		{
			get => spotifyClientPage;
			set
			{
				spotifyClientPage = value;
				OnPropertyChanged();
			}
		}


		public Color backgroundColor;
		public Color BackgroundColor
		{
			get => backgroundColor;
			set
			{
				backgroundColor = value;
				OnPropertyChanged();
			}
		}

		public OnboardingViewModel()
		{
			Source = [];

			backgroundColor = Color.Parse("#d7ceff");

			Source.Add(new OnboardingPage
			{
				Title = "Welcome to Beautiful Lyrics Mobile",
				Description = "Completely revamp Spotify's boring lyrics with beat by beat lyrics, bringing your music to life. Heavily inspired by the beautiful-lyrics spicetify extension by @surfbryce",
				Color = Color.Parse("#d7ceff"),
			});

			Source.Add(new OnboardingPage
			{
				Title = "Let's Get You Set Up",
				Description = "In order for this app to work, you have to create a Spotify Developer App. Don't worry, it's easy and free! If you would like to learn more, tap the help icon at the top",
				Color = Color.Parse("#7DCCDE"),
				SpotifyClientPage = true
			});

			Source.Add(new OnboardingPage
			{
				Title = "Ready to Get Started?",
				Description = "Your beautiful lyrics are just a few taps away. If you encounter any issues, let me know on my GitHub. To finish setting up, please restart the app",
				Color = Color.Parse("#F5FDC6")
			});

			Position = 0;

			SkipCommand = new Command(Close);
			GetStartedCommand = new Command(NavigateToGetStarted);
			NextCommand = new Command(NextItem);
		}

		private void Close()
		{
			Shell.Current.GoToAsync("//Home");
		}

		private void NavigateToGetStarted()
		{
			Shell.Current.GoToAsync("//Home");
		}

		private async void HelpScreen()
		{
			await Launcher.OpenAsync("https://github.com/LeNerd46/beautiful-lyrics-mobile/blob/main/setup.md");
		}

		public async void NextItem()
		{
			int count = Position + 1;

			if (count == 2)
			{
				if (string.IsNullOrWhiteSpace(Source[1].ClientId) || string.IsNullOrWhiteSpace(Source[1].ClientSecret))
					return;

				await SecureStorage.SetAsync("spotifyId", Source[1].ClientId);
				await SecureStorage.SetAsync("spotifySecret", Source[1].ClientSecret);
			}

			if (count <= Source.Count - 1)
			{
				BackgroundColor = Source[count].Color;
				Position = count;
				EnableMainPage = Position == Source.Count - 1;
				ActiveSkipPage = !EnableMainPage;
			}
			else
				await Shell.Current.GoToAsync("//Home");
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class OnboardingPage
	{
		public string Title { get; set; }
		public string Description { get; set; }
		public Color Color { get; set; }
		public string Image { get; set; }

		public string ClientId { get; set; }
		public string ClientSecret { get; set; }

		public bool SpotifyClientPage { get; set; }
		public Command HelpCommand => new Command(async () =>
		{
			await Launcher.OpenAsync("https://github.com/LeNerd46/beautiful-lyrics-mobile");
		});
	}
}