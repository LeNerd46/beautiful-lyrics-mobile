#if ANDROID
using Android.Health.Connect.DataTypes;
using Android.Widget;
using AndroidX.Core.App;

#endif
using BeautifulLyricsAndroid.Entities;
using BeautifulLyricsMobile.Entities;
using MauiIcons.Core;
using MauiIcons.Material.Rounded;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Newtonsoft.Json;
using RestSharp;
using SkiaSharp;
using SpotifyAPI.Web;
using System.Diagnostics;
using Button = Microsoft.Maui.Controls.Button;

namespace BeautifulLyricsMobile.Pages;

public partial class LyricsEditor : ContentPage
{
	private CustomSyncedLyrics LyricsSave;

	private List<LineVocal> Vocals { get; set; }
	// private List<SyllableVocalSet> Syllables { get; set; } = [];
	private SyllableVocal currentLine;

	private int index = 0;
	private int wordIndex = 0;
	private int lineIndex = 0;

	private Stopwatch stopwatch;
	private double startTime;
	private bool started = false;
	private bool simple = true;
	private int lineCount = 0;

	private int selectedLineIndex = 0;
	private int selectedWordIndex = 0;
	private int cursorPosition = 0;

	public LyricsEditor()
	{
		InitializeComponent();
		_ = new MauiIcon();

									 // This feels wrong
		LyricsSave = new CustomSyncedLyrics([]);

		Task.Run(async () =>
		{
			FullTrack track = await LyricsView.Spotify.Tracks.Get(LyricsView.CurrentTrackId);

			LyricsSave.Title = track.Name;
			LyricsSave.Artist = track.Artists[0].Name;
			LyricsSave.Album = track.Album.Images[0].Url;
		});
	}

	private void StartSync(bool simple)
	{
		// Background
		Task.Run(async () =>
		{
			FullTrack track = await LyricsView.Spotify.Tracks.Get(LyricsView.CurrentTrackId);
			using HttpClient download = new HttpClient();

			try
			{
				var imageStream = await download.GetStreamAsync(track.Album.Images[0].Url);

				SKBitmap skImage = SKBitmap.Decode(imageStream);
				SKSurface surface = SKSurface.Create(new SKImageInfo(track.Album.Images[0].Width * 3, track.Album.Images[0].Height * 3));
				SKCanvas canvas = surface.Canvas;

				SKImageFilter filter = SKImageFilter.CreateBlur(50, 50);

				SKPaint paint = new SKPaint
				{
					ImageFilter = filter
				};

				canvas.Scale(3);
				canvas.DrawBitmap(skImage, new SKPoint(0, 0), paint);

				using var thingImage = surface.Snapshot();
				using var data = thingImage.Encode(SKEncodedImageFormat.Jpeg, 100);

				await MainContentPage.Dispatcher.DispatchAsync(async () => MainContentPage.BackgroundImageSource = ImageSource.FromStream(() => data.AsStream()));
			}
			catch (Exception ex)
			{
				// Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
			}
		});

		LyricsContainer.RemoveAt(0);

		this.simple = simple;

		if (simple)
			Task.Run(RenderLyrics);
		else
			Task.Run(RenderLyricsAdvanced);
	}

	private async Task RenderLyrics()
	{
		var response = await LyricsView.Client.ExecuteAsync(new RestRequest(LyricsView.CurrentTrackId));

		if (!response.IsSuccessful)
			return;

		string type = response.Content.Split('\"')[7];

		if (type == "Line")
		{
			LineSyncedLyrics lineVocals = JsonConvert.DeserializeObject<LineSyncedLyrics>(response.Content);

			TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
			{
				LineLyrics = lineVocals
			});

			LineSyncedLyrics lyrics = transformedLyrics.Lyrics.LineLyrics;
			Vocals = lyrics.Content.Where(x => x is LineVocal).Select(x => x as LineVocal).ToList();
			lineCount = Vocals.Count;

			foreach (var item in lyrics.Content)
			{
				if (item is LineVocal vocal)
				{
					FlexLayout lineGroup = new FlexLayout()
					{
						Style = Application.Current.Resources.MergedDictionaries.Last()[vocal.OppositeAligned ? "ActiveLyricOppositeAligned" : "ActiveLyric"] as Style,
						InputTransparent = true
					};

					foreach (var word in vocal.Text.Split(' '))
					{
						lineGroup.Add(new Label
						{
							Text = word,
							Style = Application.Current.Resources.MergedDictionaries.Last()["ActiveLabel"] as Style,
							InputTransparent = true
						});
					}

					await LyricsContainer.Dispatcher.DispatchAsync(() => LyricsContainer.Add(lineGroup));
				}
			}
		}

