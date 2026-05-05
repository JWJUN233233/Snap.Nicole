using Microsoft.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Navigation;

internal sealed class NavigationService : INavigationService
{
    public Frame? Frame { get; set; }

    public NavigationView? NavigationView { get; set; }

    public bool NavigateTo(Type pageType)
    {
        if (Frame?.Navigate(pageType) is not true)
        {
            return false;
        }

        UpdateSelectedItem(pageType);
        return true;
    }

    public bool NavigateTo(NavigationViewItem item)
    {
        if (NavigationExtensions.GetNavigateTo(item) is not Type pageType)
        {
            return false;
        }

        if (Frame?.Navigate(pageType) is not true)
        {
            return false;
        }

        NavigationView?.SelectedItem = item;
        return true;
    }

    private void UpdateSelectedItem(Type pageType)
    {
        if (NavigationView is not { } navigationView)
        {
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
}
