using Android.Views;
using BeautifulLyricsMobileV2.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Platforms.Android.PlatformServices
{
    public class TouchScrollViewHandler : ScrollViewHandler
    {
		public TouchScrollViewHandler() { }

		protected override void ConnectHandler(MauiScrollView platformView)
		{
			base.ConnectHandler(platformView);

			platformView.Touch += OnTouch;
		}

		protected override void DisconnectHandler(MauiScrollView platformView)
		{
			base.DisconnectHandler(platformView);

			platformView.Touch -= OnTouch;
		}

		private void OnTouch(object? sender, global::Android.Views.View.TouchEventArgs e)
		{
			var scroll = VirtualView as TouchScrollView;

			switch (e.Event.Action)
			{
				case MotionEventActions.Down:
					scroll?.OnTouch();
					break;

				case MotionEventActions.Up:
					scroll?.OnRelease();
					break;
			}

			e.Handled = false;
		}
	}
}
