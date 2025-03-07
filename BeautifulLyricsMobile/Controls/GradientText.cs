﻿using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Controls
{
	public class GradientLabel : Label, IElement
	{
		public static readonly BindableProperty StartColorProperty = BindableProperty.Create(nameof(StartColor), typeof(Color), typeof(Color), Colors.White);

		public Color StartColor
		{
			get => (Color)GetValue(StartColorProperty);
			set => SetValue(StartColorProperty, value);
		}

		public static readonly BindableProperty EndColorProperty = BindableProperty.Create(nameof(EndColor), typeof(Color), typeof(Color), new Color(224, 224, 224));

		public Color EndColor
		{
			get => (Color)GetValue(EndColorProperty);
			set => SetValue(EndColorProperty, value);
		}

		public static readonly BindableProperty ProgressProperty = BindableProperty.Create(nameof(GradientProgress), typeof(float), typeof(float), 0f);

		public float GradientProgress
		{
			get => (float)GetValue(ProgressProperty);
			set => SetValue(ProgressProperty, value);
		}

		public static readonly BindableProperty LabelOpacityProperty = BindableProperty.Create(nameof(LabelOpacityProperty), typeof(float), typeof(GradientLabel), 0.5f);

		public float LabelOpacity
		{
			get => (float)GetValue(LabelOpacityProperty);
			set => SetValue(LabelOpacityProperty, value);
		}

		public static readonly BindableProperty ShadowRadiusProperty = BindableProperty.Create(nameof(ShadowRadius), typeof(float), typeof(GradientLabel), 10f);

		public float ShadowRadius
		{
			get => (float)GetValue(ShadowRadiusProperty);
			set => SetValue(ShadowRadiusProperty, value);
		}

		public static readonly BindableProperty ShadowOpacityProperty = BindableProperty.Create(nameof(ShadowOpacity), typeof(float), typeof(GradientLabel), 0f);

		public float ShadowOpacity
		{
			get => (float)GetValue(ShadowOpacityProperty);
			set => SetValue(ShadowOpacityProperty, value);
		}

		public Task MyFadeTo(float target, uint duration = 250, Easing easing = null)
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();

			Animation animation = new Animation(v => LabelOpacity = (float)v, LabelOpacity, target);

			this.Animate("FadeToAnimation", animation, 16, duration, easing ?? Easing.Linear, (v, c) => taskCompletionSource.SetResult(true));

			return taskCompletionSource.Task;
		}
	}
}