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
        SentryDiagnostics.AddBreadcrumb("Main window loaded", "ui.window", "ui");

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
                ["item"] = item.Name ?? string.Empty,
            };
            SentryDiagnostics.AddBreadcrumb("Navigation item invoked", "ui.navigation", "ui", data);
            navigationService.NavigateTo(item);
        }
    }

    void IXamlWindowCloseHandler.OnWindowClosing(out bool cancel)
    {
        cancel = false;
    }

    void IXamlWindowCloseHandler.OnWindowClosed()
    {
        SentryDiagnostics.AddBreadcrumb("Main window closed", "ui.window", "ui");
        ContentFrame.Content = null;
        navigationService.Frame = null;
        navigationService.NavigationView = null;
    }
}
