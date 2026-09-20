#nullable enable
using Android.Graphics;
using Microsoft.Maui;
using Font = Microsoft.Maui.Font;

namespace XcavateMobileApp;

public sealed class WeightAwareFontManager : IFontManager
{
    readonly FontManager fontManager;

    public WeightAwareFontManager(IFontRegistrar fontRegistrar, IServiceProvider? serviceProvider)
    {
        fontManager = new FontManager(fontRegistrar, serviceProvider);
    }

    public double DefaultFontSize => fontManager.DefaultFontSize;

    public Typeface DefaultTypeface => fontManager.DefaultTypeface;

    public Typeface? GetTypeface(Font font) => fontManager.GetTypeface(XcavateFonts.WithExtraBoldWeight(font));

    public FontSize GetFontSize(Font font, float defaultFontSize = 0f) => fontManager.GetFontSize(font, defaultFontSize);
}
