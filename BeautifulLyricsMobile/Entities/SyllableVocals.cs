﻿using BeautifulLyricsAndroid.Entities;
using BeautifulLyricsAndroid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NTextCat.Commons;
using BeautifulLyricsMobile;
using BeautifulLyricsMobile.Controls;
using BeautifulLyricsMobile.Models;
using Android.Widget;

namespace BeautifulLyricsAndroid.Entities
{
	internal class SyllableVocals : ISyncedVocals
	{
		public FlexLayout Container;

		public double StartTime { get; }
		public double Duration { get; }
		public List<AnimatedSyllable> Syllables { get; } = [];
		private bool Active { get; set; }
		public bool Sung { get; set; }

		private LyricState State = LyricState.Idle;
		private bool isSleeping = true;

		public event EventHandler<bool> ActivityChanged;
		public event EventHandler RequestedTimeSkip;

		public bool IsBackground { get; set; }
		public bool OppositeAligned { get; set; }

		// Represents one line
		public SyllableVocals(FlexLayout lineContainer, List<SyllableMetadata> syllables, bool isBackground, bool isRomanized, bool oppositeAligned)
		{
			// FlexLayout container = new FlexLayout();
			// container.Dispatcher.Dispatch(() => container.Style = Application.Current.Resources.MergedDictionaries.Last()["IdleLyric"] as Style);
			// Container = container;
			Container = lineContainer;
			List<View> views = [];

			Active = false;
			IsBackground = isBackground;
			OppositeAligned = oppositeAligned;

			StartTime = syllables[0].StartTime;
			Duration = syllables.Last().EndTime - StartTime;

			List<List<SyllableMetadata>> syllableGroups = [];
			List<SyllableMetadata> currentSyllableGroup = [];

			List<View> visualElements = [];
			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.Last();
			Style lyricLabel = styles["LyricLabel"] as Style;
			Style backgroundLyricLabel = styles["BackgroundLyricLabel"] as Style;
			Style lyricEmphasizedLabel = styles["LyricEmphasizedLabel"] as Style;
			Style backgroundLyricEmphasizedLabel = styles["BackgroundLyricEmphasizedLabel"] as Style;

			lineContainer.BatchBegin();

			// Go through and create our syllable groups
			foreach (var syllableMetadata in syllables)
			{
				currentSyllableGroup.Add(syllableMetadata);

				if (!syllableMetadata.IsPartOfWord)
				{
					syllableGroups.Add(currentSyllableGroup);
					currentSyllableGroup = [];
				}
			}

			if (currentSyllableGroup.Count > 0)
				syllableGroups.Add(currentSyllableGroup);

			// Go through our groups and start building our visuals
			foreach (var syllableGroup in syllableGroups)
			{
				FlexLayout parentElement = Container;
				// parentElement.Dispatcher.Dispatch(() => parentElement.Style = Application.Current.Resources.MergedDictionaries.Last()["IdleLyric"] as Style);
				int syllableCount = syllableGroup.Count;
				bool isInWordGroup = syllableCount > 1;

				/*if (isInWordGroup)
				{
					FlexLayout parent = new FlexLayout();
					parent.Dispatcher.Dispatch(() => parent.Style = Application.Current.Resources.MergedDictionaries.Last()["IdleWordGroup"] as Style);

					parentElement = parent;
					Container.Dispatcher.Dispatch(() => Container.Children.Add(parent));

					*//*for (int i = 0; i < syllableCount; i++)
					{
						syllableGroup[i].IsPartOfWord = true;

						syllableGroup[i].IsStartOfWord = i == 0;
						syllableGroup[i].IsEndOfWord = i == syllableCount - 1;
					}*//*
				}*/

				int index = 0;

				HorizontalStackLayout wordGroup = null;
				bool firstSyllable = true;

				foreach (var syllableMetadata in syllableGroup)
				{
					bool isEmphasized = IsEmphasized(syllableMetadata, isRomanized);
					syllableMetadata.IsEmphasized = isEmphasized;

					GradientLabel syllableLabel = new GradientLabel
					{
						MaxLines = 1
					};

					List<AnimatedLetter> letters = [];
					HorizontalStackLayout emphasisGroup = [];
					// FlexLayout emphasisSyllable = null;

					if (!syllableMetadata.IsPartOfWord)
					{
						if (!isEmphasized)
						{
							if (wordGroup != null)
							{
								syllableLabel.Style = IsBackground ? backgroundLyricLabel : lyricLabel;
								// syllableLabel.Style = Application.Current.Resources.MergedDictionaries.Last()[IsBackground ? "BackgroundSungLabel" : "SungLabel"] as Style;
								syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

								wordGroup.Add(syllableLabel);
								views.Add(wordGroup);
								// lineContainer.Dispatcher.DispatchAsync(async () => lineContainer.Children.Add(wordGroup)); // .Wait()
								// visualElements.Add(wordGroup);
								// lineContainer.Children.Add(wordGroup);

								wordGroup = null;
							}
							else
							{
								syllableLabel.Style = IsBackground ? backgroundLyricLabel : lyricLabel;
								// syllableLabel.Style = Application.Current.Resources.MergedDictionaries.Last()[IsBackground ? "BackgroundSungLabel" : "SungLabel"] as Style;
								syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

								// lineContainer.Dispatcher.Dispatch(() => lineContainer.Children.Add(syllableLabel));
								views.Add(syllableLabel);
								// lineContainer.Dispatcher.DispatchAsync(async () => lineContainer.Children.Add(syllableLabel)); // .Wait();
								// visualElements.Add(syllableLabel);
							}
						}
						else
						{
							// Determine whether or not our content is a set of letters or a single text
							List<string> letterTexts = [];
							emphasisGroup.Margin = new Thickness(0, 0, 2, 0);
							//emphasisGroup.Style = Application.Current.Resources.MergedDictionaries.Last()["EmphasisGroupYesMargin"] as Style;

							foreach (var letter in isRomanized ? syllableMetadata.RomanizedText.ToCharArray() : syllableMetadata.Text.ToCharArray())
							{
								letterTexts.Add(letter.ToString());
							}

							double relativeTimestep = 1d / letterTexts.Count;

							letters = [];
							double relativeTimestamp = 0;

							foreach (var letter in letterTexts)
							{
								GradientLabel letterLabel = new GradientLabel();
								//letterLabel.LabelOpacity = 0.75f;
								letterLabel.Style = IsBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel;
								// letterLabel.Style = Application.Current.Resources.MergedDictionaries.Last()["SungEmphasizedLabel"] as Style;

								letterLabel.Text = letter;
								emphasisGroup.Add(letterLabel);

								// emphasisSyllable.Add(letterLabel);

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
								// lineContainer.Dispatcher.DispatchAsync(async () => lineContainer.Children.Add(wordGroup)); // .Wait();
								// visualElements.Add(wordGroup);
							}
							else
								views.Add(emphasisGroup);
							// lineContainer.Dispatcher.DispatchAsync(async () => lineContainer.Add(emphasisGroup)); //.Wait();
							// visualElements.Add(emphasisGroup);
						}
					}
					else
					{
						if (!isEmphasized)
						{
							wordGroup ??= [];

							syllableLabel.Style = IsBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel;
							// syllableLabel.Style = Application.Current.Resources.MergedDictionaries.Last()["SungEmphasizedLabel"] as Style;
							syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

							wordGroup.Add(syllableLabel);
						}
						else
						{
							// Determine whether or not our content is a set of letters or a single text
							List<string> letterTexts = [];

							foreach (var letter in isRomanized ? syllableMetadata.RomanizedText.ToCharArray() : syllableMetadata.Text.ToCharArray())
							{
								letterTexts.Add(letter.ToString());
							}

							double relativeTimestep = (double)1 / (double)letterTexts.Count;

							letters = [];
							double relativeTimestamp = 0;

							foreach (var letter in letterTexts)
							{
								GradientLabel letterLabel = new GradientLabel();
								//letterLabel.LabelOpacity = 0.75f;
								letterLabel.Style = IsBackground ? backgroundLyricEmphasizedLabel : lyricEmphasizedLabel;
								// letterLabel.Style = Application.Current.Resources.MergedDictionaries.Last()["SungEmphasizedLabel"] as Style;
								
								letterLabel.Text = letter;
								emphasisGroup.Add(letterLabel);

								// emphasisSyllable.Add(letterLabel);

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

					/*lineContainer.Dispatcher.Dispatch(() =>
					{
						lineContainer.BatchBegin();

						try
						{
							// visualElements.ForEach(lineContainer.Add);

							foreach(var element in visualElements)
							{
								if (element is Label label)
									lineContainer.Add(label);
								else if (element is HorizontalStackLayout layout)
									lineContainer.Add(layout);
								else
									lineContainer.Add(element);
							}
						}
						finally
						{
							lineContainer.BatchCommit();
						}
					});*/

					// else
					// {
					// emphasisSyllable = new FlexLayout();
					// emphasisSyllable.Dispatcher.Dispatch(() => emphasisSyllable.Style = Application.Current.Resources.MergedDictionaries.Last()["IdleLyric"] as Style);

					// emphasisSyllable.Dispatcher.Dispatch(() => emphasisSyllable.Children.Add(syllableLabel));

					// syllableLabel.Dispatcher.Dispatch(() => syllableLabel.Style = Application.Current.Resources.MergedDictionaries.Last()["SungLabel"] as Style);
					// syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

					// lineContainer.Dispatcher.DispatchAsync(async () => lineContainer.Children.Add(syllableLabel)).Wait();
					// lineContainer.Add(syllableLabel);
					// }


					// else
					// syllableLabel.Text = isRomanized ? syllableMetadata.RomanizedText : syllableMetadata.Text;

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

					if (syllableMetadata.IsEmphasized)
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

					// parentElement.Dispatcher.Dispatch(() => parentElement.Add(isEmphasized ? emphasisSyllable : syllableLabel));
					index++;
				}
			}

			views.ForEach(Container.Add);

			lineContainer.BatchCommit();

			SetToGeneralState(false);
			// lineContainer.Dispatcher.Dispatch(() => lineContainer.Add(Container));
		}

		private Springs CreateSprings() => new Springs
		{
			// Scale = new Spring(0, 0.7, 0.6),
			Scale = new Spring(0, 0.6f, 0.7f),
			YOffset = new Spring(0, 0.4f, 1.25f),
			// YOffset = new Spring(0, 1.25, 0.7),
			Glow = new Spring(0, 0.5f, 1)
		};

		private bool IsEmphasized(SyllableMetadata metadata, bool isRomanized) => metadata.EndTime - metadata.StartTime >= 1 && (isRomanized ? metadata.RomanizedText.Length <= 12 : metadata.Text.Length <= 12);

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

				State = state ? LyricState.Sung : LyricState.Idle;
			}
		}

		private void UpdateLiveTextState(LiveText liveText, double timeScale, double glowTimeScale, bool forceTo = true)
		{
			Spline scaleSpline = GetSpline(scaleRange);
			Spline yOffsetSpline = GetSpline(yOffsetRange);
			Spline glowSpline = GetSpline(glowRange);

			double scale = scaleSpline.At(timeScale);
			double yOffset = yOffsetSpline.At(timeScale);
			double glowAlpha = glowSpline.At(glowTimeScale);

			if (forceTo)
			{
				liveText.Springs.Scale.Set(scale);
				liveText.Springs.YOffset.Set(yOffset);
				liveText.Springs.Glow.Set(glowAlpha);
			}
			else
			{
				liveText.Springs.Scale.Final = scale;
				liveText.Springs.YOffset.Final = yOffset;
				liveText.Springs.Glow.Final = glowAlpha;
			}
		}

		private Spline GetSpline(List<KeyValuePair<double, double>> range)
		{
			var times = range.Select(x => x.Key).ToList();
			var values = range.Select(x => x.Value).ToList();

			return new Spline(times, values);
		}

		private readonly List<KeyValuePair<double, double>> scaleRange =
		[
			new(0, 0.95d), // Lowest
			// new(0, 1.2),
			new(0.7d, 1.025d), // Highest
			// new(0.7, 1.15),
			new(1, 1), // Resting
		];

		private readonly List<KeyValuePair<double, double>> yOffsetRange =
		[
			new(0, 1d / 100d),
			// new(0, (double)4 / (double)5),
			// new(0.7, (double)1.5), // Lowest
			new(0.9f, -1d / 60d), 
			// new(0.7, -((double)1 / (double)5)), 
			// new(0, (double)-1), // Highest
			new (1, 0)
		];

		private readonly List<KeyValuePair<double, double>> glowRange =
		[
			new(0, 0),
			new(0.15d, 1),
			new(0.6d, 1),
			new(1, 0)
		];

		public void Animate(double songTimestamp, double deltaTime, bool isImmeiate = true) // Spelled wrong, but I don't care
		{
			double relativeTime = songTimestamp - StartTime;
			double timeScale = Math.Max(0, Math.Min((double)relativeTime / (double)Duration, 1));

			bool pastStart = relativeTime >= 0;
			bool beforeEnd = relativeTime <= Duration;
			bool isActive = pastStart && beforeEnd;
			Active = isActive;

			LyricState stateNow = isActive ? LyricState.Active : pastStart ? LyricState.Sung : LyricState.Idle;

			bool stateChanged = stateNow != State;
			bool shouldUpdateVisualState = stateChanged || isActive || isImmeiate;

			if (stateChanged)
			{
				LyricState oldState = State;
				State = stateNow;

				if (State != LyricState.Sung)
					EvaluateClassState();

				if (oldState == LyricState.Active)
					ActivityChanged?.Invoke(this, false);
				else if (isActive)
					ActivityChanged?.Invoke(this, true);
			}

			if (shouldUpdateVisualState)
				this.isSleeping = false;

			bool isMoving = this.isSleeping == false;

			if (shouldUpdateVisualState || isMoving)
			{
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
								UpdateLiveTextState(letter.LiveText, letterTimeScale, glowTimeScale, isImmeiate);

							if (isMoving)
							{
								bool letterIsSleeping = UpdateLiveTextVisuals(letter.LiveText, true, letterTimeScale, deltaTime);

								if (!letterIsSleeping)
									isSleeping = false;
							}
						}
					}

					if (shouldUpdateVisualState)
						UpdateLiveTextState(syllable.LiveText, syllableTimeScale, syllableTimeScale, isImmeiate);

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

		private bool UpdateLiveTextVisuals(LiveText liveText, bool isEmphasized, double timeScale, double deltaTime)
		{
			double scale = liveText.Springs.Scale.Update(deltaTime);
			double yOffset = liveText.Springs.YOffset.Update(deltaTime) * 50;
			double glowAlpha = liveText.Springs.Glow.Update(deltaTime);

			if (double.IsInfinity(scale)) return true;

			float gradientProgress = (int)Math.Round(-20 + 120 * timeScale);
			liveText.YOffset = yOffset * (isEmphasized ? 3 : 1);
			liveText.Scale = scale;

			if (liveText.Object is GradientLabel label)
			{
				label.Dispatcher.Dispatch(() =>
				{
					label.Scale = scale;
					label.TranslationY = yOffset * (isEmphasized ? 1.5 : 1);
					label.GradientProgress = gradientProgress;

					label.ShadowRadius = 4 + (2 * (float)glowAlpha * (isEmphasized ? 3 : 1));
					label.ShadowOpacity = (float)Math.Max(0, Math.Min(1, glowAlpha * (isEmphasized ? 1 : 0.5f)));
				});
			}

			return liveText.Springs.Scale.Sleeping && liveText.Springs.YOffset.Sleeping && liveText.Springs.Glow.Sleeping;
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
								letterLabel.MyFadeTo(1f, 2500, Easing.SpringOut);
								//letterLabel.LabelOpacity = 1f;
								// letterLabel.FadeTo(1, 250, Easing.SpringOut);
							}
						}

