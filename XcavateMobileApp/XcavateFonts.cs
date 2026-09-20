using Microsoft.Maui;
using Font = Microsoft.Maui.Font;

namespace XcavateMobileApp;

public static class XcavateFonts
{
    public const string Regular = "XcavateFont";
    public const string ExtraBold = "XcavateFontExtraBold";

    // XcavateFont is the DM Sans variable font registered as a single alias, so
    // FontAttributes.Bold never reaches a bolder face: iOS ignores the weight of
    // custom fonts completely and Android only steps up to wght 700. Route bold
    // requests to the static ExtraBold (wght 800) cut instead.
    public static Font WithExtraBoldWeight(Font font)
    {
        var weight = font.Weight == default ? FontWeight.Regular : font.Weight;

        if (weight >= FontWeight.Bold && string.Equals(font.Family, Regular, StringComparison.OrdinalIgnoreCase))
        {
            return Font.OfSize(ExtraBold, font.Size, FontWeight.Regular, font.Slant, font.AutoScalingEnabled);
        }

        return font;
    }
}
