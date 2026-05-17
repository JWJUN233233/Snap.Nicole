using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels.Settings;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SettingsViewModel>();
    }
}
