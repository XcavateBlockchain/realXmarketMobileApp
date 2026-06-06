using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Plugin.Fingerprint;
using PlutoFramework.Model;
using Plutonication;

namespace XcavateMobileApp.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    EnableOnBackInvokedCallback = false,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionView },
    DataScheme = "plutonication",
    AutoVerify = true,
    Categories = new[] {
        Intent.CategoryDefault,
        Intent.CategoryBrowsable
})]
public class MainActivity : MauiAppCompatActivity
{
    private sealed class InsetsListener(Func<global::Android.Views.View?, WindowInsetsCompat?, WindowInsetsCompat?> applyInsets) : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(global::Android.Views.View? v, WindowInsetsCompat? insets) => applyInsets(v, insets);
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        // Android 15 (targetSdk 35) enforces edge-to-edge by default.
        // Ask the window/content view to fit system bars and cutouts.
        if (Window is not null)
        {
            Window.SetDecorFitsSystemWindows(true);
        }

        base.OnCreate(savedInstanceState);

        var rootContent = FindViewById<ViewGroup>(global::Android.Resource.Id.Content);
        if (rootContent is not null)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(rootContent, new InsetsListener((view, insets) =>
            {
                if (view is null || insets is null)
                {
                    return insets ?? WindowInsetsCompat.Consumed;
                }

                var systemInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
                view.SetPadding(systemInsets.Left, systemInsets.Top, systemInsets.Right, systemInsets.Bottom);
                return insets;
            }));

            ViewCompat.RequestApplyInsets(rootContent);
        }

        CrossFingerprint.SetCurrentActivityResolver(() => this);

        if (Intent.Data != null)
        {
            var uriString = Intent?.Data.ToString();

            if (uriString.Equals("plutonication:") || uriString.Equals("plutonication://"))
            {
                // Nothing
            }
            else if (uriString.StartsWith("plutonication"))
            {
                AccessCredentials ac = new AccessCredentials(new Uri(uriString));

                PlutonicationModel.ProcessAccessCredentials(ac);
            }
        }
    }
}
