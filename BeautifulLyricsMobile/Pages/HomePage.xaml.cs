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
using Com.Spotify.Protocol.Client;
using Android.Runtime;
using static Com.Spotify.Protocol.Client.CallResult;
using System.Collections.ObjectModel;

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

        if (!LyricsView.IsPlaying)
        {
            pauseButton.IsEnabled = true;
            playButton.IsEnabled = false;

            pauseButton.IsVisible = true;
            playButton.IsVisible = false;

            // fullPauseButton.IsVisible = true;
            // fullPlayButton.IsVisible = false;

            Song.ToggleTimer(true);
        }
        else
        {
            pauseButton.IsEnabled = false;
            playButton.IsEnabled = true;

            pauseButton.IsVisible = false;
            playButton.IsVisible = true;

            // fullPauseButton.IsVisible = false;
            // fullPlayButton.IsVisible = true;

            Song.ToggleTimer(false);
        }

        interludeToggle.IsToggled = Preferences.Get("showInterludes", true);

#if ANDROID
        SpotifyBroadcastReceiver.SongChanged += async (sender, song) =>
        {
            lyricsView.OnAppearing(song);

            Song.Title = song.Name;
            Song.Artist = song.Artist;
            Song.Album = song.Album;

            Song.Duration = song.Length;

            await Task.Run(async () =>
            {
                while (LyricsView.Remote == null)
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
                        PlayerState playerThing = await LyricsView.RequestPositionSync();

                        if (playerThing != null)
                        {
                            Song.Image = $"https://i.scdn.co/image/{playerThing.Track.ImageUri.Raw.Split(':')[2]}";
                            Song.Timestamp = playerThing.PlaybackPosition;

                            /*albumCoverPlayer.Dispatcher.Dispatch(() =>
							{
								albumCoverPlayer.IsVisible = false;
								coverImageBorder.IsVisible = true;
							});*/

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
                        /*albumCoverPlayer.Dispatcher.Dispatch(() =>
						{
							albumCoverPlayer.Source = MediaSource.FromFile(path);

							albumCoverPlayer.IsVisible = true;
							coverImageBorder.IsVisible = false;
						});*/

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

                    PlayerState playerThing = await LyricsView.RequestPositionSync();

                    if (playerThing != null)
                    {
                        Song.Image = $"https://i.scdn.co/image/{playerThing.Track.ImageUri.Raw.Split(':')[2]}";
                        Song.Timestamp = playerThing.PlaybackPosition;
                    }
                }

                PlayerState player = await LyricsView.RequestPositionSync();

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

                /*if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = true;
					playButton.IsVisible = false;

					pauseButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = true;
					fullPlayButton.IsVisible = false;
				}*/

                // pauseIcon.IsVisible = true;
                // playIcon.IsVisible = false;
            }
            else
            {
                Song.PlayStatus = MaterialRoundedIcons.PlayArrow;

                /*if (!fullSongCard.IsVisible)
				{
					pauseButton.IsVisible = false;
					playButton.IsVisible = true;

					playButton.BackgroundColor = Colors.Transparent;
				}
				else
				{
					fullPauseButton.IsVisible = false;
					fullPlayButton.IsVisible = true;
				}*/

                // pauseIcon.IsVisible = false;
                // playIcon.IsVisible = true;
            }
        };

        bool connected = false;

        SpotifyBroadcastReceiver.SpotifyConnected += async (sender, remote) =>
        {
            if (connected) return;
            connected = true;

            ListItems items = await GetRecommendedItems();

            Song.GreetingMessage = items.Items[0].Title;
            int albumIndex = 0;
            int recentIndex = 0;

            if (items.Items.Any(x => x.Title.ToLower() == "jump back in"))
            {
                albumIndex = items.Items.IndexOf(items.Items.First(x => x.Title.ToLower() == "jump back in"));
                Song.FunTitle = "Jump Back In";
            }
            else if(items.Items.Any(x => x.Title.ToLower() == "recommended for today"))
            {
                albumIndex = items.Items.IndexOf(items.Items.First(x => x.Title.ToLower() == "recommended for today"));
                Song.FunTitle = "Recommended For Today";
            }
            else
            {
                albumIndex = 2;
                Song.FunTitle = items.Items[2].Title;
            }

            if(items.Items.Any(x => x.Title.ToLower() == "recents"))
            {
                recentIndex = items.Items.IndexOf(items.Items.First(x => x.Title.ToLower() == "recents"));
                Song.RecentsTitle = "Recently Played";
            }
            else
            {
                recentIndex = 3;
                Song.RecentsTitle = items.Items[3].Title;
            }


            ListItemCallback callback = new ListItemCallback();
            ListItemCallback callback2 = new ListItemCallback();
            ListItemCallback callback3 = new ListItemCallback();

            remote.Remote.ContentApi.GetChildrenOfItem(items.Items[0], 8, 0).SetResultCallback(callback);
            remote.Remote.ContentApi.GetChildrenOfItem(items.Items[albumIndex], 10, 0).SetResultCallback(callback2);
            remote.Remote.ContentApi.GetChildrenOfItem(items.Items[recentIndex], 15, 0).SetResultCallback(callback3);

            while (callback.Items == null || callback2.Items == null || callback3.Items == null)
                await Task.Delay(10);

            foreach (var item in callback.Items.Items)
            {
                string type = item.Uri.Split(':')[1];

                Song.GridRecommendedItems.Add(new PlayableItem
                {
                    Id = item.Id.Split(':')[2],
                    Type = type,
                    Title = item.Title,
                    Image = item.ImageUri.Raw
                });
            }

            foreach (var item in callback2.Items.Items)
            {
                string type = item.Uri.Split(':')[1];

                Song.JumpBackInItems.Add(new PlayableItem
                {
                    Id = item.Id.Split(':')[2],
                    Type = type,
                    Title = item.Title,
                    Image = item.ImageUri.Raw,
                    Subtitle = item.Subtitle
                });
            }

            foreach (var item in callback3.Items.Items)
            {
                string imageUrl = "";
                string type = item.Uri.Split(':')[1];

                if (item.ImageUri.Raw.StartsWith('h'))
                    imageUrl = item.ImageUri.Raw;
                else
                    imageUrl = $"https://i.scdn.co/image/{item.ImageUri.Raw.Split(':')[2]}";

                Song.RecentlyPlayedItems.Add(new PlayableItem
                {
                    Id = item.Id.Split(':')[2],
                    Type = type,
                    Title = item.Title,
                    Image = imageUrl,
                    Subtitle = item.Subtitle
                });

            }
        };
#endif
    }

    protected override bool OnBackButtonPressed()
    {
        if (lyricsView.IsVisible)
        {
            // lyricsView.IsVisible = false;
            // lyricsView.Dispatcher.Dispatch(() =>
            // {
            // 	lyricsView.TranslateTo(0, 1000, 1000, Easing.SpringOut);
            // });

            lyricsView.IsVisible = false;

            Shell.SetTabBarIsVisible(this, true);
            return true;
        }

        // lyricsView.OnBackButtonPressed();

        return base.OnBackButtonPressed();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        // lyricsView.Dispatcher.Dispatch(() =>
        // {
        // 	lyricsView.TranslateTo(0, 0, 1000, Easing.SpringOut);
        // });

        lyricsView.IsVisible = true;

        Shell.SetTabBarIsVisible(this, false);
    }

    private void TogglePause(object sender, EventArgs e)
    {
        isPlaying = !isPlaying;
        Song.ToggleTimer(isPlaying);

#if ANDROID
        if (isPlaying)
            LyricsView.Remote.PlayerApi.Resume();
        else
            LyricsView.Remote.PlayerApi.Pause();
#endif
    }

    private void FullCardCollapse(object sender, EventArgs e)
    {
        lyricsView.IsVisible = false;
    }

    private void SkipNext(object sender, EventArgs e)
    {
#if ANDROID
        LyricsView.Remote.PlayerApi.SkipNext();
#endif
    }

    private void SkipPrevious(object sender, EventArgs e)
    {
#if ANDROID
        LyricsView.Remote.PlayerApi.SkipPrevious();
#endif
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        // if (LyricsView.Remote != null && Math.Abs(e.NewValue - e.OldValue) > 1000)
        // 	LyricsView.Remote.PlayerApi.SeekTo((long)e.NewValue);
    }

    private async void NavigateToLyricsView(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SearchPage());
    }

    private async void AddLyrics(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LyricsEditor());
    }

    private void DeleteSong(object sender, EventArgs e)
    {
#if ANDROID
        string path = Path.Combine(FileSystem.AppDataDirectory, $"{LyricsView.CurrentTrackId}.json");

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
            FullTrack track = await LyricsView.Spotify.Tracks.Get(LyricsView.CurrentTrackId);
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    public static async Task<ListItems> GetRecommendedItems()
    {
        ListItemCallback callback = new ListItemCallback();
        LyricsView.Remote.ContentApi?.GetRecommendedContentItems("default").SetResultCallback(callback);

        while (callback.Items == null)
        {
            await Task.Delay(10);
        }

        return callback.Items;
    }

    public class ListItemCallback : Java.Lang.Object, IResultCallback
    {
        public ListItems Items { get; set; }

        public void OnResult(Java.Lang.Object? p0)
        {
            if (p0 is ListItems items)
                Items = items;
        }
    }

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
        // albumCoverPlayer.Handler?.DisconnectHandler();
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

    private async void NewId(object sender, EventArgs e)
    {
        await SecureStorage.SetAsync("spotifyId", clientIdEntry.Text);
    }

    private async void NewSecret(object sender, EventArgs e)
    {
        await SecureStorage.SetAsync("spotifySecret", clientSecretEntry.Text);
    }

    private void Switch_Toggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("showInterludes", e.Value);
    }
}