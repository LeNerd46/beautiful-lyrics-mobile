using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MauiIcons.FontAwesome;
using MauiIcons.Material.Rounded;
using MR.Gestures;
using BeautifulLyricsMobile.Controls;
using BeautifulLyricsMobile.Platforms.Android;
using Microsoft.Maui.Controls.Compatibility.Hosting;

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
				.UseFontAwesomeMauiIcons()
				.UseMaterialRoundedMauiIcons()
				.ConfigureMRGestures()
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
#endif
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
