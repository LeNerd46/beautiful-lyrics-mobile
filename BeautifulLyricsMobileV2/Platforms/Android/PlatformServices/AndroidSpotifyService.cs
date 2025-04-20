using Android.Runtime;
using BeautifulLyricsMobileV2.Entities;
using BeautifulLyricsMobileV2.Services;
using Com.Spotify.Android.Appremote.Api;
using Com.Spotify.Protocol.Client;
using Com.Spotify.Protocol.Types;
using CommunityToolkit.Maui.Alerts;
using Java.Util.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Platforms.Android.PlatformServices
{
	public class AndroidSpotifyService : ISpotifyRemoteService
	{
		private static SpotifyAppRemote Remote;
		public event EventHandler Connected;

		public void SetRemoteClient(object client)
		{
			Remote = client as SpotifyAppRemote;
			Toast.Make("Spotify Connected!");
		}

		public object Client => Remote;

		public async Task<SpotifyPlayerState> GetPlayerState()
		{
			if (Remote == null) return null;
			TaskCompletionSource<SpotifyPlayerState> completeSource = new TaskCompletionSource<SpotifyPlayerState>();

			Remote.PlayerApi?.PlayerState.SetResultCallback(new ResultCallback<PlayerState>(player =>
			{
				Track track = player.Track;

				completeSource.TrySetResult(new SpotifyPlayerState
				{
					IsPaused = player.IsPaused,
					PlaybackPosition = player.PlaybackPosition,
					Track = new SpotifyTrack
					{
						Title = track.Name,
						Uri = track.Uri,
						Image = track.ImageUri.Raw,
						Album = new SpotifyAlbum
						{
							Title = track.Album.Name,
							Uri = track.Album.Uri
						},
						Artist = new SpotifyArtist
						{
							Name = track.Artist.Name,
							Uri = track.Artist.Uri
						},
						Duration = track.Duration
					}
				});
			}));

			return await completeSource.Task;
		}

		public void InvokeConnected()
		{
			Connected?.Invoke(this, EventArgs.Empty);
		}
	}

	// Helper class so I can use lambda expressions like they do in Java
	public class ResultCallback<T>(Action<T> onResult) : Java.Lang.Object, CallResult.IResultCallback where T : Java.Lang.Object
	{
		private readonly Action<T> onResult = onResult;

		public void OnResult(T result) => onResult(result);

		public void OnResult(Java.Lang.Object? p0) => onResult(p0.JavaCast<T>());
	}
}