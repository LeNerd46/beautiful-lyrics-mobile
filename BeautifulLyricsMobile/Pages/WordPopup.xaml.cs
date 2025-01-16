using BeautifulLyricsAndroid;
using BeautifulLyricsAndroid.Entities;
using MauiIcons.Core;

namespace BeautifulLyricsMobile.Pages;

public partial class WordPopup : ContentView
{
	public event EventHandler Canceled;
	public event EventHandler Finished;

	private int cursorPosition = 0;
	public List<int> splits;

	public WordPopup()
	{
		InitializeComponent();
		_ = new MauiIcon();
		splits = [];
	}

	public void SetWord(string word)
	{
		foreach(var letter in word.ToCharArray())
		{
			wordContainer.Add(new Label
			{
				Text = letter.ToString(),
				TextColor = Colors.White,
				FontAttributes = FontAttributes.Bold
			});
		}
	}

	private void CursorLeft(object sender, EventArgs e)
	{
		cursorPosition -= cursorPosition == 1 ? 0 : 1;

		Label label = wordContainer[cursorPosition] as Label;
		var cursorX = label.X + label.Width;

		cursor.TranslationX = cursorX;
	}

	private void CursorRight(object sender, EventArgs e)
	{
		cursorPosition += cursorPosition == wordContainer.Count - 2 ? 0 : 1;

		Label label = wordContainer[cursorPosition] as Label;
		var cursorX = label.X + label.Width;

		cursor.TranslationX = cursorX;
	}

	private void SplitWord(object sender, EventArgs e)
	{
		splits.Add(cursorPosition);
		(wordContainer[cursorPosition] as Label).Margin = new Thickness(0, 0, 2, 0);
	}

	private void CancelSplit(object sender, EventArgs e)
	{
		foreach (var item in wordContainer.Children.ToList())
		{
			if (item is not BoxView)
				wordContainer.Remove(item);
		}

		splits = [];

		Canceled?.Invoke(this, new EventArgs());
	}

	private void FinishSplit(object sender, EventArgs e)
	{
		foreach (var item in wordContainer.Children.ToList())
		{
			if (item is not BoxView)
				wordContainer.Remove(item);
		}

		splits = [];

		Finished?.Invoke(this, new EventArgs());
	}
}