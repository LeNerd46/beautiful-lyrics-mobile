using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsAndroid.Entities
{
	internal class ProviderLyrics
	{
		public StaticSyncedLyrics StaticLyrics { get; set; }
		public LineSyncedLyrics LineLyrics { get; set; }
		public SyllableSyncedLyrics SyllableLyrics { get; set; }
	}

	internal class LineInfo { }

	internal class SyllableSyncedLyrics : TimeMetadata
	{
		public string Type { get; set; } = "Syllable";
		public List<object> Content { get; set; } // Either SyllableVocalSet or Interlude
	}

	internal class LineSyncedLyrics : TimeMetadata
	{
		public string Type { get; set; } = "Line";
		public List<object> Content { get; set; } // Either LineVocal or Interlude
	}

	internal class StaticSyncedLyrics
	{
		public string Type { get; set; } = "Static";
		public List<TextMetadata> Lines { get; set; }
	}

	#region Syllable
	internal class SyllableVocalSet
	{
		public string Type { get; set; } = "Vocal";
		public bool OppositeAligned { get; set; }

		public SyllableVocal Lead { get; set; }
		public List<SyllableVocal> Background { get; set; }
	}

	internal class SyllableVocal : TimeMetadata
	{
		public List<SyllableMetadata> Syllables { get; set; }
	}

	internal class SyllableMetadata : VocalMetadata
	{
		public bool IsPartOfWord { get; set; }

		internal bool IsEmphasized { get; set; }
		internal bool IsStartOfWord { get; set; }
		internal bool IsEndOfWord { get; set; }
	}

	#endregion

	#region Line

	internal class LineVocal : VocalMetadata
	{
		public string Type { get; set; } = "Vocal";
		public bool OppositeAligned { get; set; }
	}

	#endregion

	internal class VocalMetadata : TimeMetadata
	{
		public string Text { get; set; }
		public string? RomanizedText { get; set; }
	}

	internal class Interlude : LineInfo
	{
		public TimeMetadata Time { get; set; }
		public SyncType Type { get => SyncType.Interlude; }

		[JsonProperty("StartTime")]
		private double startTime { set => Time.StartTime = value; }

		[JsonProperty("EndTime")]
		private double endTime { set => Time.EndTime = value; }
	}

	internal class TextMetadata
	{
		public string Text { get; set; }
		public string? RomanizedText { get; set; }
	}

	internal class TimeMetadata
	{
		public double StartTime { get; set; }
		public double EndTime { get; set; }
	}

	internal enum SyncType
	{
		Static,
		Line,
		Vocal,
		Syllable,
		Interlude
	}
}