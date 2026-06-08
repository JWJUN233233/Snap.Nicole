using Microsoft.UI.Xaml;
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

    private void OnApiKeyPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        if (ViewModel.ModelConfiguration.Settings.ModelProviderProfiles.CurrentItem is not { } profile)
        {
            return;
        }

        string? currentValue = profile.ApiKey;
        string newValue = passwordBox.Password;

        if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        profile.ApiKey = newValue;
    }
}
