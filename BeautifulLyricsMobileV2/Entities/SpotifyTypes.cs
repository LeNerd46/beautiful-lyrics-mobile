using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	public class SpotifyPlayerState
	{
		public bool IsPaused { get; set; }
		public long PlaybackPosition { get; set; }
		public SpotifyTrack Track { get; set; }
	}

	public class SpotifyTrack
	{
		/// <summary>
		/// The name of the track
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The ID of the track
		/// </summary>
		public string Id { get => Uri.Split(':')[2]; }

		/// <summary>
		/// The Spotify URI
		/// </summary>
		/// <example>spotify:track:6dOtVTDdiauQNBQEDOtlAB</example>
		public string Uri { get; set; }

		/// <summary>
		/// The URL for the image
		/// </summary>
		public string Image { get; set; }

		/// <summary>
		/// The album this track is a part of
		/// </summary>
		public SpotifyAlbum Album { get; set; }

		/// <summary>
		/// The main artist of this track
		/// </summary>
		public SpotifyArtist Artist { get; set; }

		/// <summary>
		/// All artists of this track
		/// </summary>
		public List<SpotifyArtist> Artists { get; set; }

		/// <summary>
		/// Duration of the track
		/// </summary>
		public long Duration { get; set; }
	}

	public class SpotifyAlbum
	{
		/// <summary>
		/// The title of the album
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The Spotify Uri
		/// </summary>
		/// <example>spotify:album:7aJuG4TFXa2hmE4z1yxc3n</example>
		public string Uri { get; set; }

		/// <summary>
		/// The ID of the album
		/// </summary>
		public string Id { get => Uri.Split(':')[2]; }
	}

	public class SpotifyArtist
	{
		/// <summary>
		/// The name of the artist
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The Spotify URI
		/// </summary>
		/// <example>spotify:artist:06HL4z0CvFAxyc27GXpf02</example>
		public string Uri { get; set; }

		/// <summary>
		/// The ID of the artist
		/// </summary>
		public string Id { get => Uri.Split(':')[2]; }
	}
}