using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Pages;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Windows;

internal sealed partial class MainWindow : Window
{
    private readonly INavigationService navigationService;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel;
        this.navigationService = navigationService;
        InitializeComponent();
        navigationService.Frame = ContentFrame;
        navigationService.NavigationView = Root;
    }

    internal MainViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        navigationService.NavigateTo(typeof(HomePage));
    }

    private void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            navigationService.NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer is NavigationViewItem item)
        {
            navigationService.NavigateTo(item);
        }
    }
}
