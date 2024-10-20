using Microsoft.Maui.Animations;
using Newtonsoft.Json;
using NTextCat;
using NTextCat.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsAndroid.Entities
{
	internal static class LyricUtilities
	{
		internal static readonly string[] RightToLeftLangauges =
		{
			// Persian
			"pes", "urd",

			// Arabic Languages
			"arb", "uig",

			// Hebrew Languages
			"heb", "ydd",

			// Mende Languages
			"men"
		};

		internal static NaturalAlignment GetNaturalAlignment(string language) => RightToLeftLangauges.Contains(language) ? NaturalAlignment.Right : NaturalAlignment.Left;

		internal static string GetLanguage(string text)
		{
			var factory = new RankedLanguageIdentifierFactory();

			using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync("Core14.profile.xml").GetAwaiter().GetResult();

			var identifier = factory.Load(fileStream);
			var languages = identifier.Identify(text);

			return languages.FirstOrDefault().Item1.Iso639_3;
		}

		internal static TransformedLyrics TransformLyrics(ProviderLyrics providedLyrics)
		{
			TransformedLyrics lyrics = new TransformedLyrics()
			{
				Lyrics = providedLyrics
			};

			if (lyrics.Lyrics.StaticLyrics is StaticSyncedLyrics staticLyrics)
			{
				string textToProcess = staticLyrics.Lines[0].Text;

				for (int i = 1; i < staticLyrics.Lines.Count; i++)
				{
					textToProcess += $"\n{staticLyrics.Lines[i].Text}";
				}

				lyrics.Language = GetLanguage(textToProcess);
				lyrics.NaturalAlignment = GetNaturalAlignment(lyrics.Language);

				// Romanization

				return lyrics;
			}
			else if (lyrics.Lyrics.LineLyrics is LineSyncedLyrics lineLyrics)
			{
				List<string> lines = [];
				List<LineVocal> lineVocals = [];

				foreach (var vocalGroup in lineLyrics.Content)
				{
					LineVocal deserialize = JsonConvert.DeserializeObject<LineVocal>(vocalGroup.ToString());

					if (deserialize is LineVocal vocal)
					{
						lines.Add(vocal.Text);
						lineVocals.Add(vocal);
					}
				}

				lineLyrics.Content = lineVocals.ToList<object>();
				string textToProcess = lines.Join("\n");

				lyrics.Language = GetLanguage(textToProcess);
				lyrics.NaturalAlignment = GetNaturalAlignment(lyrics.Language);

				// Romanization

			}
			else if (lyrics.Lyrics.SyllableLyrics is SyllableSyncedLyrics syllableLyrics)
			{
				List<string> lines = [];

				foreach (var vocalGroup in syllableLyrics.Content)
				{
					SyllableVocalSet syllableVocalSet = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());

					if (syllableVocalSet is SyllableVocalSet vocalSet)
					{
						string text = vocalSet.Lead.Syllables[0].Text;

						for (int i = 1; i < vocalSet.Lead.Syllables.Count; i++)
						{
							var syllable = vocalSet.Lead.Syllables[i];
							text += (syllable.IsPartOfWord ? "" : " ") + syllable.Text;
						}

						lines.Add(text);
					}
				}

				string textToProcess = lines.Join("\n");

				lyrics.Language = GetLanguage(textToProcess);
				lyrics.NaturalAlignment = GetNaturalAlignment(lyrics.Language);

				// Romanization
			}

			List<TimeMetadata> vocalTimes = new List<TimeMetadata>();

			if (lyrics.Lyrics.LineLyrics is LineSyncedLyrics lineLyricsToo)
			{
				foreach(var vocal in lineLyricsToo.Content)
				{
					if(vocal is LineVocal line)
					{
						vocalTimes.Add(new TimeMetadata
						{
							StartTime = line.StartTime,
							EndTime = line.EndTime
						});
					}
				}
			}
			else if (lyrics.Lyrics.SyllableLyrics is SyllableSyncedLyrics syllableLyricsToo)
			{
				foreach (var vocalGroup in syllableLyricsToo.Content)
				{
					SyllableVocalSet syllableVocalSet = JsonConvert.DeserializeObject<SyllableVocalSet>(vocalGroup.ToString());

					if (syllableVocalSet is SyllableVocalSet vocalSetToo)
					{
						double startTime = vocalSetToo.Lead.StartTime;
						double endTime = vocalSetToo.Lead.EndTime;

						if (vocalSetToo.Background != null)
						{
							foreach (var backgroundVocal in vocalSetToo.Background)
							{
								startTime = Math.Min(startTime, backgroundVocal.StartTime);
								endTime = Math.Max(endTime, backgroundVocal.EndTime);
							}
						}

						vocalTimes.Add(new TimeMetadata()
						{
							StartTime = startTime,
							EndTime = endTime
						});
					}
				}
			}

			// Check if first vocal group needs an interlude before it
			bool addedStartInterlude = false;
			var firstVocalGroup = vocalTimes[0];

			if (firstVocalGroup.StartTime >= 2)
			{
				vocalTimes.Insert(0, new TimeMetadata()
				{
					StartTime = -1,
					EndTime = -1
				});

				if (lyrics.Lyrics.LineLyrics is LineSyncedLyrics lineLyricsThree)
				{
					TimeMetadata time = new TimeMetadata()
					{
						StartTime = -1,
						EndTime = -1
					};

					var newList = lineLyricsThree.Content.ToList();
					newList.Insert(0, new Interlude()
					{
						Time = time
					});

					lineLyricsThree.Content = [.. newList];
				}
				else if (lyrics.Lyrics.SyllableLyrics is SyllableSyncedLyrics syllableLyricsThree)
				{
					TimeMetadata time = new TimeMetadata()
					{
						StartTime = -1,
						EndTime = -1
					};

					var newList = syllableLyricsThree.Content.ToList();
					newList.Insert(0, new Interlude()
					{
						Time = time
					});

					syllableLyricsThree.Content = [.. newList];
				}

				addedStartInterlude = true;
			}

			for (int i = vocalTimes.Count - 1; i > (addedStartInterlude ? 1 : 0); i--)
			{
				var endingVocalGroup = vocalTimes[i];
				var startingVocalGroup = vocalTimes[i - 1];

				if (endingVocalGroup.StartTime - startingVocalGroup.EndTime >= 2)
				{
					vocalTimes.Insert(i, new TimeMetadata()
					{
						StartTime = -1,
						EndTime = -1
					});

					if (lyrics.Lyrics.StaticLyrics is StaticSyncedLyrics)
						return lyrics;
					else if (lyrics.Lyrics.LineLyrics is LineSyncedLyrics lineLyricsFour)
					{
						TimeMetadata time = new TimeMetadata()
						{
							StartTime = startingVocalGroup.EndTime,
							EndTime = endingVocalGroup.StartTime - 0.25f
						};

						var newList = lineLyricsFour.Content.ToList();
						newList.Insert(i, new Interlude()
						{
							Time = time
						});

						lineLyricsFour.Content = [.. newList];
					}
					else if (lyrics.Lyrics.SyllableLyrics is SyllableSyncedLyrics syllableLyricsFour)
					{
						TimeMetadata time = new TimeMetadata()
						{
							StartTime = startingVocalGroup.EndTime,
							EndTime = endingVocalGroup.StartTime - 0.25f
						};

						var newList = syllableLyricsFour.Content.ToList();
						newList.Insert(i, new Interlude()
						{
							Time = time
						});

						syllableLyricsFour.Content = [.. newList];
					}
				}
			}

			return lyrics;
		}
	}

	internal class TransformedLyrics
	{
		public NaturalAlignment NaturalAlignment { get; set; }
		public string Language { get; set; }
		public string? RomanizedLanguage { get; set; }

		public ProviderLyrics Lyrics { get; set; }
	}

	internal enum NaturalAlignment
	{
		Right,
		Left
	}
}