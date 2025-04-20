using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	public class ProviderLyrics
	{
		public StaticSyncedLyrics StaticLyrics { get; set; }
		public LineSyncedLyrics LineLyrics { get; set; }
		public SyllableSyncedLyrics SyllableLyrics { get; set; }
	}

	public class SyllableSyncedLyrics : TimeMetadata
	{
		public string Type { get => "Syllable"; }
		public List<object> Content { get; set; } // Either SyllableVocalSet or Interlude
	}

	public class LineSyncedLyrics : TimeMetadata
	{
		public string Type { get => "Line"; }
		public List<object> Content { get; set; } // Either LineVocal or Interlude
	}

	public class StaticSyncedLyrics
	{
		public string Type { get => "Static"; }
		public List<TextMetadata> Lines { get; set; }
	}

	// SYLLABLE 

	public class SyllableVocalSet
	{
		public string Type { get => "Vocal"; }
		public bool OppositeAligned { get; set; }

		public SyllableVocal Lead { get; set; }
		public List<SyllableVocal> Background { get; set; }
	}

	public class SyllableVocal : TimeMetadata
	{
		public List<SyllableMetadata> Syllables { get; set; }
	}

	public class SyllableMetadata : VocalMetadata
	{
		public bool IsPartOfWord { get; set; }

		internal List<int> Splits { get; set; }
	}

	// LINE

	public class LineVocal : VocalMetadata
	{
		public string Type { get => "Vocal"; }
		public bool OppositeAligned { get; set; }
	}

	// INTERLUDE

	public class LineInfo { }

	public class Interlude : LineInfo
	{
		public TimeMetadata Time { get; set; }
		public SyncType Type { get => SyncType.Interlude; }

		[JsonProperty("StartTime")]
		private double startTime { set => Time.StartTime = value; }

		[JsonProperty("EndTime")]
		private double endTime { set => Time.EndTime = value; }
	}

	public class VocalMetadata : TimeMetadata
	{
		public string Text { get; set; }
		public string? RomanizedText { get; set; }
	}

	public class TextMetadata
	{
		public string Text { get; set; }
		public string? RomanizedText { get; set; }
	}

	public class TimeMetadata
	{
		public double StartTime { get; set; }
		public double EndTime { get; set; }
	}

	public enum SyncType
	{
		Static,
		Line,
		Vocal,
		Syllable,
		Interlude
	}
}
