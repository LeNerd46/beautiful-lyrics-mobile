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
	}
}