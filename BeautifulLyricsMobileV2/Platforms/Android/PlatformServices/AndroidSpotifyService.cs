using Android.AdServices.OnDevicePersonalization;
using Android.Runtime;
using BeautifulLyricsMobileV2.Entities;
using BeautifulLyricsMobileV2.Services;
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Client;
using Com.Spotify.Protocol.Types;
using CommunityToolkit.Maui.Alerts;
using Java.Lang;

namespace BeautifulLyricsMobileV2.Platforms.Android.PlatformServices
{
	public class AndroidSpotifyService : ISpotifyRemoteService
	{
		private static SpotifyAppRemote Remote;
		public event EventHandler Connected;
		public event EventHandler Resumed;

		public void SetRemoteClient(object client)
		{
			Remote = (client as SpotifyAppRemote)!;
			Toast.Make("Spotify Connected!");
		}

		public object Client => Remote;

		public async Task<SpotifyPlayerState> GetPlayerState()
		{
			if (Remote == null) return null;
			TaskCompletionSource<SpotifyPlayerState> completeSource = new TaskCompletionSource<SpotifyPlayerState>();

			Remote.PlayerApi?.PlayerState?.SetResultCallback(new ResultCallback<PlayerState>(player =>
			{
				if (player?.Track == null)
					return;

				Track track = player.Track;
				var rawImage = track.ImageUri?.Raw;
				string imageUrl = rawImage != null && rawImage.Contains(':') && rawImage.Split(':').Length >= 3 ? $"https://i.scdn.co/image/{rawImage.Split(':')[2]}" : "https://t4.ftcdn.net/jpg/06/71/92/37/360_F_671923740_x0zOL3OIuUAnSF6sr7PuznCI5bQFKhI0.jpg";

				completeSource.TrySetResult(new SpotifyPlayerState
				{
					IsPaused = player.IsPaused,
					PlaybackPosition = player.PlaybackPosition,
					Track = new SpotifyTrack
					{
						Title = track.Name!,
						Uri = track.Uri!,
						Image = imageUrl,
						SpotifyImage = rawImage!,
						Album = new SpotifyAlbum
						{
							Title = track.Album?.Name ?? "Unknown Album",
							Uri = track.Album?.Uri!
						},
						Artist = new SpotifyArtist
						{
							Name = track.Artist?.Name ?? "Unknown Artist",
							Uri = track.Artist?.Uri!
						},
						Duration = track.Duration
					}
				});
			}));

			return await completeSource.Task;
		}

		public void InvokeConnected() => Connected?.Invoke(this, EventArgs.Empty);
		public void InvokeResumed() => Resumed?.Invoke(this, EventArgs.Empty);
		public void Resume() => Remote?.PlayerApi?.Resume();

		public async Task<SpotifyLibraryState> GetLibraryState(string id, PlayableItemType type = PlayableItemType.Track)
		{
			if (Remote == null) return null;
			TaskCompletionSource<SpotifyLibraryState> completeSource = new TaskCompletionSource<SpotifyLibraryState>();

			string uri = type switch
			{
				PlayableItemType.Track => $"spotify:track:{id}",
				PlayableItemType.Album => $"spotify:album:{id}",
				PlayableItemType.Artist => $"spotify:artist:{id}",
				PlayableItemType.Playlist => $"spotify:playlist:{id}",
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

			Remote.UserApi?.GetLibraryState(uri)?.SetResultCallback(new ResultCallback<LibraryState>(state =>
			{
				if (state == null)
				{
					SpotifyLibraryState nullState = new SpotifyLibraryState
					{
						Uri = uri,
						CanAdd = false,
						IsAdded = false
					};

					completeSource.TrySetResult(nullState);
				}
				else
				{
					SpotifyLibraryState libraryState = new SpotifyLibraryState
					{
						Uri = string.IsNullOrWhiteSpace(state.Uri) ? uri : state.Uri,
						IsAdded = state.IsAdded,
						CanAdd = state.CanAdd
					};

					completeSource.TrySetResult(libraryState);
				}
			}));

			return await completeSource.Task;
		}

		public async Task SaveLibraryItem(string id, PlayableItemType type = PlayableItemType.Track)
		{
			if (Remote == null) return;
			TaskCompletionSource completionSource = new TaskCompletionSource();

			string uri = type switch
			{
				PlayableItemType.Track => $"spotify:track:{id}",
				PlayableItemType.Album => $"spotify:album:{id}",
				PlayableItemType.Artist => $"spotify:artist:{id}",
				PlayableItemType.Playlist => $"spotify:playlist:{id}",
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

			Remote.UserApi?.AddToLibrary(uri)?.SetResultCallback(new ResultCallback<Empty>(result => completionSource.TrySetResult()));
			await completionSource.Task;
		}

		public async Task RemoveLibraryItem(string id, PlayableItemType type = PlayableItemType.Track)
		{
			if (Remote == null) return;
			TaskCompletionSource completionSource = new TaskCompletionSource();

			string uri = type switch
			{
				PlayableItemType.Track => $"spotify:track:{id}",
				PlayableItemType.Album => $"spotify:album:{id}",
				PlayableItemType.Artist => $"spotify:artist:{id}",
				PlayableItemType.Playlist => $"spotify:playlist:{id}",
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

			Remote.UserApi?.RemoveFromLibrary(uri)?.SetResultCallback(new ResultCallback<Empty>(result => completionSource.TrySetResult()));
			await completionSource.Task;
		}
	}


	// Helper class so I can use lambda expressions like they do in Java
	public class ResultCallback<T>(Action<T> onResult) : Java.Lang.Object, CallResult.IResultCallback where T : Java.Lang.Object
	{
		private readonly Action<T> onResult = onResult;

		public void OnResult(T result) => onResult(result);

		public void OnResult(Java.Lang.Object? p0) => onResult(p0.JavaCast<T>());
	}

	public class ErrorCallback<T>(Action<T> onError) : Java.Lang.Object, IErrorCallback where T : Java.Lang.Throwable
	{
		private readonly Action<T> onError = onError;

		public void OnError(T result) => onError(result);

		public void OnError(Java.Lang.Throwable? p0) => onError(p0.JavaCast<T>());
	}
}