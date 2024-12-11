using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Entities
{
	public class LyricsScroller(ScrollView _scrollView, VerticalStackLayout _lyircsContainer)
	{
		private ScrollView scrollView = _scrollView;
		private VerticalStackLayout lyricsContainer = _lyircsContainer;
		private const double Damping = 0.5;
		private const double Tension = 0.5;

		public async Task ScrollToLineAsync(int lineIndex)
		{
			var taretChild = lyricsContainer.Children[lineIndex];
			var targetY = GetChildOffsetY(taretChild as View);

			await AnimateSpringScroll(scrollView.ScrollY, targetY);
		}

		private double GetChildOffsetY(View child) => child.Y;

		private Task AnimateSpringScroll(double startY, double endY)
		{
			var tcs = new TaskCompletionSource<bool>();
			double currentY = startY;
			double velocity = 0;
			const double frameRate = (double)1 / 60.0;

			Device.StartTimer(TimeSpan.FromSeconds(frameRate), () =>
			{
				var force = (endY - currentY) * Tension;
				velocity += force - (velocity * Damping);
				currentY += velocity;

				scrollView.ScrollToAsync(0, currentY, false);

				if(Math.Abs(currentY - endY) < 0.1 && Math.Abs(velocity) < 0.1)
				{
					tcs.SetResult(true);
					return false;
				}

				return true;
			});

			return tcs.Task;
		}

		public async Task ScrollAsync()
		{
			uint animationDuration = 500;
			uint delayBetweenLines = 50;

			for (int i = 0; i < lyricsContainer.Children.Count; i++)
			{
				var line = lyricsContainer.Children[i] as View;

				line.Animate("MoveUp", new Animation(v =>
				{
					line.TranslationY = -v;
				}, 0, line.Height), length: animationDuration, easing: Easing.SpringIn);

				await Task.Delay((int)delayBetweenLines);
			}
		}

		// public async Task ScrollAsync(int lineIndex, double delay = 50)
		// {
		// 	for (int i = 0; i < lineIndex; i++)
		// 	{
		// 		await ScrollToLineAsync(i);
		// 		await Task.Delay(TimeSpan.FromMilliseconds(delay));
		// 	}
		// }
	}
}
