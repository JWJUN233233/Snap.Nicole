using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Snap.Nicole.ViewModels;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class AgentPage : Page
{
    private bool disposed;

    public AgentPage()
    {
        InitializeComponent();
        ViewModel = App.Host.Services.GetRequiredService<AgentViewModel>();
        DataContext = ViewModel;

        Unloaded += OnUnloaded;
    }

    internal AgentViewModel ViewModel { get; }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Cleanup();
        base.OnNavigatedFrom(e);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        Unloaded -= OnUnloaded;

        DataContext = null;
        ViewModel.Dispose();
    }
}
