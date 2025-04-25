using BeautifulLyricsMobileV2.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Services
{
	public interface ISpotifyRemoteService
	{
		public void SetRemoteClient(object client);
		public event EventHandler Connected;

		public void InvokeConnected();

		/// <summary>
		/// Gets the actual Spotify client
		/// </summary>
		/// <returns>An object that can be casted to the platform specific client</returns>
		public object Client { get; }

		/// <summary>
		/// Gets information about the current track being played
		/// </summary>
		public Task<SpotifyPlayerState> GetPlayerState();

		/// <summary>
		/// Gets the state of a track in the library
		/// </summary>
		/// <param name="id">The ID of the item</param>
		/// <param name="type">The type of item</param>
		/// <returns>Library information about the current item</returns>
		public Task<SpotifyLibraryState> GetLibraryState(string id, PlayableItemType type = PlayableItemType.Track);

		/// <summary>
		/// Saves an item to the library
		/// </summary>
		/// <param name="id">ID of the item</param>
		/// <param name="type">The type of item</param>
		public Task SaveLibraryItem(string id, PlayableItemType type = PlayableItemType.Track);

		/// <summary>
		/// Removes an item from the library
		/// </summary>
		/// <param name="id">ID of the item</param>
		/// <param name="type">The type of item</param>
		/// <returns></returns>
		public Task RemoveLibraryItem(string id, PlayableItemType type = PlayableItemType.Track);

		/// <summary>
		/// Resumes playback of the current track
		/// </summary>
		public void Resume();
	}
}