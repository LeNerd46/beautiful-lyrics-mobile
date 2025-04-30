using BeautifulLyricsMobileV2.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	class LineVocals : ISyncedVocals
	{
		public event EventHandler<View> ActivityChanged;

		public FlexLayout Container { get; set; }
		public GradientLabel LyricText { get; set; }

		public double StartTime { get; set; }
		public double Duration { get; set; }

		private bool Active { get; set; }
		private LyricState State { get; set; }
		private bool isSleeping = true;
		public bool IsBackground { get; set; }

		private Spline glowSpline;
		private readonly Spring glowSpring;

		private readonly List<KeyValuePair<double, double>> glowRange =
		[
			new(0, 0),
			new(0.5, 1),
			new(0.925, 1),
			new(1, 0)
		];

		private Spline GetSpline(List<KeyValuePair<double, double>> range) => new Spline([.. range.Select(x => x.Key)], [.. range.Select(x => x.Value)]);

		public LineVocals(FlexLayout contianer, LineVocal vocal, bool isRomanized)
		{
			Container = contianer;

			StartTime = vocal.StartTime;
			Duration = vocal.EndTime - vocal.StartTime;
			State = LyricState.Idle;

			glowSpline = GetSpline(glowRange);
			glowSpring = new Spring(0, 0.5, 1.0);

			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.First();

			LyricText = new GradientLabel
			{
				Text = isRomanized ? vocal.RomanizedText : vocal.Text,
				Style = styles["LineLabel"] as Style,
				LineVocal = true
			};

			Container.Add(LyricText);

			SetToGeneralState(false);
		}

		private void SetToGeneralState(bool state)
		{
			int timeScale = state ? 1 : 0;

			UpdateLiveTextState(timeScale, true);
			UpdateLiveTextVisuals(timeScale, 0);

			State = state ? LyricState.Sung : LyricState.Idle;
			EvaluateClassState();
		}

		private void UpdateLiveTextState(double timeScale, bool forceTo = true)
		{
			double glowAlpha = glowSpline.At(timeScale);

			if (forceTo)
				glowSpring.Set(glowAlpha);
			else

				glowSpring.Final = glowAlpha;
		}

		private bool UpdateLiveTextVisuals(double timeScale, double deltaTime)
		{
			double glowAlpha = glowSpring.Update(deltaTime);

			LyricText.Dispatcher.Dispatch(() =>
			{
				LyricText.ShadowRadius = (float)(4 + (8 * glowAlpha));
				LyricText.ShadowOpacity = (float)(glowAlpha * 0.5);

				LyricText.Progress = (float)(120 * timeScale);
			});

			return glowSpring.Sleeping;
		}

		private void EvaluateClassState()
		{
			if (State == LyricState.Active)
				LyricText.FadeTo(0.75f, 250, Easing.CubicInOut);
			else if (State == LyricState.Sung)
			{
				LyricText.Progress = 0;
				LyricText.FadeTo(0.65f, 250, Easing.CubicInOut);

				UpdateLiveTextVisuals(0, 1.0 / 60);
			}
			else
			{
				LyricText.Progress = 0;
				LyricText.FadeTo(0.35f, 250, Easing.CubicInOut);

				UpdateLiveTextVisuals(0, 1.0 / 60);
			}
		}

		public void Animate(double songTimestamp, double deltaTime, bool isImmediate = true)
		{
			double relativeTime = songTimestamp - StartTime;
			double timeScale = Math.Max(0, Math.Min((double)relativeTime / (double)Duration, 1));

			bool pastStart = relativeTime >= 0;
			bool beforeEnd = relativeTime <= Duration;
			bool isActive = pastStart && beforeEnd;

			LyricState stateNow = isActive ? LyricState.Active : pastStart ? LyricState.Sung : LyricState.Idle;

			bool stateChanged = stateNow != State;
			bool shouldUpdateVisualState = stateChanged || isActive || isImmediate;

			if(stateChanged)
			{
				State = stateNow;
				EvaluateClassState();

				if (State == LyricState.Active)
					ActivityChanged?.Invoke(this, Container);
			}

			if(shouldUpdateVisualState)
			{
				isSleeping = false;

				UpdateLiveTextState(timeScale, (isImmediate || (relativeTime < 0) || false));
			}

			if(!isSleeping)
			{
				bool isSleeping = UpdateLiveTextVisuals(timeScale, deltaTime);

				if(isSleeping)
				{
					this.isSleeping = true;

					if (!isActive)
						EvaluateClassState();
				}
			}
		}

		public bool IsActive() => Active;
	}
}
