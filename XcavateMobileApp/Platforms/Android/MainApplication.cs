using Android.App;
using Android.Runtime;
using AndroidX.AppCompat.App;

namespace XcavateMobileApp.Platforms.Android;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		base.OnCreate();

		// Pins the whole process (including WebViews, which derive their color
		// scheme and force-dark behavior from the app's night mode) to light.
		AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
