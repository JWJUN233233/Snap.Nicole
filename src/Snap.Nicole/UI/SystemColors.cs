using Windows.UI;

namespace Snap.Nicole.UI;

internal static class SystemColors
{
    public static Color BaseLowColor(bool isDarkMode)
    {
        return isDarkMode ? ColorHelper.ToColor(0x33FFFFFF) : ColorHelper.ToColor(0x33000000);
    }

    public static Color BaseMediumLowColor(bool isDarkMode)
    {
        return isDarkMode ? ColorHelper.ToColor(0x66FFFFFF) : ColorHelper.ToColor(0x66000000);
    }

    public static Color BaseHighColor(bool isDarkMode)
    {
        return isDarkMode ? ColorHelper.ToColor(0xFFFFFFFF) : ColorHelper.ToColor(0xFF000000);
    }
}
