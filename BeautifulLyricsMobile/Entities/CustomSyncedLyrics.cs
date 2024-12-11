using BeautifulLyricsAndroid.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Entities
{
	public class CustomSyncedLyrics
	{
		public List<SyllableVocalSet> Lines { get; }

		public string Title { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }

		public CustomSyncedLyrics(List<SyllableVocalSet> lines)
		{
			Lines = lines;
		}
	}
}
