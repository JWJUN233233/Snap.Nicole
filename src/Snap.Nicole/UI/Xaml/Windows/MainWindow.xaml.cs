using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Snap.Nicole.Resources;
using Snap.Nicole.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Pages;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Windows;

internal sealed partial class MainWindow : Window, IXamlWindowExtendsContentIntoTitleBar
{
    private readonly INavigationService navigationService;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel;
        this.navigationService = navigationService;
        InitializeComponent();
        navigationService.Frame = ContentFrame;
        navigationService.NavigationView = NavView;
    }

    public TitleBar TitleBar { get => TitleBarElement; }

    internal MainViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigationViewItem settingsItem = (NavigationViewItem)NavView.SettingsItem!;
        NavigationExtensions.SetNavigateTo(settingsItem, typeof(SettingsPage));
        settingsItem.SetBinding(ContentControl.ContentProperty, new Binding
        {
            Source = StringResourceProxy.Default,
            Path = new PropertyPath($"[{nameof(SRName.UIXamlWindowsMainWindowNavigationViewItemSettingsContent)}]"),
            Mode = BindingMode.OneWay,
        });

        navigationService.NavigateTo(typeof(HomePage));
    }

    private void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            navigationService.NavigateTo(item);
        }
    }
}
