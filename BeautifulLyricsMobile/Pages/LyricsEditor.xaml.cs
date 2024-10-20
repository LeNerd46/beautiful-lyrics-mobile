using Android.Icu.Util;
using Android.Widget;
using BeautifulLyricsAndroid.Entities;
using Newtonsoft.Json;
using RestSharp;
using SkiaSharp;
using SpotifyAPI.Web;
using System.Diagnostics;

namespace BeautifulLyricsMobile.Pages;

public partial class LyricsEditor : ContentPage
{
	private List<LineVocal> Vocals { get; set; }
	private List<SyllableVocalSet> Syllables { get; set; } = [];
	private SyllableVocal currentLine;

	private int index = 0;
	private int wordIndex = 0;
	private int lineIndex = 0;

	private Stopwatch stopwatch;
	private double startTime;
	private bool started = false;

	public LyricsEditor()
	{
		InitializeComponent();

		// Background
		Task.Run(async () =>
		{
			FullTrack track = await MainPage.Spotify.Tracks.Get(MainPage.CurrentTrackId);
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
				Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
			}
		});

		Task.Run(RenderLyrics);
	}

	private async Task RenderLyrics()
	{
		var response = await MainPage.Client.ExecuteAsync(new RestRequest(MainPage.CurrentTrackId));

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

		while (!started) ;

		stopwatch = new Stopwatch();
		MainPage.Remote.PlayerApi.Resume();
		MainPage.Remote.PlayerApi.SkipPrevious();
		stopwatch.Start();
	}

	private void OnScreenTouch(object sender, MR.Gestures.DownUpEventArgs e)
	{
		if(!started)
			return;

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;
		// LineVocal line = Vocals[index];

		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		if (index == 0)
			startTime = seconds;

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

		label.Scale = 1.1;
		label.TranslationY = 1;
		label.Shadow = new Shadow
		{
			Brush = Brush.White
		};
	}

	private void OnScreenRelease(object sender, MR.Gestures.DownUpEventArgs e)
	{
		if (!started)
		{
			started = true;
			return;
		}

		FlexLayout container = LyricsContainer.Children[lineIndex] as FlexLayout;
		Label label = container[wordIndex] as Label;
		LineVocal line = Vocals[lineIndex];

		double seconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;

		currentLine.Syllables.Last().EndTime = seconds;

		label.Scale = 1;
		label.TranslationY = 0;
		label.Shadow = null;

		wordIndex++;
		index++;

		// We've reached the last word in the line
		if (wordIndex == container.Children.Count)
		{
			wordIndex = 0;
			lineIndex++;

			currentLine.EndTime = seconds;

			Syllables.Add(new SyllableVocalSet
			{
				Type = "Vocal",
				OppositeAligned = line.OppositeAligned,
				Lead = currentLine
			});

			currentLine = null;

			// We've reached the end of the song
			if (lineIndex == Vocals.Count)
			{
				File.WriteAllText(Path.Combine(FileSystem.CacheDirectory, $"{MainPage.CurrentTrackId}.json"), JsonConvert.SerializeObject(new SyllableSyncedLyrics
				{
					StartTime = startTime,
					EndTime = seconds,
					Content = [.. Syllables]
				}));

				Toast.MakeText(Platform.CurrentActivity, "Lyrics Saved!", ToastLength.Short).Show();
			}
			else
				ScrollViewer.ScrollToAsync(LyricsContainer.Children[lineIndex] as FlexLayout, ScrollToPosition.Center, true);
		}
	}
}