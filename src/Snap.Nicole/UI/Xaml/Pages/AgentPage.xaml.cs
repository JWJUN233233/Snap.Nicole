using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels.Agent;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class AgentPage : Page
{
    public AgentPage()
    {
        InitializeComponent();
        ViewModel = App.Host.Services.GetRequiredService<AgentViewModel>();
        DataContext = ViewModel;
    }

    internal AgentViewModel ViewModel { get; }
}
