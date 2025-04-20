using BeautifulLyricsMobileV2.Controls;
using CommunityToolkit.Maui.Alerts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	class SyllableVocals : ISyncedVocals
	{
		public FlexLayout Container { get; set; }
		public event EventHandler<View> ActivityChanged;

		public double StartTime { get; set; }
		public double Duration { get; set; }
		public List<AnimatedSyllable> Syllables { get; } = [];
		private bool Active { get; set; }
		public bool Sung { get; set; }

		private LyricState State = LyricState.Idle;
		private bool isSleeping = true;

		public bool IsBackground { get; set; }
		public bool OppositeAligned { get; set; }

		public SyllableVocals(FlexLayout lineContainer, List<SyllableMetadata> syllables, bool isBackground, bool isRomanized, bool oppositeAligned)
		{
			Container = lineContainer;
			List<View> views = [];

			Active = false;
			IsBackground = isBackground;
			OppositeAligned = oppositeAligned;

			StartTime = syllables[0].StartTime;
			Duration = syllables[^1].EndTime - StartTime + 0.3d;

			List<List<SyllableMetadata>> syllableGroups = [];
			List<SyllableMetadata> currentGroup = [];

			List<View> visualElements = [];
			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.First();

			Style lyricLabel = styles["LyricLabel"] as Style;
			Style backgroundLyricLabel = styles["BackgroundLyricLabel"] as Style;
			Style lyricEmphasizedLabel = styles["LyricEmphasizedLabel"] as Style;
			Style backgroundLyricEmphasizedLabel = styles["BackgroundLyricEmphasizedLabel"] as Style;

			lineContainer.BatchBegin();

			// Go through and create our syllable groups
			foreach (var syllableMetadata in syllables)
			{
				currentGroup.Add(syllableMetadata);

				if (!syllableMetadata.IsPartOfWord)
				{
					syllableGroups.Add(currentGroup);
					currentGroup = [];
				}
			}

			if (currentGroup.Count > 0)
				syllableGroups.Add(currentGroup);

			// Go through and start building our visuals
			foreach (var syllableGroup in syllableGroups)
			{
				int syllableCount = syllableGroup.Count;
				bool isInWordGroup = syllableCount > 1;

				int index = 0;

				HorizontalStackLayout wordGroup = null;
				bool firstSyllable = true;

				foreach (var syllableMetadata in syllableGroup)
				{
					bool isEmphasized = IsEmphasized(syllableMetadata, isRomanized);

					GradientLabel syllableLabel = new GradientLabel
					{
						MaxLines = 1
					};

					List<AnimatedLetter> letters = [];
					HorizontalStackLayout emphasisGroup = [];

					if (!syllableMetadata.IsPartOfWord)
					{
						if (!isEmphasized)
						{
							if (wordGroup != null)
							{
								syllableLabel.Style = isBackground ? backgroundLyricLabel : lyricLabel;
								syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

								wordGroup.Add(syllableLabel);
								views.Add(wordGroup);

								wordGroup = null;
							}
							else
							{
								syllableLabel.Style = isBackground ? backgroundLyricLabel : lyricLabel;
								syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

								views.Add(syllableLabel);
							}
						}
						else
						{
							// Determine whether or not out content is a set of letters or a single text
							List<string> letterTexts = isRomanized ? [.. syllableMetadata.RomanizedText.Select(x => x.ToString())] : [.. syllableMetadata.Text.Select(x => x.ToString())];
							emphasisGroup.Margin = new Thickness(0, 0, 2, 0);

							double relativeTimestep = 1d / letterTexts.Count;

							letters.Clear();
							double relativeTimestamp = 0;

							foreach (var letter in letterTexts)
							{
								GradientLabel letterLabel = new GradientLabel
								{
									Text = letter,
									Style = IsBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel
								};

								emphasisGroup.Add(letterLabel);

								letters.Add(new AnimatedLetter
								{
									Start = relativeTimestamp,
									Duration = relativeTimestep,
									GlowDuration = 1 - relativeTimestamp,
									LiveText = new LiveText
									{
										Object = letterLabel,
										Springs = CreateSprings()
									}
								});

								relativeTimestamp += relativeTimestep;
							}

							if (wordGroup != null)
							{
								wordGroup.Add(emphasisGroup);
								views.Add(wordGroup);
							}
							else
								views.Add(emphasisGroup);
						}
					}
					else
					{
						if (!isEmphasized)
						{
							wordGroup ??= [];

							syllableLabel.Style = isBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel;
							syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

							wordGroup.Add(syllableLabel);
						}
						else
						{
							// Determine whether or not our content is a set of letters or a single text
							List<string> letterTexts = isRomanized ? [.. syllableMetadata.RomanizedText.Select(x => x.ToString())] : [.. syllableMetadata.Text.Select(x => x.ToString())];

							double relativeTimestep = 1d / letterTexts.Count;

							letters.Clear();
							double relativeTimestamp = 0;

							foreach (var letter in letterTexts)
							{
								GradientLabel letterLabel = new GradientLabel
								{
									Text = letter,
									Style = IsBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel
								};

								emphasisGroup.Add(letterLabel);

								letters.Add(new AnimatedLetter
								{
									Start = relativeTimestamp,
									Duration = relativeTimestep,
									GlowDuration = 1 - relativeTimestamp,
									LiveText = new LiveText
									{
										Object = letterLabel,
										Springs = CreateSprings()
									}
								});

								relativeTimestamp += relativeTimestep;
							}

							wordGroup ??= [];
							wordGroup.Add(emphasisGroup);
						}
					}

					double relativeStart = syllableMetadata.StartTime - StartTime;
					double relativeEnd = syllableMetadata.EndTime - StartTime;

					double relativeStartScale = relativeStart / Duration;
					double relativeEndScale = relativeEnd / Duration;

					double duration = relativeEnd - relativeStart;
					double durationScale = relativeEndScale - relativeStartScale;

					LiveText syllableLiveText = new LiveText
					{
						Object = isEmphasized ? emphasisGroup : syllableLabel,
						Springs = CreateSprings()
					};

					if (isEmphasized)
					{
						Syllables.Add(new AnimatedSyllable
						{
							Type = "Letters",
							Start = relativeStart,
							Duration = duration,
							StartScale = relativeStartScale,
							DurationScale = durationScale,
							LiveText = syllableLiveText,
							Letters = letters
						});
					}
					else
					{
						Syllables.Add(new AnimatedSyllable
						{
							Type = "Syllable",
							Start = relativeStart,
							Duration = duration,
							StartScale = relativeStartScale,
							DurationScale = durationScale,
							LiveText = syllableLiveText
						});
					}

					index++;
				}
			}

			views.ForEach(Container.Add);
			lineContainer.BatchCommit();
			SetToGeneralState(false);
		}

		private Springs CreateSprings() => new Springs
		{
			Scale = new Spring(0, 0.6f, 0.7f),
			YOffset = new Spring(0, 0.4f, 1.25f),
			Glow = new Spring(0, 0.5f, 1f)
		};

		private void SetToGeneralState(bool state)
		{
			double timeScale = state ? 1 : 0;

			foreach (var syllable in Syllables)
			{
				UpdateLiveTextState(syllable.LiveText, timeScale, timeScale, true);
				UpdateLiveTextVisuals(syllable.LiveText, false, timeScale, 0);

				if (syllable.Type == "Letters")
				{
					foreach (var letter in syllable.Letters)
					{
						UpdateLiveTextState(letter.LiveText, timeScale, timeScale, true);
						UpdateLiveTextVisuals(letter.LiveText, true, timeScale, 0);
					}
				}
			}

			State = state ? LyricState.Sung : LyricState.Idle;
		}

		private void UpdateLiveTextState(LiveText liveText, double timeScale, double glowTimeScale, bool forceTo = true)
		{
			Spline scaleSpline = GetSpline(scaleRange);
			Spline yOffsetSpline = GetSpline(yOffsetRange);
			Spline glowSpline = GetSpline(glowRange);

			double scale = scaleSpline.At(timeScale);
			double yOffset = yOffsetSpline.At(timeScale);
			double glow = glowSpline.At(glowTimeScale);

			if (forceTo)
			{
				liveText.Springs.Scale.Set(scale);
				liveText.Springs.YOffset.Set(yOffset);
				liveText.Springs.Glow.Set(glow);
			}
			else
			{
				liveText.Springs.Scale.Final = scale;
				liveText.Springs.YOffset.Final = yOffset;
				liveText.Springs.Glow.Final = glow;
			}
		}

		private bool UpdateLiveTextVisuals(LiveText liveText, bool isEmphasized, double timeScale, double deltaTime)
		{
			double scale = liveText.Springs.Scale.Update(deltaTime);
			double yOffset = liveText.Springs.YOffset.Update(deltaTime) * 50;
			double glow = liveText.Springs.Glow.Update(deltaTime);

			float gradientProgress = (int)Math.Round(-20 + 120 * timeScale);

			if (liveText.Object is GradientLabel label)
			{
				label.Dispatcher.Dispatch(() =>
				{
					if (double.IsNaN(scale) || double.IsInfinity(scale)) return;

					label.Scale = scale;
					label.TranslationY = yOffset * (isEmphasized ? 1.5d : 1d);
					label.Progress = gradientProgress;

					label.ShadowRadius = (float)(4 * (2 * (float)glow * (isEmphasized ? 3d : 1d)));
					label.ShadowOpacity = (float)Math.Max(0, Math.Min(1, glow * (isEmphasized ? 1 : 0.4f)));
				});
			}

			return liveText.Springs.Scale.Sleeping && liveText.Springs.YOffset.Sleeping && liveText.Springs.Glow.Sleeping;
		}

		public void Animate(double songTimestamp, double deltaTime, bool isImmediate = true)
		{
			double relativeTime = songTimestamp - StartTime;

			bool pastStart = relativeTime >= 0;
			bool beforeEnd = relativeTime <= Duration;
			bool isActive = pastStart && beforeEnd;
			Active = isActive;

			LyricState stateNow = isActive ? LyricState.Active : pastStart ? LyricState.Sung : LyricState.Idle;

			bool stateChanged = stateNow != State;
			bool shouldUpdateVisualState = stateChanged || isActive || isImmediate;

			if (stateChanged)
			{
				State = stateNow;
				EvaluateClassState();

				if (State == LyricState.Active)
					ActivityChanged?.Invoke(this, Container);
			}

			this.isSleeping = !shouldUpdateVisualState;
			bool isMoving = this.isSleeping == false;

			if (shouldUpdateVisualState || isMoving)
			{
				double timeScale = Math.Max(0, Math.Min((double)relativeTime / (double)Duration, 1));
				bool isSleeping = true;

				foreach (var syllable in Syllables)
				{
					double syllableTimeScale = Math.Max(0, Math.Min((double)(timeScale - syllable.StartScale) / (double)syllable.DurationScale, 1));

					if (syllable.Type == "Letters")
					{
						double timeAlpha = Math.Sin(syllableTimeScale * (Math.PI / 2)); // easeSinOut

						foreach (var letter in syllable.Letters)
						{
							double letterTime = timeAlpha - letter.Start;
							double letterTimeScale = Math.Max(0, Math.Min(letterTime / letter.Duration, 1));
							double glowTimeScale = Math.Max(0, Math.Min(letterTime / letter.GlowDuration, 1));

							if (shouldUpdateVisualState)
								UpdateLiveTextState(letter.LiveText, letterTimeScale, glowTimeScale, isImmediate);

							if (isMoving)
							{
								bool letterIsSleeping = UpdateLiveTextVisuals(letter.LiveText, true, letterTimeScale, deltaTime);

								if (!letterIsSleeping)
									isSleeping = false;
							}
						}
					}

					if (shouldUpdateVisualState)
						UpdateLiveTextState(syllable.LiveText, syllableTimeScale, syllableTimeScale, isImmediate);

					if (isMoving)
					{
						bool syllableIsSleeping = UpdateLiveTextVisuals(syllable.LiveText, false, syllableTimeScale, deltaTime);

						if (!syllableIsSleeping)
							isSleeping = false;
					}
				}

				if (isSleeping)
				{
					this.isSleeping = true;

					if (!isActive)
						EvaluateClassState();
				}
			}
		}

		private void EvaluateClassState()
		{
			try
			{
				if (State == LyricState.Active)
				{
					foreach (var syllable in Syllables)
					{
						if (syllable.Type == "Letters")
						{
							foreach (var letter in syllable.Letters)
							{
								GradientLabel letterLabel = letter.LiveText.Object as GradientLabel;
								_ = letterLabel.MyFadeTo(0.85f, 250, Easing.CubicInOut);
							}
						}

						if (syllable.LiveText.Object is GradientLabel label)
							_ = label.MyFadeTo(0.85f, 250, Easing.CubicInOut);
					}
				}

				if (State == LyricState.Sung)
				{
					foreach (var syllable in Syllables)
					{
						if (syllable.Type == "Letters")
						{
							foreach (var letter in syllable.Letters)
							{
								GradientLabel letterLabel = letter.LiveText.Object as GradientLabel;
								letterLabel.Progress = 0;
								_ = letterLabel.MyFadeTo(0.75f, 250, Easing.CubicInOut);

								// SetToGeneralState(false);
								UpdateLiveTextVisuals(letter.LiveText, true, 0, 0);
							}
						}

						if (syllable.LiveText.Object is GradientLabel label)
						{
							label.Progress = 0;
							_ = label.MyFadeTo(0.75f, 250, Easing.CubicInOut);

							// SetToGeneralState(false);
							UpdateLiveTextVisuals(syllable.LiveText, false, 0, 0);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MainThread.BeginInvokeOnMainThread(() => Toast.Make(ex.Message).Show());
			}
		}

		public bool IsActive() => Active;
		private bool IsEmphasized(SyllableMetadata metadata, bool isRomanized) => metadata.EndTime - metadata.StartTime >= 1 && (isRomanized ? metadata.RomanizedText.Length <= 12 : metadata.Text.Length <= 12);

		private Spline GetSpline(List<KeyValuePair<double, double>> range) => new Spline([.. range.Select(x => x.Key)], [.. range.Select(x => x.Value)]);

		private readonly List<KeyValuePair<double, double>> scaleRange =
		[
			new(0, 0.95d), // Lowest
			new(0.7d, 1.025d), // Highest
			new(1, 1) // Resting
		];

		private readonly List<KeyValuePair<double, double>> yOffsetRange =
		[
			new(0, 1d / 100d), // Lowest
			new(0.9d, -(1d / 60d)), // Highest
			new(1, 0) // Resting
		];

		private readonly List<KeyValuePair<double, double>> glowRange =
		[
			new(0, 0), // Lowest
			new(0.15d, 1), // Highest
			new(0.6d, 1), // Sustain
			new(1, 0) // Resting
		];
	}

	internal struct AnimatedSyllable
	{
		public double Start { get; set; }
		public double Duration { get; set; }

		public double StartScale { get; set; }
		public double DurationScale { get; set; }

		public LiveText LiveText { get; set; }

		public string Type { get; set; }
		public List<AnimatedLetter> Letters { get; set; }
	}

	internal struct AnimatedLetter
	{
		public double Start { get; set; }
		public double Duration { get; set; }
		public double GlowDuration { get; set; }

		public LiveText LiveText { get; set; }
	}
}
