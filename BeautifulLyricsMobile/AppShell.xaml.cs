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
		public AppShell()
		{
			InitializeComponent();
		}
	}
}
