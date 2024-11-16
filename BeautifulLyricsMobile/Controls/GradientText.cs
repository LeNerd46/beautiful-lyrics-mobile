using Microsoft.Maui.Handlers;
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
	}
}