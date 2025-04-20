using Newtonsoft.Json;
using NTextCat;
using NTextCat.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	internal static class LyricUtilities
	{
		private static readonly string[] RightToLeftLanguages =
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

		internal static NaturalAlignment GetNaturalAlignment(string language) => RightToLeftLanguages.Contains(language) ? NaturalAlignment.Right : NaturalAlignment.Left;

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
			TransformedLyrics lyrics = new TransformedLyrics
			{
				Lyrics = providedLyrics
			};

			List<TimeMetadata> vocalTimes = [];

			if (lyrics.Lyrics.StaticLyrics is StaticSyncedLyrics staticLyrics)
			{
				string textToProcess = string.Join("\n", staticLyrics.Lines.Select(x => x.Text));

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

						vocalTimes.Add(new TimeMetadata
						{
							StartTime = vocal.StartTime,
							EndTime = vocal.EndTime
						});
					}

					lineLyrics.Content = [.. lines];
					string textToProcess = lines.Join("\n");

					lyrics.Language = GetLanguage(textToProcess);
					lyrics.NaturalAlignment = GetNaturalAlignment(lyrics.Language);

					// Romanization
				}

				// Check if first vocal group needs an interlude before it
				bool addedStartInterlude = false;
				var firstVocalGroup = vocalTimes[0];
				TimeMetadata time = new TimeMetadata
				{
					StartTime = -1,
					EndTime = -1
				};

				if (firstVocalGroup.StartTime >= 2)
				{
					vocalTimes.Insert(0, time);

					var newList = lineLyrics.Content.ToList();
					newList.Insert(0, new Interlude
					{
						Time = time
					});

					lineLyrics.Content = [.. newList];
					addedStartInterlude = true;
				}

				for (int i = vocalTimes.Count; i > (addedStartInterlude ? 1 : 0); i--)
				{
					var endingVocalGroup = vocalTimes[i];
					var startingVocalGroup = vocalTimes[i - 1];

					if (endingVocalGroup.StartTime - startingVocalGroup.EndTime >= 2)
					{
						vocalTimes.Insert(i, time);

						TimeMetadata newTime = new TimeMetadata
						{
							StartTime = startingVocalGroup.StartTime,
							EndTime = endingVocalGroup.EndTime - 0.25f
						};

						lineLyrics.Content.Insert(i, new Interlude
						{
							Time = newTime
						});
					}
				}
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

						double startTime = vocalSet.Lead.StartTime;
						double endTime = vocalSet.Lead.EndTime;

						if (vocalSet.Background != null)
						{
							foreach (var backgroundVocal in vocalSet.Background)
							{
								startTime = Math.Min(startTime, backgroundVocal.StartTime);
								endTime = Math.Max(endTime, backgroundVocal.EndTime);
							}
						}

						vocalTimes.Add(new TimeMetadata
						{
							StartTime = startTime,
							EndTime = endTime
						});
					}
				}

				string textToProcess = lines.Join("\n");

				lyrics.Language = GetLanguage(textToProcess);
				lyrics.NaturalAlignment = GetNaturalAlignment(lyrics.Language);

				// Romanization

				// Check if first vocal group needs an interlude before it
				bool addedStartInterlude = false;
				var firstVocalGroup = vocalTimes[0];

				TimeMetadata time = new TimeMetadata
				{
					StartTime = -1,
					EndTime = -1
				};

				if (firstVocalGroup.StartTime >= 2)
				{
					vocalTimes.Insert(0, time);
					syllableLyrics.Content.Insert(0, new Interlude
					{
						Time = time
					});

					addedStartInterlude = true;
				}

				for (int i = vocalTimes.Count - 1; i > (addedStartInterlude ? 1 : 0); i--)
				{
					var endingVocalGroup = vocalTimes[i];
					var startingVocalGroup = vocalTimes[i - 1];

					if (endingVocalGroup.StartTime - startingVocalGroup.EndTime >= 2)
					{
						vocalTimes.Insert(i, time);

						TimeMetadata newTime = new TimeMetadata
						{
							StartTime = startingVocalGroup.EndTime,
							EndTime = endingVocalGroup.StartTime - 0.25f
						};

						syllableLyrics.Content.Insert(i, new Interlude
						{
							Time = newTime
						});
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
