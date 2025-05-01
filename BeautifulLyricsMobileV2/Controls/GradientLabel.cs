using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Controls
{
    public class GradientLabel : Label, IElement
    {
        public static readonly BindableProperty StartColorProperty = BindableProperty.Create(nameof(StartColor), typeof(Color), typeof(GradientLabel), Colors.White);

        public Color StartColor
		{
			get => (Color)GetValue(StartColorProperty);
			set => SetValue(StartColorProperty, value);
		}

		public static readonly BindableProperty EndColorProperty = BindableProperty.Create(nameof(EndColor), typeof(Color), typeof(GradientLabel), new Color(224, 224, 224));

		public Color EndColor
		{
			get => (Color)GetValue(EndColorProperty);
			set => SetValue(EndColorProperty, value);
		}

		public static readonly BindableProperty ProgressProperty = BindableProperty.Create(nameof(Progress), typeof(float), typeof(GradientLabel), 0.0f);

		public float Progress
		{
			get => (float)GetValue(ProgressProperty);
			set => SetValue(ProgressProperty, value);
		}

		public static readonly BindableProperty LabelOpacityProperty = BindableProperty.Create(nameof(LabelOpacity), typeof(float), typeof(GradientLabel), 0.35f);

		public float LabelOpacity
		{
			get => (float)GetValue(LabelOpacityProperty);
			set => SetValue(LabelOpacityProperty, value);
		}

		public static readonly BindableProperty ShadowRadiusProperty = BindableProperty.Create(nameof(ShadowRadius), typeof(float), typeof(GradientLabel), 10.0f);

		public float ShadowRadius
		{
			get => (float)GetValue(ShadowRadiusProperty);
			set => SetValue(ShadowRadiusProperty, value);
		}

		public static readonly BindableProperty ShadowOpacityProperty = BindableProperty.Create(nameof(ShadowOpacity), typeof(float), typeof(GradientLabel), 0.0f);

		public float ShadowOpacity
		{
			get => (float)GetValue(ShadowOpacityProperty);
			set => SetValue(ShadowOpacityProperty, value);
		}

		public static readonly BindableProperty LineVocalProperty = BindableProperty.Create(nameof(LineVocal), typeof(bool), typeof(GradientLabel), false);

		public bool LineVocal
		{
			get => (bool)GetValue(LineVocalProperty);
			set => SetValue(LineVocalProperty, value);
		}

		public Task FadeTo(float target, uint duration = 250, Easing easing = null)
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();

			Animation animation = new Animation(v => LabelOpacity = (float)v, LabelOpacity, target);
			this.Animate("MyFadeToAnimation", animation, 16, duration, easing ?? Easing.Linear, (v, c) => taskCompletionSource.SetResult(true));

			return taskCompletionSource.Task;
		}
	}
}
