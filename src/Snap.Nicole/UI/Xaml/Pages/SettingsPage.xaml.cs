using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels.Settings;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Host.Services.GetRequiredService<SettingsViewModel>();
        DataContext = ViewModel;
    }

    internal SettingsViewModel ViewModel { get; }
}
