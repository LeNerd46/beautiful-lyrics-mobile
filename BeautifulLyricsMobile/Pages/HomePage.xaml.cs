#if ANDROID
using Android.Widget;
using Com.Spotify.Protocol.Types;
#endif
using BeautifulLyricsMobile.Models;
using MauiIcons.Core;
using MauiIcons.Material.Rounded;
using SkiaSharp;
using SpotifyAPI.Web;
using BeautifulLyricsMobile.Controls;
using RestSharp;
using Swan;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net;
using CommunityToolkit.Maui.Views;
using System.Diagnostics;
using Com.Spotify.Android.Appremote.Api;
using Microsoft.Maui.Controls;
using SpotifyAPI.Web.Auth;

namespace BeautifulLyricsMobile.Pages;

public partial class HomePage : ContentPage
{
	public SongViewModel Song { get; set; }

	private bool isPlaying = false;

	public HomePage()
	{
		InitializeComponent();
		_ = new MauiIcon();

		Song = new SongViewModel
		{
			Image = "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg",
			AnimatedImage = "https://static.videezy.com/system/resources/previews/000/037/474/original/circle_loading.mp4",
			PlayStatus = MaterialRoundedIcons.Pause
		};

		BindingContext = Song;

		if (!MainPage.IsPlaying)
		{
			pauseButton.IsEnabled = true;
			playButton.IsEnabled = false;

			pauseButton.IsVisible = true;
			playButton.IsVisible = false;

			fullPauseButton.IsVisible = true;
			fullPlayButton.IsVisible = false;

			Song.ToggleTimer(true);
		}
		else
		{
			pauseButton.IsEnabled = false;
			playButton.IsEnabled = true;

			pauseButton.IsVisible = false;
			playButton.IsVisible = true;

			fullPauseButton.IsVisible = false;
			fullPlayButton.IsVisible = true;

			Song.ToggleTimer(false);
		}

#if ANDROID
		SpotifyBroadcastReceiver.SongChanged += (sender, song) =>
		{
			Song.Title = song.Name;
			Song.Artist = song.Artist;
			Song.Album = song.Album;

			Song.Duration = song.Length;

			var imageTask = Task.Run(async () =>
			{
				while (MainPage.Remote == null)
					await Task.Delay(10);

				try
				{
					// Find song on Apple Music
					RestClient iTunes = new RestClient("https://itunes.apple.com/");
					var search = await iTunes.ExecuteAsync(new RestRequest($"search?term={song.Name.Replace(' ', '+')}+{song.Artist.Replace(' ', '+')}&limit=1"));

					if (search == null)
					{
						Toast.MakeText(Platform.CurrentActivity, "Failed to Search", ToastLength.Long).Show();
						return;
					}

					using var memoryStream = new MemoryStream(search.RawBytes);
					using var reader = new StreamReader(memoryStream);
					var jsonBuilder = new StringBuilder();
					string line;

					while ((line = await reader.ReadLineAsync()) != null)
					{
						jsonBuilder.AppendLine(line);
					}

					JObject json = JObject.Parse(jsonBuilder.ToString());
					string albumUrl = json["results"][0]["collectionViewUrl"].ToString();

					// Finally get album cover with the Apple Music album URL
					// RestClient animatedCover = new RestClient("https://clients.dodoapps.io/playlist-precis/");
					// RestResponse response = await animatedCover.ExecuteAsync(new RestRequest("playlist-artwork.php", Method.Post).AddParameter("url", albumUrl).AddParameter("animation", "true"));
					// JObject albumCover = JObject.Parse(response.Content);
					RestClient animatedCover = new RestClient("https://24f2-75-231-80-212.ngrok-free.app/api/apple");
					// byte[] response = await animatedCover.DownloadDataAsync(new RestRequest($"album/{albumUrl.Split('/')[^1].Split('?')[0]}"));
					Stream stream = await animatedCover.DownloadStreamAsync(new RestRequest($"album/{albumUrl.Split('/')[^1].Split('?')[0]}"));

					// if (string.IsNullOrWhiteSpace(albumCover["animatedUrl"].ToString()))
					if (stream == null)
					{
						// There is no animated cover art
						PlayerState playerThing = await MainPage.RequestPositionSync();

						if (playerThing != null)
						{
							Song.Image = $"https://i.scdn.co/image/{playerThing.Track.ImageUri.Raw.Split(':')[2]}";
							Song.Timestamp = playerThing.PlaybackPosition;

							albumCoverPlayer.Dispatcher.Dispatch(() =>
							{
								albumCoverPlayer.IsVisible = false;
								coverImageBorder.IsVisible = true;
							});

							// Toast.MakeText(Platform.CurrentActivity, "No Animated Cover!", ToastLength.Long).Show();
						}
					}
					else
					{
						// Song.AnimatedImage = albumCover["animatedUrl"].ToString();
						if (File.Exists(Song.AnimatedImage))
							File.Delete(Song.AnimatedImage);

						string path = Path.GetTempFileName();
						using var fileStream = File.OpenWrite(path);
						await stream.CopyToAsync(fileStream);
						// await File.WriteAllBytesAsync(path, response);


						// Song.AnimatedImage = $"https://4ff9-75-231-80-212.ngrok-free.app//api/apple/album/{albumUrl.Split('/')[^1].Split('?')[0]}";
						Song.AnimatedImage = path;
						albumCoverPlayer.Dispatcher.Dispatch(() =>
						{
							albumCoverPlayer.Source = MediaSource.FromFile(path);

							albumCoverPlayer.IsVisible = true;
							coverImageBorder.IsVisible = false;
						});

						/*try
						{
							using var client = new HttpClient();
							var data = await client.GetByteArrayAsync(albumCover["animatedUrl"].ToString());

							string inputPath = $"{Path.GetTempPath()}/input.mp4";
							string outputPath = $"{Path.GetTempFileName()}/output.mp4";
							await File.WriteAllBytesAsync(inputPath, data);

							string ffmpeg = Path.Combine(Android.App.Application.Context.ApplicationInfo.NativeLibraryDir, "ffmpeg");
							string tempPathName = Path.GetTempFileName();

							if (!File.Exists(ffmpeg))
								throw new FileNotFoundException("Could not find ffmpeg");

							var startInfo = new ProcessStartInfo
							{
								FileName = ffmpeg,
								Arguments = $"-i {inputPath} -c copy -movflags +faststart {outputPath}",
								RedirectStandardOutput = true,
								RedirectStandardError = true,
								UseShellExecute = false,
								CreateNoWindow = true
							};

							using var process = new Process();
							process.StartInfo = startInfo;

							process.Start();
							await process.WaitForExitAsync();

							Song.AnimatedImage = outputPath;
						}
						catch(Exception ex)
						{
							Debug.WriteLine($"[Beautiful Lyrics ERROR] {ex.Message}");
						}*/

						// if (File.Exists(Song.AnimatedImage))
						// 	File.Delete(Song.AnimatedImage);

						// using var client = new HttpClient();
						// var data = await client.GetByteArrayAsync(albumCover["animatedUrl"].ToString());

						// string input = Path.GetTempFileName();
						// string output = Path.GetTempFileName();
						// FileStream fileStream = new FileStream(input, FileMode.Create, FileAccess.Write);
						// FileStream outputStream = new FileStream(output, FileMode.Create, FileAccess.ReadWrite);

						// Processor processor = new Processor();
						// processor.Process(input, output);

						// string output = FixMoovAtom(new MemoryStream(data));
						// albumCoverPlayer.Dispatcher.Dispatch(() => albumCoverPlayer.Source = MediaSource.FromFile(output));
						// Song.AnimatedImage = output;
					}
				}
				catch (Exception ex)
				{
					// Toast.MakeText(Platform.CurrentActivity, "Failed to Search", ToastLength.Long).Show();

					PlayerState playerThing = await MainPage.RequestPositionSync();

					if (playerThing != null)
					{
						Song.Image = $"https://i.scdn.co/image/{playerThing.Track.ImageUri.Raw.Split(':')[2]}";
						Song.Timestamp = playerThing.PlaybackPosition;
					}
				}

				PlayerState player = await MainPage.RequestPositionSync();

				if (player != null)
				{
					Song.Image = $"https://i.scdn.co/image/{player.Track.ImageUri.Raw.Split(':')[2]}";
					Song.Timestamp = player.PlaybackPosition;
				}
			});
		};

		SpotifyBroadcastReceiver.PlaybackChanged += (sender, player) =>
		{
			if (player.IsPlaying)
			{
				Song.PlayStatus = MaterialRoundedIcons.Pause;
				Song.Timestamp = player.Position;

				if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = true;
					playButton.IsVisible = false;

					pauseButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = true;
					fullPlayButton.IsVisible = false;
				}

				// pauseIcon.IsVisible = true;
				// playIcon.IsVisible = false;
			}
			else
			{
				Song.PlayStatus = MaterialRoundedIcons.PlayArrow;

				if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = false;
					playButton.IsVisible = true;

					playButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = false;
					fullPlayButton.IsVisible = true;
				}

				// pauseIcon.IsVisible = false;
				// playIcon.IsVisible = true;
			}
		};
