using CommunityToolkit.Maui;
using System.Globalization;
using PlutoFramework;
using PlutoFramework.Constants;
using PlutoFrameworkCore.Solana;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui;



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
            .UsePlutoFrameworkMinimal()
            .AddAppSettings();

        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("xcavatefont.ttf", "XcavateFont");
            fonts.AddFont("xcavatefontextrabold.ttf", "XcavateFontExtraBold");
        });

#if ANDROID || IOS
        builder.Services.Replace(ServiceDescriptor.Singleton<IFontManager>(
            serviceProvider => new WeightAwareFontManager(serviceProvider.GetRequiredService<IFontRegistrar>(), serviceProvider)));
#endif

        var app = builder.Build();

        MauiAppBuilderExtensions.Services = app.Services;

        // The Solana programs' Anchor IDLs ship as embedded resources (linked from
        // idls/{cluster}/ in the csproj) so transaction error pages can decode custom
        // program errors into the program authors' own words. A cluster without bundled
        // IDLs - mainnet until those programs deploy - registers nothing, and its
        // failures keep the node's raw reason.
        SolanaProgramErrorCatalogs.RegisterFromAssembly(
            Assembly.GetExecutingAssembly(), "XcavateMobileApp.Idls.Devnet.", SolanaCluster.Devnet);
        SolanaProgramErrorCatalogs.RegisterFromAssembly(
            Assembly.GetExecutingAssembly(), "XcavateMobileApp.Idls.Mainnet.", SolanaCluster.Mainnet);

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
        Endpoints.ConfigureEndpointUrls(key => configuration[key]);

        return builder;
    }
}
