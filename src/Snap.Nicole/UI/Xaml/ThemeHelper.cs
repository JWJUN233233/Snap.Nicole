using Microsoft.UI.Xaml;

namespace Snap.Nicole.UI.Xaml;

internal static class ThemeHelper
{
    public static bool IsDark(ApplicationTheme applicationTheme)
    {
        return applicationTheme is ApplicationTheme.Dark;
    }

    public static bool IsDark(ElementTheme elementTheme)
    {
        ApplicationTheme appTheme = Application.Current.RequestedTheme;
        return IsDark(elementTheme, appTheme);
    }

    public static bool IsDark(ElementTheme elementTheme, ApplicationTheme applicationTheme)
    {
        return elementTheme switch
        {
            ElementTheme.Default => IsDark(applicationTheme),
            ElementTheme.Dark => true,
            _ => false,
        };
    }
}
