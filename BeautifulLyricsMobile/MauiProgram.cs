using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MauiIcons.FontAwesome;
using MauiIcons.Material.Rounded;
using MR.Gestures;
using BeautifulLyricsMobile.Controls;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using SkiaSharp.Views.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using The49.Maui.BottomSheet;


#if ANDROID
using Android.Views;
using BeautifulLyricsMobile.Platforms.Android;
#endif

namespace BeautifulLyricsMobile
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseMauiCommunityToolkit()
				.UseMauiCommunityToolkitMediaElement()
				.UseFontAwesomeMauiIcons()
				.UseMaterialRoundedMauiIcons()
				.UseBottomSheet()
				.ConfigureMRGestures()
				.ConfigureLifecycleEvents(events =>
				{
#if ANDROID
					events.AddAndroid(android => android.OnCreate((activity, state) =>
					{
						activity.Window?.AddFlags(WindowManagerFlags.LayoutNoLimits);
						activity.Window?.ClearFlags(WindowManagerFlags.TranslucentStatus);
						activity.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);
					}));
#endif
				})
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
					fonts.AddFont("LyricsMedium.ttf", "LyricsMedium");
				})
				.UseMauiCompatibility()
				.ConfigureMauiHandlers(handlers =>
				{
#if ANDROID
					handlers.AddCompatibilityRenderer(typeof(GradientLabel), typeof(GradientLabelRenderer));
					handlers.AddHandler(typeof(TouchScrollView), typeof(TouchScrollViewHandler));
#endif
					handlers.AddHandler(typeof(BlobAnimationView), typeof(SKCanvasViewHandler));
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
