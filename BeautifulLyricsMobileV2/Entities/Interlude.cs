using BeautifulLyricsMobile.CurveInterpolator;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	struct MainAnimations
	{
		public double YOffsetDamping { get; set; }
		public double YOffsetFrequency { get; set; }
		public double ScaleDamping { get; set; }
		public double ScaleFrequency { get; set; }

		public List<KeyValuePair<double, double>> BaseScaleRange { get; set; }
		public List<KeyValuePair<double, double>> OpacityRange { get; set; }
		public List<KeyValuePair<double, double>> YOffsetRange { get; set; }
	}

	struct DotAnimations
	{
		public double YOffsetDamping { get; set; }
		public double YOffsetFrequency { get; set; }

		public double ScaleDamping { get; set; }
		public double ScaleFrequency { get; set; }

		public double GlowDamping { get; set; }
		public double GlowFrequency { get; set; }

		public List<KeyValuePair<double, double>> ScaleRange { get; set; }
		public List<KeyValuePair<double, double>> YOffsetRange { get; set; }
		public List<KeyValuePair<double, double>> GlowRange { get; set; }
		public List<KeyValuePair<double, double>> OpacityRange { get; set; }
	}

	struct DotSprings
	{
		public Spring Scale { get; set; }
		public Spring YOffset { get; set; }
		public Spring Glow { get; set; }
		public Spring Opacity { get; set; }
	}

	struct DotLiveText
	{
		public Ellipse Object { get; set; }
		public DotSprings Springs { get; set; }
	}

	struct MainSprings
	{
		public Spring Scale { get; set; }
		public Spring YOffset { get; set; }
		public Spring Opacity { get; set; }
	}

	struct MainLiveText
	{
		public HorizontalStackLayout Object { get; set; }
		public MainSprings Springs { get; set; }
	}

	struct AnimatedDot
	{
		public double Start { get; set; }
		public double Duration { get; set; }
		public double GlowDuration { get; set; }

		public DotLiveText LiveText { get; set; }
	}

	internal class InterludeVisual : ISyncedVocals
	{
		private HorizontalStackLayout Container { get; set; }

		private double StartTime { get; set; }
		public double Duration { get; set; }
		private List<AnimatedDot> Dots { get; set; } = [];
		private MainLiveText LiveText { get; set; }

		private LyricState State { get; set; }
		private bool IsSleeping { get; set; }

		private readonly CurveInterpolator ScaleSpline;
		private readonly CurveInterpolator OpacitySpline;

		public MainAnimations MainAnimations = new MainAnimations
		{
			YOffsetDamping = 0.4,
			YOffsetFrequency = 1.25,
			ScaleDamping = 0.7,
			ScaleFrequency = 5,

			BaseScaleRange =
			[
				new(0, 0),
				new(0.2, 1.05),
				new(-0.075, 1.15),
				new(-0, 0)
			],

			OpacityRange =
			[
				new(0, 0),
				new(0.5, 1),
				new(-0.075, 1),
				new(-0, 0)
			],

			YOffsetRange =
			[
				new(0, 1d / 100d),
				new(0.9, -(1d / 60d)),
				new(1, 0)
			]
		};

		public DotAnimations DotAnimations = new DotAnimations
		{
			YOffsetDamping = 0.4,
			YOffsetFrequency = 1.25,

			ScaleDamping = 0.6,
			ScaleFrequency = 0.7,

			GlowDamping = 0.5,
			GlowFrequency = 1,

			ScaleRange =
			[
				new(0, 0.75),
				new(0.7, 1.05),
				new(1, 1)
			],

			YOffsetRange =
			[
				new(0, 0.125),
				new(0.9, -0.2),
				new(1, 0)
			],

			GlowRange =
			[
				new(0, 0),
				new(0.6, 1),
				new(1, 1)
			],

			OpacityRange =
			[
				new(0, 0.35),
				new(0.6, 1),
				new(1, 1)
			]
		};

		public readonly double PulseInterval = 2.25;
		public readonly double DownPulse = 0.95;
		public readonly double UpPulse = 1.05;

		private CurveInterpolator MainYOffsetSpline;

		public event EventHandler<bool> ActivityChanged;
		public event EventHandler RequestedTimeSkip;

		private Spline scaleSpline;
		private Spline yOffsetSpline;
		private Spline glowSpline;
		private Spline opacitySpline;

		private MainSprings CreateMainSprings() => new MainSprings
		{
			Scale = new Spring(0, MainAnimations.ScaleDamping, MainAnimations.ScaleFrequency),
			YOffset = new Spring(0, MainAnimations.YOffsetDamping, MainAnimations.YOffsetFrequency),
			Opacity = new Spring(0, MainAnimations.YOffsetDamping, MainAnimations.YOffsetFrequency)
		};

		private DotSprings CreateDotSprings() => new DotSprings
		{
			Scale = new Spring(0, DotAnimations.ScaleDamping, DotAnimations.ScaleFrequency),
			YOffset = new Spring(0, DotAnimations.YOffsetDamping, DotAnimations.YOffsetFrequency),
			Glow = new Spring(0, DotAnimations.GlowDamping, DotAnimations.GlowFrequency),
			Opacity = new Spring(0, DotAnimations.GlowDamping, DotAnimations.GlowFrequency)
		};

		public InterludeVisual(FlexLayout lineContainer, Interlude interludeMetadata)
		{
			HorizontalStackLayout container = new HorizontalStackLayout
			{
				IsVisible = false
			};

			container.Dispatcher.Dispatch(() => container.Style = (Style)Application.Current.Resources["Interlude"]);
			Container = container;

			var points = MainAnimations.YOffsetRange.Select(metadata => new double[] { metadata.Key, metadata.Value }).ToArray();
			List<Vector> vectors = [];

			foreach (var point in points)
			{
				vectors.Add(new Vector
				{
					Numbers = [.. point]
				});
			}

			scaleSpline = GetSpline(DotAnimations.ScaleRange.Select(x => x.Key).ToList(), DotAnimations.ScaleRange.Select(x => x.Value).ToList());
			yOffsetSpline = GetSpline(DotAnimations.YOffsetRange.Select(x => x.Key).ToList(), DotAnimations.YOffsetRange.Select(x => x.Value).ToList());
			glowSpline = GetSpline(DotAnimations.GlowRange.Select(x => x.Key).ToList(), DotAnimations.GlowRange.Select(x => x.Value).ToList());
			opacitySpline = GetSpline(DotAnimations.OpacityRange.Select(x => x.Key).ToList(), DotAnimations.OpacityRange.Select(x => x.Value).ToList());

			MainYOffsetSpline = new CurveInterpolator([.. vectors]);

			LiveText = new MainLiveText
			{
				Object = container,
				Springs = CreateMainSprings()
			};

			StartTime = interludeMetadata.Time.StartTime;
			Duration = interludeMetadata.Time.EndTime - StartTime;

			// Create our splines
			var scaleRange = MainAnimations.BaseScaleRange.Select(point => new
			{
				Time = point.Key,
				point.Value
			}).ToList();

			var opacityRange = MainAnimations.OpacityRange.Select(point => new
			{
				Time = point.Key,
				point.Value
			}).ToList();

			// wtf is this code dude
			scaleRange[2] = new { Time = scaleRange[2].Time + Duration, scaleRange[2].Value };
			opacityRange[2] = new { Time = opacityRange[2].Time + Duration, opacityRange[2].Value };
			scaleRange[3] = new { Time = scaleRange[3].Time + Duration, scaleRange[3].Value };
			opacityRange[3] = new { Time = opacityRange[3].Time + Duration, opacityRange[3].Value };

			var startPoint = scaleRange[1];
			var endPoint = scaleRange[2];

			double deltaTime = endPoint.Time - startPoint.Time;

			for (double i = Math.Floor(deltaTime / PulseInterval); i > 0; i -= 1)
			{
				double time = startPoint.Time + (i * PulseInterval);
				double value = (i % 2 == 0) ? UpPulse : DownPulse;

				scaleRange.Insert(2, new { Time = time, Value = value });
			}

			scaleRange.ForEach(point => point = new { Time = point.Time / Duration, point.Value });
			opacityRange.ForEach(point => point = new { Time = point.Time / Duration, point.Value });

			var scalePoints = scaleRange.Select(metadata => new double[] { metadata.Time, metadata.Value }).ToArray();
			List<Vector> scaleVectors = [];

			foreach (var point in scalePoints)
			{
				scaleVectors.Add(new Vector
				{
					Numbers = [.. point]
				});
			}

			ScaleSpline = new CurveInterpolator([.. scaleVectors]);

			var opacityPoints = opacityRange.Select(metadata => new double[] { metadata.Time, metadata.Value }).ToArray();
			List<Vector> opacityVectors = [];

			foreach (var point in opacityPoints)
			{
				opacityVectors.Add(new Vector
				{
					Numbers = [.. point]
				});
			}

			OpacitySpline = new CurveInterpolator([.. opacityVectors]);

			// Go through and create all our dots
			double dotStep = 0.92d / 3d;
			double startTime = 0;

			ResourceDictionary styles = Application.Current.Resources.MergedDictionaries.First();

			try
			{
				for (int i = 0; i < 3; i++)
				{
					Ellipse dot = new Ellipse();
					dot.Dispatcher.Dispatch(() => dot.Style = styles["InterludeDot"] as Style);

					Dots.Add(new AnimatedDot
					{
						Start = startTime,
						Duration = dotStep,
						GlowDuration = (1 - startTime),

						LiveText = new DotLiveText
						{
							Object = dot,
							Springs = CreateDotSprings()
						}
					});

					Container.Children.Add(dot);
					startTime += dotStep;
				}

				SetToGeneralState(false);
				lineContainer.Add(container);
			}
			catch (Exception)
			{
				// Stupid
			}

		}

		private Spline GetSpline(List<double> times, List<double> values) => new Spline(times, values);

		private void SetToGeneralState(bool state)
		{
			double timeScale = state ? 1 : 0;

			foreach (var dot in Dots)
			{
				UpdateLiveDotState(dot.LiveText, timeScale, timeScale, true);
				UpdateLiveDotVisuals(dot.LiveText, 0);
			}

			UpdateLiveMainState(LiveText, timeScale, true);
			UpdateLiveMainVisuals(LiveText, 0);

			State = state ? LyricState.Sung : LyricState.Sung;
		}

		private void UpdateLiveDotState(DotLiveText liveText, double timeScale, double glowTimeScale, bool forceTo)
		{
			double scale = scaleSpline.At(timeScale);
			double yOffset = yOffsetSpline.At(timeScale);
			double glowAlpha = glowSpline.At(glowTimeScale);
			double opacity = opacitySpline.At(timeScale);

			if (forceTo)
			{
				LiveText.Springs.Scale.Set(scale);
				liveText.Springs.YOffset.Set(yOffset);
				liveText.Springs.Glow.Set(glowAlpha);
				liveText.Springs.Opacity.Set(opacity);
			}
			else
			{
				liveText.Springs.Scale.Final = scale;
				liveText.Springs.YOffset.Final = yOffset;
				liveText.Springs.Glow.Final = glowAlpha;
				liveText.Springs.Opacity.Final = opacity;
			}
		}

		private bool UpdateLiveDotVisuals(DotLiveText liveText, double deltaTime)
		{
			double scale = liveText.Springs.Scale.Update(deltaTime);
			double yOffset = liveText.Springs.YOffset.Update(deltaTime) * 12;
			double glowAlpha = liveText.Springs.Glow.Update(deltaTime);
			double opacity = liveText.Springs.Opacity.Update(deltaTime);

			if (yOffset > 10)
				return liveText.Springs.Scale.Sleeping && liveText.Springs.YOffset.Sleeping && liveText.Springs.Glow.Sleeping && liveText.Springs.Opacity.Sleeping;

			// Update visuals
			liveText.Object.Dispatcher.Dispatch(() =>
			{
				if (double.IsNaN(scale) || double.IsInfinity(scale)) return;

				liveText.Object.Scale = scale;
				liveText.Object.TranslationY = yOffset;
				// Glow
				liveText.Object.Opacity = opacity;

				// System.Diagnostics.Debug.WriteLine($"Scale: {scale}");
			});

			return liveText.Springs.Scale.Sleeping && liveText.Springs.YOffset.Sleeping && liveText.Springs.Glow.Sleeping && liveText.Springs.Opacity.Sleeping;
		}

		private void UpdateLiveMainState(MainLiveText liveText, double timeScale, bool forceTo = true)
		{
			// Grab easy values
			var yOffset = MainYOffsetSpline.GetPointAt(timeScale).Numbers[1];

			// Find our scale/opacity points
			var scaleIntersections = ScaleSpline.GetIntersects(timeScale);
			var opacityIntersections = OpacitySpline.GetIntersects(timeScale);

			var scale = (scaleIntersections.Length == 0) ? 1 : scaleIntersections[^1].Numbers[1];
			var opacity = (opacityIntersections.Length == 0) ? 1 : opacityIntersections[^1].Numbers[1];

			// Apply them
			if (forceTo)
			{
				liveText.Springs.Scale.Set(scale);
				liveText.Springs.YOffset.Set(yOffset);
				liveText.Springs.Opacity.Set(opacity);
			}
			else
			{
				liveText.Springs.Scale.Final = scale;
				liveText.Springs.YOffset.Final = yOffset;
				liveText.Springs.Opacity.Final = opacity;
			}
		}

		private bool UpdateLiveMainVisuals(MainLiveText liveText, double deltaTime)
		{
			double scale = LiveText.Springs.Scale.Update(deltaTime);
			double yOffset = LiveText.Springs.YOffset.Update(deltaTime) * 25;
			double opacity = LiveText.Springs.Opacity.Update(deltaTime);


			liveText.Object.Dispatcher.Dispatch(() =>
			{
				if (double.IsNaN(scale) || double.IsInfinity(scale)) return;

				liveText.Object.Scale = scale;
				liveText.Object.TranslationY = yOffset;
				liveText.Object.Opacity = opacity;
			});

			return liveText.Springs.Scale.Sleeping && liveText.Springs.YOffset.Sleeping && liveText.Springs.Opacity.Sleeping;
		}

		public void Animate(double songTimestamp, double deltaTime, bool isImmeiate = true)
		{
			Task.Run(() =>
			{
				double relativeTime = songTimestamp - StartTime;
				double timeScale = Math.Max(0, Math.Min((double)relativeTime / (double)Duration, 1));

				bool pastStart = (relativeTime >= 0);
				bool beforeEnd = (relativeTime <= Duration);
				bool isActive = pastStart && beforeEnd;

				LyricState stateNow = isActive ? LyricState.Active : pastStart ? LyricState.Sung : LyricState.Idle;

				bool stateChanged = stateNow != State;
				bool shouldUpdateVisualState = stateChanged || isActive || isImmeiate;

				if (stateChanged)
				{
					LyricState oldState = State;
					State = stateNow;

					if (State == LyricState.Active)
						Container.Dispatcher.Dispatch(() => Container.IsVisible = true);
					else if (State == LyricState.Sung)
					{
						Container.Dispatcher.Dispatch(async () =>
						{
							await Container.ScaleTo(0, 350, Easing.SpringOut);
							Container.IsVisible = false;
						});
					}
				}

				if (shouldUpdateVisualState)
					IsSleeping = false;

				bool isMoving = !IsSleeping; // What an odd way to do this
				if (shouldUpdateVisualState || isMoving)
				{
					bool isSleeping = true;

					foreach (var dot in Dots)
					{
						double dotTimeScale = Math.Max(0, Math.Min((double)(timeScale - dot.Start) / (double)dot.Duration, 1));

						if (shouldUpdateVisualState)
							UpdateLiveDotState(dot.LiveText, dotTimeScale, dotTimeScale, isImmeiate);

						if (isMoving)
						{
							bool dotIsSleeping = UpdateLiveDotVisuals(dot.LiveText, deltaTime);

							if (!dotIsSleeping)
								isSleeping = false;
						}
					}

					if (shouldUpdateVisualState)
						UpdateLiveMainState(LiveText, timeScale, isImmeiate);

					if (isMoving)
					{
						bool mainIsSleeping = UpdateLiveMainVisuals(LiveText, deltaTime);

						if (!mainIsSleeping)
							isSleeping = false;
					}

					if (isSleeping)
						IsSleeping = true;
				}
			}).ContinueWith(t =>
			{
				//if (t.IsFaulted)
				//	MainThread.BeginInvokeOnMainThread(() => Toast.Make(t.Exception?.Message).Show());
			});
		}

		public void ForceState(bool state)
		{
			SetToGeneralState(state);
		}

		public bool IsActive() => State == LyricState.Active;
	}
}