#endif
	}

	protected override bool OnBackButtonPressed()
	{
		if (fullSongCard.IsVisible)
		{
			fullSongCard.IsVisible = false;
			return true;
		}

		return base.OnBackButtonPressed();
	}

	private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		fullSongCard.IsVisible = true;
	}

	private void TogglePause(object sender, EventArgs e)
	{
		isPlaying = !isPlaying;
		Song.ToggleTimer(isPlaying);

#if ANDROID
		if (isPlaying)
			MainPage.Remote.PlayerApi.Resume();
		else
			MainPage.Remote.PlayerApi.Pause();
#endif
	}

	private void FullCardCollapse(object sender, EventArgs e)
	{
		fullSongCard.IsVisible = false;
	}

	private void SkipNext(object sender, EventArgs e)
	{
#if ANDROID
		MainPage.Remote.PlayerApi.SkipNext();
#endif
	}

	private void SkipPrevious(object sender, EventArgs e)
	{
#if ANDROID
		MainPage.Remote.PlayerApi.SkipPrevious();
#endif
	}

	private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		// if (MainPage.Remote != null && Math.Abs(e.NewValue - e.OldValue) > 1000)
		// 	MainPage.Remote.PlayerApi.SeekTo((long)e.NewValue);
	}

	private async void LyricsView(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new MainPage());
	}

	private async void AddLyrics(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new LyricsEditor());
	}

	private void DeleteSong(object sender, EventArgs e)
	{
#if ANDROID
		string path = Path.Combine(FileSystem.CacheDirectory, $"{MainPage.CurrentTrackId}.json");

		if (File.Exists(path))
			File.Delete(path);
		else
			Toast.MakeText(Platform.CurrentActivity, "No lyrics found!", ToastLength.Short).Show();
#endif
	}

	private void GetColors(object sender, EventArgs e)
	{
		colorContainer.Clear();

		SKBitmap image = null;

		List<(SKColor, int)> colors = [];

		var task = Task.Run(async () =>
		{
			FullTrack track = await MainPage.Spotify.Tracks.Get(MainPage.CurrentTrackId);
			using HttpClient download = new HttpClient();

			try
			{
				var imageStream = await download.GetStreamAsync(track.Album.Images[0].Url);

				var inputBitmap = SKBitmap.Decode(imageStream);
				image = inputBitmap;

				int resizeWidth = 100;
				int resizeHeight = (int)((double)inputBitmap.Height / inputBitmap.Width * resizeWidth);
				using var resizedBitmap = inputBitmap.Resize(new SKImageInfo(resizeWidth, resizeHeight), SKFilterQuality.Low);

				const int k = 5;
				const int colorDepth = 8;

				int factor = 256 / colorDepth;
				Dictionary<SKColor, int> colorFrequency = [];

				for (int y = 0; y < resizedBitmap.Height; y++)
				{
					for (int x = 0; x < resizedBitmap.Width; x++)
					{
						var pixel = resizedBitmap.GetPixel(x, y);

						var quantizedColor = new SKColor((byte)((pixel.Red / factor) * factor), (byte)((pixel.Green / factor) * factor), (byte)((pixel.Blue / factor) * factor));

						if (colorFrequency.ContainsKey(quantizedColor))
							colorFrequency[quantizedColor]++;
						else
							colorFrequency[quantizedColor] = 1;
					}
				}

				colors = colorFrequency.OrderByDescending(kvp => kvp.Value).Take(k).Select(kvp => (kvp.Key, kvp.Value)).ToList();
			}
			catch (Exception ex)
			{
				// Toast.MakeText(Platform.CurrentActivity, ex.Message, ToastLength.Long).Show();
			}
		});

		task.Wait();

		foreach (var color in colors)
		{
			colorContainer.Add(new Label
			{
				Text = $"({color.Item1.Red}, {color.Item1.Green}, {color.Item1.Blue}) - {color.Item2}",
				TextColor = new Color(color.Item1.Red, color.Item1.Green, color.Item1.Blue)
			});
		}

		// gridBoy.Add(new BlobAnimationView(colors.Select(x => x.Item1).ToArray())

		foreach (var blob in gridBoy.Children.Where(x => x is BlobAnimationView).ToList())
		{
			gridBoy.Children.Remove(blob);
		}

		gridBoy.Add(new BlobAnimationView(image)
		{
			HorizontalOptions = LayoutOptions.FillAndExpand,
			VerticalOptions = LayoutOptions.FillAndExpand,
			InputTransparent = true,
			ZIndex = -1
		});
	}

	private void ContentPage_Unloaded(object sender, EventArgs e)
	{
		albumCoverPlayer.Handler?.DisconnectHandler();
	}

	private string FixMoovAtom(Stream input)
	{
		try
		{

			string tempFilePath = Path.GetTempFileName();
			using FileStream output = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
			byte[] buffer = new byte[8];
			long moovStart = 0;
			long moovSize = 0;

			while (input.Position < input.Length)
			{
				input.Read(buffer, 0, 8);
				int atomSize = BitConverter.ToInt32(buffer.Take(4).Reverse().ToArray(), 0);
				string atomType = System.Text.Encoding.ASCII.GetString(buffer, 4, 4);

				if (atomType == "moov")
				{
					moovStart = input.Position - 8;
					moovSize = atomSize;

					break;
				}

				input.Seek(atomSize - 8, SeekOrigin.Current);
			}

			if (moovStart == 0)
				throw new InvalidOperationException("Moov atom not found");

			input.Seek(moovStart, SeekOrigin.Begin);
			byte[] moovData = new byte[moovSize];
			input.Read(moovData, 0, (int)moovSize);
			output.Write(moovData, 9, (int)moovSize);

			input.Seek(0, SeekOrigin.Begin);
			while (input.Position < input.Length)
			{
				if (input.Position >= moovStart && input.Position < moovStart + moovSize)
				{
					// Skiip moov atom
					input.Seek(moovStart + moovSize, SeekOrigin.Begin);
				}
				{
					int readBytes = input.Read(buffer, 0, buffer.Length);
					output.Write(buffer, 0, readBytes);
				}
			}

			return tempFilePath;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex.Message);
			return ex.Message;
		}
	}
}