using BeautifulLyricsMobileV2.Entities;
using BeautifulLyricsMobileV2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeautifulLyricsMobileV2.PageModels
{
	public partial class LyricsViewModel : ObservableObject
	{
		public ISpotifyRemoteService Remote { get; }

		public LyricsViewModel(ISpotifyRemoteService remote) 
		{
			Remote = remote;
			//Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg";
		}

		public LyricsViewModel()
		{
			//Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg";
		}

		[ObservableProperty]
		public partial bool IsPlaying { get; set; }

		[ObservableProperty]
		public partial bool Saved { get; set; }

		[ObservableProperty]
		private SpotifyTrack track;

		[RelayCommand]
		public async Task NoFunctionButton(Image image)
		{
			await image.ScaleTo(0.8, 150, Easing.CubicIn);
			await image.ScaleTo(1, 150, Easing.CubicOut);
		}

		[RelayCommand]
		public async Task ToggleLiked(Image image)
		{
			await image.ScaleTo(0.8, 150, Easing.CubicIn);
			await image.ScaleTo(1, 150, Easing.CubicOut);

			SpotifyLibraryState state = await Remote.GetLibraryState(Track.Id);
			Saved = !state.IsAdded;

			if(state.IsAdded)
				await Remote.RemoveLibraryItem(Track.Id);
			else
				await Remote.SaveLibraryItem(Track.Id);
		}

		[RelayCommand]
		public async Task OpenSheet(Image image)
		{
			await image.ScaleTo(0.8, 150, Easing.CubicIn);
			await image.ScaleTo(1, 150, Easing.CubicOut);

			// Doesn't work in .NET 9 :(
			// await sheet.ShowAsync();
		}
	}
}
