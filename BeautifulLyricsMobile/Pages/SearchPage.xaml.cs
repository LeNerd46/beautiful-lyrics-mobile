using BeautifulLyricsMobile.Models;
using SpotifyAPI.Web;
using static Android.App.DownloadManager;

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
	private CancellationTokenSource cancel;

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

		while (callback.Capabilities is null)
		{
			await Task.Delay(10);
		}

		return callback.Capabilities;
	}

	private async void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		cancel?.Cancel();
		cancel = new CancellationTokenSource();

		List<SearchResult> results = [];

		try
		{
			await Task.Run(async () =>
			{
				await Task.Delay(500, cancel.Token);

				if (!string.IsNullOrWhiteSpace(e.NewTextValue))
				{
					var response = await LyricsView.Spotify.Search.Item(new SearchRequest(SearchRequest.Types.All, e.NewTextValue));
					searchModel.SearchResults.Clear();

					results.AddRange(response.Tracks.Items.Select(x => new SearchResult
					{
						Title = x.Name,
						Artist = x.Artists[0].Name,
						ImageUrl = x.Album.Images.Last().Url,
						Url = $"spotify:track:{x.Id}",
						Type = typeof(FullTrack)
					}));

					results.AddRange(response.Artists.Items.Select(x => new SearchResult
					{
						Title = x.Name,
						Artist = "Artist",
						ImageUrl = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
						Url = $"spotify:artist:{x.Id}",
						Type = typeof(FullArtist)
					}));

					results.AddRange(response.Albums.Items.Select(x => new SearchResult
					{
						Title = x.Name,
						Artist = $"Album - {x.Artists[0].Name}",
						ImageUrl = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
						Url = $"spotify:album:{x.Id}",
						Type = typeof(FullAlbum)
					}));

				}
			});

			//searchModel.SearchResults = results;
			results.ForEach(searchModel.SearchResults.Add);
		}
		catch
		{

		}
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