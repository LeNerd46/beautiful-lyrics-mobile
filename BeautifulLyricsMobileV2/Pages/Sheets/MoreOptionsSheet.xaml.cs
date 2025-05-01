using BeautifulLyricsMobileV2.PageModels;

namespace BeautifulLyricsMobileV2.Pages.Sheets;

public partial class MoreOptionsSheet : ContentPage
{
	SongMoreOptionsModel Song { get; set; }

	public MoreOptionsSheet(SongMoreOptionsModel model)
	{
		InitializeComponent();
		Song = model;
		BindingContext = Song;
	}
}