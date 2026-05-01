using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        Root.DataContext = App.Host.Services.GetRequiredService<HomeViewModel>();
    }
}
