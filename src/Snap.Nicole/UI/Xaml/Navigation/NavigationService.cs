using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Sentry;
using Snap.Nicole.Core;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

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
        SentryDiagnostics.AddBreadcrumb("Navigate back", "ui.navigation", "navigation");

        if (Frame?.CanGoBack is true)
        {
            Frame.GoBack();
        }
    }

    public void Receive(NavigationPaneToggleMessage message)
    {
        SentryDiagnostics.AddBreadcrumb("Toggle navigation pane", "ui.navigation", "ui", new Dictionary<string, string>
        {
            ["is_open"] = (NavigationView?.IsPaneOpen is true ? "false" : "true"),
        });

        NavigationView?.IsPaneOpen = !NavigationView.IsPaneOpen;
    }

    public bool NavigateTo(Type pageType)
    {
        string pageName = TypeNameHelper.GetTypeDisplayName(pageType, fullName: false);
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.navigation.navigate", pageName);
        span.SetTag("ui.page", pageName);

        bool succeeded = Frame?.Navigate(pageType) is true;
        span.SetTag("ui.navigation.succeeded", succeeded);
        span.Finish(succeeded ? SpanStatus.Ok : SpanStatus.FailedPrecondition);

        return succeeded;
    }

    public bool NavigateTo(NavigationViewItem item)
    {
        if (NavigationExtensions.GetNavigateTo(item) is not Type pageType)
        {
            SentryDiagnostics.AddBreadcrumb("Navigation target missing", "ui.navigation", "navigation", new Dictionary<string, string>
            {
                ["item"] = item.Name ?? string.Empty,
            }, BreadcrumbLevel.Warning);
            return false;
        }

        return NavigateTo(pageType);
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
        SentryDiagnostics.AddBreadcrumb("Navigated", "ui.navigation", "navigation", new Dictionary<string, string>
        {
            ["page"] = TypeNameHelper.GetTypeDisplayName(e.SourcePageType, fullName: false),
        });

        UpdateSelectedItem(e.SourcePageType);
    }

    private void OnNavigationViewUnloaded(object sender, RoutedEventArgs e)
    {
        NavigationView = null;
    }
}
