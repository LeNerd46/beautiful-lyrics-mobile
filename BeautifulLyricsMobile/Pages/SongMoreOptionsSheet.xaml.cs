using BeautifulLyricsMobile.Models;
using CommunityToolkit.Maui.Alerts;
using The49.Maui.BottomSheet;

namespace BeautifulLyricsMobile.Pages;

public partial class SongMoreOptionsSheet : BottomSheet
{
	public MoreOptionsModel Info { get; set; }

	public event EventHandler Delete;
	public event EventHandler Queue;

	public SongMoreOptionsSheet()
	{
		InitializeComponent();

		Info = new MoreOptionsModel();
		BindingContext = Info;
		HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
	}

	private void DeleteButton(object sender, EventArgs e)
	{
		Delete?.Invoke(this, EventArgs.Empty);
		DismissAsync();
	}

	private void QueueButton(object sender, EventArgs e)
	{
		LyricsView.Remote?.PlayerApi?.Queue(Info.Url);
		Toast.Make("Added to queue");
		Queue?.Invoke(this, EventArgs.Empty);
		DismissAsync();
	}
}