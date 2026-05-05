using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels;
using System.Diagnostics;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Debug.Assert(App.IsHostInitialized);
        Root.DataContext = App.Host.Services.GetRequiredService<SettingsViewModel>();
    }
}
