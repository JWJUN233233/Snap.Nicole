using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SettingsViewModel>();
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;
}
