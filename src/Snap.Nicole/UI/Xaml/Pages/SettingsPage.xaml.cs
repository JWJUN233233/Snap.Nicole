using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SettingsViewModel>();
        OpenAIApiKeyBox.Password = ViewModel.OpenAIApiKey;
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    private void OnOpenAIApiKeyPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenAIApiKey = OpenAIApiKeyBox.Password;
    }
}
