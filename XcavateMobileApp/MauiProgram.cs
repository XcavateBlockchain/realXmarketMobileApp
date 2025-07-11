using CommunityToolkit.Maui;
using System.Globalization;
using PlutoFramework;
using System.Reflection;

using Microsoft.Extensions.Configuration;



#if ANDROID26_0_OR_GREATER
using PlutoFramework.Platforms.Android;
#endif

namespace XcavateMobileApp;

public static class MauiProgram
{

    public static MauiApp CreateMauiApp()
    {
        // Set InvariantCulture globally
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

#if ANDROID26_0_OR_GREATER
        AndroidNotificationHelper.AppIcon = CommunityToolkit.Maui.Core.Resource.Drawable.resourceappicon;
        AndroidNotificationHelper.MainActivityType = typeof(Platforms.Android.MainActivity);
#endif

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UsePlutoFramework()
            .AddAppSettings();

        var app = builder.Build();

        MauiAppBuilderExtensions.Services = app.Services;

        AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

        return app;
    }

    public static MauiAppBuilder AddAppSettings(this MauiAppBuilder builder)
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(XcavateMobileApp)}.appsettings.json");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        if (stream is null)
        {
            return builder;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.Configuration.AddConfiguration(configuration);

        return builder;
    }
}
