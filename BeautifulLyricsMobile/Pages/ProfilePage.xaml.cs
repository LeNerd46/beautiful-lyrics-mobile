namespace BeautifulLyricsMobile.Pages;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();

		collection.ItemsSource = Enumerable.Range(1, 100);
	}
}