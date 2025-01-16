using Android.Content;
using Android.Graphics;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using BeautifulLyricsMobile.Controls;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Platforms.Android
{
	public class GradientLabelRenderer : LabelRenderer
	{
		public GradientLabelRenderer(Context context) : base(context) { }

		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);
			SetColors();
			UpdateShadow();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			SetColors();
			UpdateShadow();
		}

		private void UpdateShadow()
		{
			GradientLabel label = Element as GradientLabel;

			if (Control == null)
				return;

			float radius = label.ShadowRadius;
			float opacity = label.ShadowOpacity;

			if (radius <= 0 || opacity <= 0)
				return;

			// if (opacity <= label.PreviousOpacity || radius <= label.PreviousRadius)
			// 	return;
			// 
			// label.PreviousOpacity = opacity;
			// label.PreviousRadius = radius;

			var color = Colors.White.ToAndroid();
			color.A = (byte)(opacity * 255);

			Control.SetShadowLayer(radius, 0f, 0f, color);
		}

		private void SetColors()
		{
			GradientLabel label = Element as GradientLabel;

			var c1 = label.StartColor.WithAlpha(label.LabelOpacity).ToAndroid();
			var c2 = label.EndColor.WithAlpha(label.LabelOpacity).ToAndroid();

			// var progress = Control.MeasuredWidth * (label.GradientProgress * 0.01f);
			float progressFraction = label.GradientProgress * 0.01f;

			// Shader shader = new LinearGradient(progress, 0, Control.MeasuredWidth, 0, c1, c2, Shader.TileMode.Clamp);

			Shader shader;

			if (progressFraction <= 0f)
				shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, c2, c2, Shader.TileMode.Clamp);
			else if (progressFraction >= 1f)
				shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, c1, c1, Shader.TileMode.Clamp);
			else
				shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, new int[] { c1, c1, c2, c2 }, [0f, progressFraction, progressFraction, 1f], Shader.TileMode.Clamp);

			Control.Paint.SetShader(shader);
			Control.Invalidate();
		}
	}
}