						if (syllable.LiveText.Object is GradientLabel label)
							label.MyFadeTo(1f, 2500, Easing.SpringOut);
						//label.LabelOpacity = 1f;
						// label.FadeTo(1, 250, Easing.SpringOut);
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
								letterLabel.GradientProgress = 0;
								letterLabel.MyFadeTo(0.75f, 2500, Easing.SpringOut);
								//letterLabel.LabelOpacity = 0.75f;
								//letterLabel.FadeTo(0.75d, 250, Easing.SpringOut);

								UpdateLiveTextVisuals(letter.LiveText, true, 0, 0);
							}
						}

						if (syllable.LiveText.Object is GradientLabel label)
						{
							label.GradientProgress = 0;
							label.MyFadeTo(0.75f, 2500, Easing.SpringOut);
							//label.LabelOpacity = 0.75f;
							//label.FadeTo(0.75d, 250, Easing.SpringOut);

							UpdateLiveTextVisuals(syllable.LiveText, false, 0, 0);
						}
					}
				}
			}
			catch(Exception ex)
			{
				MainThread.BeginInvokeOnMainThread(() => CommunityToolkit.Maui.Alerts.Toast.Make(ex.Message));
			}
		}

		public void SetBlur(double blurDistance)
		{
			throw new NotImplementedException();
		}

		public bool IsActive()
		{
			return Active;
		}

		/// <summary>
		/// EvaluateClassState();
		/// </summary>
		/// <returns></returns>
		public LyricState GetLyricState()
		{
			if (Active)
				return LyricState.Active;

			return LyricState.Sung;
		}
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