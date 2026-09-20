#nullable enable
using Microsoft.Maui;
using UIKit;
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

    public UIFont DefaultFont => fontManager.DefaultFont;

    public UIFont GetFont(Font font, double defaultFontSize = 0) => fontManager.GetFont(XcavateFonts.WithExtraBoldWeight(font), defaultFontSize);
}
