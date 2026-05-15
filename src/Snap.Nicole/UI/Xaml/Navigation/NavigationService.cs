using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Navigation;

internal sealed class NavigationService(IServiceProvider serviceProvider) : INavigationService,
    IRecipient<NavigationGoBackMessage>,
    IRecipient<NavigationPaneToggleMessage>
{
    private readonly IMessenger messenger = serviceProvider.GetRequiredService<IMessenger>();

    public Frame? Frame
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field?.Navigated -= OnFrameNavigated;
            field = value;
            field?.Navigated += OnFrameNavigated;
        }
    }

    public NavigationView? NavigationView
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is not null)
            {
                field.Unloaded -= OnNavigationViewUnloaded;
                messenger.UnregisterAll(this);
            }

            field = value;

            if (field is not null)
            {
                field.Unloaded += OnNavigationViewUnloaded;
                messenger.RegisterAll(this);
            }
        }
    }

    public void Receive(NavigationGoBackMessage message)
    {
        if (Frame?.CanGoBack is true)
        {
            Frame.GoBack();
        }
    }

    public void Receive(NavigationPaneToggleMessage message)
    {
        NavigationView?.IsPaneOpen = !NavigationView.IsPaneOpen;
    }

    public bool NavigateTo(Type pageType)
    {
        return Frame?.Navigate(pageType) is true;
    }

    public bool NavigateTo(NavigationViewItem item)
    {
        if (NavigationExtensions.GetNavigateTo(item) is not Type pageType)
        {
            return false;
        }

        return Frame?.Navigate(pageType) is true;
    }

    private void UpdateSelectedItem(Type pageType)
    {
        if (NavigationView is not { } navigationView)
        {
            return;
        }

        if (navigationView.SettingsItem is NavigationViewItem settingsItem &&
            NavigationExtensions.GetNavigateTo(settingsItem) == pageType)
        {
            navigationView.SelectedItem = settingsItem;
            return;
        }

        foreach (object menuItem in navigationView.MenuItems)
        {
            if (menuItem is NavigationViewItem item &&
                NavigationExtensions.GetNavigateTo(item) == pageType)
            {
                navigationView.SelectedItem = item;
                return;
            }
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        UpdateSelectedItem(e.SourcePageType);
    }

    private void OnNavigationViewUnloaded(object sender, RoutedEventArgs e)
    {
        NavigationView = null;
    }
}
