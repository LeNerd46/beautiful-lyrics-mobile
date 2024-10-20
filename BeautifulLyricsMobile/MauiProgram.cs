using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MauiIcons.FontAwesome;
using MauiIcons.Material.Rounded;
using BeautifulLyricsMobile.Platforms.Android;
using MR.Gestures;

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
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
