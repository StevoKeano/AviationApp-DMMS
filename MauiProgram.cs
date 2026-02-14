using AviationApp.Services;
using Microsoft.Extensions.Logging;

namespace AviationApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<IPlatformService, Platforms.Android.Services.PlatformService>();
#elif IOS
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<IPlatformService, Platforms.iOS.Services.PlatformService>();
#endif

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<OptionsPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
