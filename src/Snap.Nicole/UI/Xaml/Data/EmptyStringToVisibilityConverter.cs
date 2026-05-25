using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml;

namespace Snap.Nicole.UI.Xaml.Data;

internal sealed class EmptyStringToVisibilityConverter : EmptyStringToObjectConverter
{
    private bool isInverted;

    public EmptyStringToVisibilityConverter()
    {
        UpdateValues();
    }

    public bool IsInverted
    {
        get => isInverted;
        set
        {
            isInverted = value;
            UpdateValues();
        }
    }

    private void UpdateValues()
    {
        NotEmptyValue = isInverted
            ? Visibility.Collapsed
            : Visibility.Visible;
        EmptyValue = isInverted
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
