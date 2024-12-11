using CommunityToolkit.Maui.Core.Platform;
using MauiIcons.Core;

namespace BeautifulLyricsMobile.Pages;

public partial class OnboardingPage : ContentPage
{
	public static string clientId { get; set; }
	public static string clientSecret { get; set; }

	public OnboardingPage()
	{
		InitializeComponent();
		_ = new MauiIcon();
	}

	private void ClientIdEntry(object sender, TextChangedEventArgs e)
	{
		Entry entry = sender as Entry;
		clientId = entry.Text;
	}

	private void ClientSecretEntry(object sender, TextChangedEventArgs e)
	{
		Entry entry = sender as Entry;
		clientSecret = entry.Text;
	}
}