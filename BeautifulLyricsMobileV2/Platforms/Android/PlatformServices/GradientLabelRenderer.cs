using Android.Content;
using Android.Graphics;
using BeautifulLyricsMobileV2.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Platforms.Android.PlatformServices
{
	class GradientLabelRenderer : LabelRenderer
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

			var color = Colors.White.ToAndroid();
			color.A = (byte)(opacity * 255);

			Control.SetShadowLayer(radius, 0f, 0f, color);
		}

		private void SetColors()
		{
			GradientLabel label = Element as GradientLabel;
			Shader shader;

			var c1 = label.StartColor.WithAlpha(label.LabelOpacity).ToAndroid();
			var c2 = label.EndColor.WithAlpha(label.LabelOpacity).ToAndroid();

			float progressFraction = label.Progress * 0.01f;

			if (!label.LineVocal)
			{
				if (progressFraction <= 0f)
					shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, c2, c2, Shader.TileMode.Clamp);
				else if (progressFraction >= 1f)
					shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, c1, c1, Shader.TileMode.Clamp);
				else
					shader = new LinearGradient(0, 0, Control.MeasuredWidth, 0, [c1, c1, c2, c2], [0f, progressFraction, progressFraction, 1f], Shader.TileMode.Clamp);
			}
			else
			{
				if (progressFraction <= 0f)
					shader = new LinearGradient(0, 0, 0, Control.MeasuredHeight, c2, c2, Shader.TileMode.Clamp);
				else if (progressFraction >= 1f)
					shader = new LinearGradient(0, 0, 0, Control.MeasuredHeight, c1, c1, Shader.TileMode.Clamp);
				else
					shader = new LinearGradient(0, 0, 0, Control.MeasuredHeight, [c1, c1, c2, c2], [0f, progressFraction, progressFraction, 1f], Shader.TileMode.Clamp);
			}

			Control.Paint.SetShader(shader);
			Control.Invalidate();
		}
	}
}
