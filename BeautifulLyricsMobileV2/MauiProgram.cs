using BeautifulLyricsMobileV2.PageModels;
using BeautifulLyricsMobileV2.Services;
using CommunityToolkit.Maui;
using MauiIcons.Material.Rounded;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using BeautifulLyricsMobileV2.Pages;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using BeautifulLyricsMobileV2.Controls;
using SkiaSharp.Views.Maui.Handlers;
using Syncfusion.Maui.Toolkit.Hosting;



#if ANDROID
using BeautifulLyricsMobileV2.Platforms.Android.PlatformServices;
#endif

namespace BeautifulLyricsMobileV2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCompatibility()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()
                .UseMaterialRoundedMauiIcons()
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android => android.OnCreate((activity, state) =>
                    {
                        // Make the status bar/notification bar transparent
                        activity.Window?.SetFlags(Android.Views.WindowManagerFlags.LayoutNoLimits, Android.Views.WindowManagerFlags.LayoutNoLimits);
                        activity.Window?.ClearFlags(Android.Views.WindowManagerFlags.TranslucentStatus);
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
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddCompatibilityRenderer(typeof(GradientLabel), typeof(GradientLabelRenderer));
                    handlers.AddHandler(typeof(TouchScrollView), typeof(TouchScrollViewHandler));
#endif
                    handlers.AddHandler(typeof(BackgroundAnimationView), typeof(SKCanvasViewHandler));
                });

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LyricsViewModel>();
#if ANDROID
            builder.Services.AddSingleton<ISpotifyRemoteService, AndroidSpotifyService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
