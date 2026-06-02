using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Resources;
using Snap.Nicole.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Pages;
using Snap.Nicole.ViewModels;
using System.Collections.Generic;

namespace Snap.Nicole.UI.Xaml.Windows;

internal sealed partial class MainWindow : Window, IXamlWindowExtendsContentIntoTitleBar, IXamlWindowCloseHandler
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
        SentryDiagnostics.AddBreadcrumb("Main window loaded", SentryBreadcrumbCategories.UIWindow, SentryBreadcrumbTypes.UI);

        NavigationViewItem settingsItem = (NavigationViewItem)NavView.SettingsItem!;
        NavigationExtensions.SetNavigateTo(settingsItem, typeof(SettingsPage));
        settingsItem.SetBinding(ContentControl.ContentProperty, StringResourceProxy.Default.CreateBinding(nameof(SRName.UIXamlWindowsMainWindowNavigationViewItemSettingsContent)));
        navigationService.NavigateTo(typeof(HomePage));
    }

    private void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            Dictionary<string, string> data = new()
            {
                [SentryData.Item] = item.Name ?? string.Empty,
            };
            SentryDiagnostics.AddBreadcrumb("Navigation item invoked", SentryBreadcrumbCategories.UINavigation, SentryBreadcrumbTypes.UI, data);
            navigationService.NavigateTo(item);
        }
    }

    void IXamlWindowCloseHandler.OnWindowClosing(out bool cancel)
    {
        cancel = false;
    }

    void IXamlWindowCloseHandler.OnWindowClosed()
    {
        SentryDiagnostics.AddBreadcrumb("Main window closed", SentryBreadcrumbCategories.UIWindow, SentryBreadcrumbTypes.UI);
        ContentFrame.Content = null;
        navigationService.Frame = null;
        navigationService.NavigationView = null;
    }
}
