using BeautifulLyricsMobile.Models;
#if ANDROID
using Android.Telecom;
using Com.Spotify.Protocol.Types;
using static Com.Spotify.Protocol.Client.CallResult;
using static Com.Spotify.Protocol.Client.Subscription;
#endif

namespace BeautifulLyricsMobile.Pages;

public partial class SearchPage : ContentPage
{
	private int previouslySelectedItem = 0;

	public SearchPage()
	{
		InitializeComponent();
	}

	private void searchResults_ItemSelected(object sender, SelectedItemChangedEventArgs e)
	{
		#region SET THE COLOR WHY TF IS IT SO STUPID TO DO THIS
		var cells = searchResults.GetVisualTreeDescendants().Where(x => x is CustomViewCell);

		var previousCell = cells.ElementAt(previouslySelectedItem) as CustomViewCell;
		var cell = cells.ElementAt(e.SelectedItemIndex) as CustomViewCell;

		previousCell.View.BackgroundColor = Color.FromHex("#1f1f1f");
		cell.View.BackgroundColor = cell.SelectedBackgroundColor;

		previouslySelectedItem = e.SelectedItemIndex;
		#endregion

#if ANDRIOD
		if (MainPage.Remote == null)
			return;
#endif

		SearchResult result = e.SelectedItem as SearchResult;

		// Capabilities capabilities = RequestUserCapabilities().GetAwaiter().GetResult();

		// if (!capabilities.CanPlayOnDemand)
		// 	return;

#if ANDROID
		LyricsView.Remote.PlayerApi.Play(result.Url);
#endif
	}


#if ANDROID
	private async Task<Capabilities> RequestUserCapabilities()
	{
		RequestCapabilitiesCallback callback = new RequestCapabilitiesCallback();
		LyricsView.Remote.UserApi?.Capabilities?.SetResultCallback(callback);

		while(callback.Capabilities is null)
		{
			await Task.Delay(10);
		}

		return callback.Capabilities;
	}
#endif
}

#if ANDROID
public class RequestCapabilitiesCallback : Java.Lang.Object, IResultCallback
{
	public Capabilities Capabilities { get; set; }

	public void OnResult(Java.Lang.Object? p0)
	{
		if (p0 is Capabilities capabilities)
			Capabilities = capabilities;
	}
}
#endif