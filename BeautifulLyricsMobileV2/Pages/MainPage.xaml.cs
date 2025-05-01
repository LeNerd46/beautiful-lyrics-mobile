using BeautifulLyricsMobileV2.PageModels;
using BeautifulLyricsMobileV2.Pages;
using BeautifulLyricsMobileV2.Services;
using MauiIcons.Core;
using System.Diagnostics;

namespace BeautifulLyricsMobileV2
{
	public partial class MainPage : ContentPage
	{
		public MainPage(ISpotifyRemoteService service, LyricsViewModel model)
		{
			InitializeComponent();

			LyricsViewModel song = new LyricsViewModel(service)
			{
				Current = this
			};

			song.Saved = false;

			lyricsView.Song = song;
			lyricsView.BindingContext = song;

			lyricsView.OnAppearing();
		}
	}
}