		// while (!started) ;
		lineIndex++;
		started = true;

		stopwatch = new Stopwatch();
#if ANDROID
		LyricsView.Remote.PlayerApi.Resume();
		LyricsView.Remote.PlayerApi.SkipPrevious();
#endif
		stopwatch.Start();
	}

	private async Task RenderLyricsAdvanced()
	{
		var response = await LyricsView.Client.ExecuteAsync(new RestRequest(LyricsView.CurrentTrackId));

		if (!response.IsSuccessful)
		{
			MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(Platform.CurrentActivity, "Failed To Get Lyrics", ToastLength.Long).Show());
			return;
		}

		string type = response.Content.Split('\"')[7];
		List<LineVocal> lines = [];

		LineSyncedLyrics lineVocals = JsonConvert.DeserializeObject<LineSyncedLyrics>(response.Content);

		TransformedLyrics transformedLyrics = LyricUtilities.TransformLyrics(new ProviderLyrics
		{
			LineLyrics = lineVocals
		});

		LineSyncedLyrics lyrics = transformedLyrics.Lyrics.LineLyrics;
		Vocals = lyrics.Content.Where(x => x is LineVocal).Select(x => x as LineVocal).ToList();

		foreach (var item in lyrics.Content)
		{
			if (item is LineVocal vocal)
			{
				FlexLayout lineGroup = new FlexLayout()
				{
					Style = Application.Current.Resources.MergedDictionaries.Last()[vocal.OppositeAligned ? "ActiveLyricOppositeAligned" : "ActiveLyric"] as Style,
					InputTransparent = false
				};

				SyllableVocalSet set = new SyllableVocalSet
				{
					Type = "Vocal",
					OppositeAligned = vocal.OppositeAligned,
					Lead = new SyllableVocal
					{
						Syllables = []
					}
				};

				string leadVocals = vocal.Text;
				string? backgroundVocals = null;

				if (vocal.Text.Contains('('))
				{
					set.Background = [];

					var split = leadVocals.Split('(');
					var splitAfer = leadVocals.Split(')');

					if (split.Length == 2)
					{
						string lineBefore = split[1];
						backgroundVocals = lineBefore.Split(')')[0];

						string outputBefore = split[0].Trim();
						string outputAfter = splitAfer[1];

						leadVocals = $"{outputBefore} {outputAfter}".Trim();
					}
					else
					{
						// (Hey! I don't knwo about you (I don't know about you) but I'm feeling 22

						string first = split[1];
						string second = first.Split(')')[0]; // Hey!
						string third = split[2];
						string fourth = third.Split(')')[0]; // I don't know about you

						backgroundVocals = $"{second} {fourth}"; // Hey! I don't know about you

						string leadFirst = split[0].Trim(); // ""
						string leadSecond = splitAfer[1]; // I don't know about you (I don't know about you
						string leadThird = splitAfer[2]; // but I'm feeling 22

						leadVocals = $"{leadFirst} {second.Split('(')[0]} {third}".Trim();
					}
				}

				foreach (var word in leadVocals.Split(' '))
				{
					var button = new Button
					{
						Text = word,
						BackgroundColor = Colors.Transparent,
						TextColor = Colors.White,
						Style = Application.Current.Resources.MergedDictionaries.Last()["ActiveLabel"] as Style,
						HorizontalOptions = LayoutOptions.Start
					};

					button.Clicked += (sender, e) =>
					{
						Button self = sender as Button;

						selectedLineIndex = LyricsContainer.IndexOf(LyricsContainer.Children.FirstOrDefault(x => x is FlexLayout flex && flex.Children.Any(x => x as Button == self)));
						selectedWordIndex = ((FlexLayout)LyricsContainer.Children[selectedLineIndex]).IndexOf(self);
						selectedLineIndex--;

						foreach (var letter in self.Text.ToCharArray())
						{
							wordContainer.Add(new Label
							{
								Text = letter.ToString(),
								TextColor = Colors.White,
								FontAttributes = FontAttributes.Bold
							});
						}

						WordPopupThing.IsVisible = true;
						WordPopupThing.IsEnabled = true;
					};

					lineGroup.Add(button);

					set.Lead.Syllables.Add(new SyllableMetadata
					{
						Text = word,
						IsPartOfWord = false
					});
				}

				if (backgroundVocals != null)
				{
					foreach (var word in backgroundVocals.Split(' '))
					{
						set.Background.Add(new SyllableVocal
						{
							Syllables = [new SyllableMetadata
								{
									Text = word,
									IsPartOfWord = false
								}]
						});
					}
				}

				LyricsSave.Lines.Add(set);
				await LyricsContainer.Dispatcher.DispatchAsync(() => LyricsContainer.Add(lineGroup));
			}
		}

		Button finish = new Button
		{
			Text = "Finish"
		};

		finish.Clicked += async (sender, e) =>
		{
			foreach(var line in LyricsSave.Lines)
			{
				foreach(var word in line.Lead.Syllables)
				{
					if(word.Splits != null || word.Splits.Count > 0)
					{
						string text = word.Text;

						List<SyllableMetadata> parts = [];
						int start = 0;

						foreach (var index in splits)
						{
							parts.Add(new SyllableMetadata
							{
								Text = text[start..index],
								IsPartOfWord = true
							});

							start = index;
						}

						parts.Add(new SyllableMetadata
						{
							Text = text[start..],
							IsPartOfWord = false
						});

						int wordIndex = line.Lead.Syllables.IndexOf(word);
						line.Lead.Syllables.RemoveAt(wordIndex);
						line.Lead.Syllables.InsertRange(wordIndex, parts);
					}
				}
			}

			await LyricsContainer.Dispatcher.DispatchAsync(() => LyricsContainer.Children.Clear());
			await Task.Run(RenderLyricsAdvancedSync);
		};

		LyricsContainer.Dispatcher.Dispatch(() => LyricsContainer.Add(finish));
	}

	private async Task RenderLyricsAdvancedSync()
	{
		lineCount = LyricsSave.Lines.Count;
		var styles = Application.Current.Resources.MergedDictionaries.Last();
		List<Layout> lines = [];

		foreach(var item in LyricsSave.Lines)
		{
			if(item is SyllableVocalSet vocal)
			{
				FlexLayout lineGroup = new FlexLayout
				{
					Style = styles[vocal.OppositeAligned ? "LyricGroupOppositeAligned" : "LyricGroup"] as Style,
					InputTransparent = true
				};

				foreach(var word in vocal.Lead.Syllables)
				{
					lineGroup.Add(new Label
					{
						Text = word.Text,
						Style = word.IsPartOfWord ? styles["LyricEmphasizedLabel"] as Style : styles["LyricLabel"] as Style,
						InputTransparent = true
					});
				}

				lines.Add(lineGroup);
			}
		}

		await LyricsContainer.Dispatcher.DispatchAsync(() => lines.ForEach(LyricsContainer.Add));

		started = true;

		stopwatch.Reset();
	}

	private void OnScreenTouch(object sender, MR.Gestures.DownUpEventArgs e)
	{
		if (!started)
			return;

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;
		// LineVocal line = Vocals[index];

		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		if (index == 0)
			startTime = seconds;

		if (simple)
		{
			currentLine ??= new SyllableVocal
			{
				StartTime = seconds,
				Syllables = []
			};

			currentLine.Syllables.Add(new SyllableMetadata
			{
				StartTime = seconds,
				Text = label.Text,
				IsPartOfWord = false
			});
		}
		else
		{
			SyllableVocal current = LyricsSave.Lines[lineIndex].Lead;

			if (wordIndex == 0)
				current.StartTime = seconds;

			current.Syllables[wordIndex].StartTime = seconds;
		}

		label.ScaleTo(1.1d, 250, Easing.SpringOut);
		// label.Scale = 1.1;
		label.TranslateTo(0, 1, 250, Easing.SpringOut);
		// label.TranslationY = 1;
		label.Shadow = new Shadow
		{
			Brush = Brush.White
		};
	}

	private void OnScreenRelease(object sender, MR.Gestures.DownUpEventArgs e)
	{
		if (!started)
			return;

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;
		LineVocal line = simple ? Vocals[lineIndex] : null;

		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		if (simple)
			currentLine.Syllables.Last().EndTime = seconds;
		else
			LyricsSave.Lines[lineIndex].Lead.Syllables[wordIndex].EndTime = seconds;

		label.ScaleTo(1, 250, Easing.SpringOut);
		label.TranslateTo(0, 0, 250, Easing.SpringOut);
		// label.Scale = 1;
		// label.TranslationY = 0;
		label.Shadow = null;

		wordIndex++;
		index++; // We're literally only using this in one spot

		// We've reached the last word in the line
		if (wordIndex == container.Children.Count)
		{
			wordIndex = 0;
			lineIndex++;

			if (simple)
			{
				currentLine.EndTime = seconds;

				LyricsSave.Lines.Add(new SyllableVocalSet
				{
					Type = "Vocal",
					OppositeAligned = line.OppositeAligned,
					Lead = currentLine
				});

				currentLine = null;
			}

			// We've reached the end of the song
			if (lineIndex == lineCount)
			{
				try
				{
					File.WriteAllText(System.IO.Path.Combine(FileSystem.AppDataDirectory, $"{LyricsView.CurrentTrackId}.json"), JsonConvert.SerializeObject(new SyllableSyncedLyrics
					{
						StartTime = startTime,
						EndTime = seconds,
						Content = [.. LyricsSave.Lines]
					}));
#if ANDROID
					Toast.MakeText(Platform.CurrentActivity, "Lyrics Saved!", ToastLength.Short).Show();
#endif
				}
				catch (Exception ex)
				{
					Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Short).Show();
				}
			}
			else
				ScrollViewer.ScrollToAsync(LyricsContainer.Children[lineIndex] as FlexLayout, ScrollToPosition.Center, true);
		}
	}

	private void SimpleSync(object sender, EventArgs e)
	{
		StartSync(true);
	}

	private void AdvancedSync(object sender, EventArgs e)
	{
		Toast.MakeText(Platform.CurrentActivity, "This isn't working right now, use simple sync", ToastLength.Long).Show();

		return;

		StartSync(false);
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

	private List<int> splits = [];

	private void SplitWord(object sender, EventArgs e)
	{
		splits.Add(cursorPosition);
		(wordContainer[cursorPosition] as Label).Margin = new Thickness(0, 0, 2, 0);
	}
	
	private void CancelSplit(object sender, EventArgs e)
	{
		WordPopupThing.IsVisible = false;
		WordPopupThing.IsEnabled = false;

		foreach (var item in wordContainer.Children.ToList())
		{
			if (item is not BoxView)
				wordContainer.Remove(item);
		}

		splits = [];

		selectedLineIndex = 0;
		selectedWordIndex = 0;

		cursorPosition = 0;
	}

	private void FinishSplit(object sender, EventArgs e)
	{
		SyllableVocalSet set = LyricsSave.Lines[selectedLineIndex];
		SyllableMetadata metadata = set.Lead.Syllables[selectedWordIndex]; // If you split multiple words in the same line, it breaks because you just added a ton of syllables

		// We'll do the actual splits when we're done syncing the entire song, in case if you want to come back to it later
		metadata.Splits = splits;

		WordPopupThing.IsVisible = false;
		WordPopupThing.IsEnabled = false;

		foreach (var item in wordContainer.Children.ToList())
		{
			if (item is not BoxView)
				wordContainer.Remove(item);
		}

		splits = [];

		selectedLineIndex = 0;
		selectedWordIndex = 0;

		cursorPosition = 0;
	}

	private void FinishSplitOld(object sender, EventArgs e)
	{
		SyllableVocalSet set = LyricsSave.Lines[selectedLineIndex];
		SyllableMetadata metadata = set.Lead.Syllables[selectedWordIndex]; // If you split multiple words in the same line, it breaks because you just added a ton of syllables
		string word = metadata.Text;

		List<SyllableMetadata> parts = [];
		int start = 0;

		foreach (var index in splits)
		{
			parts.Add(new SyllableMetadata
			{
				Text = word[start..index],
				IsPartOfWord = true
			});

			start = index;
		}

		parts.Add(new SyllableMetadata
		{
			Text = word[start..],
			IsPartOfWord = false
		});

		set.Lead.Syllables.RemoveAt(selectedWordIndex);
		set.Lead.Syllables.InsertRange(selectedWordIndex, parts);

		WordPopupThing.IsVisible = false;
		WordPopupThing.IsEnabled = false;

		foreach (var item in wordContainer.Children.ToList())
		{
			if (item is not BoxView)
				wordContainer.Remove(item);
		}

		splits = [];

		selectedLineIndex = 0;
		selectedWordIndex = 0;

		cursorPosition = 0;
	}
}