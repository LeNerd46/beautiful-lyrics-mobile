using BeautifulLyricsMobileV2.PageModels;
using BeautifulLyricsMobileV2.Pages;
using BeautifulLyricsMobileV2.Services;

namespace BeautifulLyricsMobileV2
{
    public partial class MainPage : ContentPage
    {
		public MainPage(ISpotifyRemoteService service, LyricsViewModel model)
        {
            try
            {
                InitializeComponent();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
			}

            lyricsView.Song = new LyricsViewModel
            {
                Remote = service,
				Title = "Title",
				Artist = "Artist",
				Album = "Album",
				Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
                IsPlaying = false,
				Duration = 0
			};
        }
	}
}
