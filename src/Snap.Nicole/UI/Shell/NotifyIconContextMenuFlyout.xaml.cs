using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.ViewModels.NotifyIcon;

namespace Snap.Nicole.UI.Shell;

internal sealed partial class NotifyIconContextMenuFlyout : Flyout
{
    public NotifyIconContextMenuFlyout(NotifyIconContextMenuFlyoutViewModel viewModel)
    {
        InitializeComponent();
        Root.DataContext = viewModel;
    }
}
