using CommunityToolkit.Maui;
using System.Globalization;
using PlutoFramework;


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
            .UsePlutoFramework(
                appNamespace: "XcavateMobileApp"
            );

        var app = builder.Build();

        AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

        return app;
    }
}
