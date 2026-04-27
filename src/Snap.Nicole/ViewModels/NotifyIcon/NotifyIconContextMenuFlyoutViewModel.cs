using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Nicole.Core.Hosting;
using System;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels.NotifyIcon;

internal sealed partial class NotifyIconContextMenuFlyoutViewModel(IServiceProvider serviceProvider)
{
    private readonly IApplicationLifeTime applicationLifeTime = serviceProvider.GetRequiredService<IApplicationLifeTime>();
    private readonly IWindowLifeTime<MainWindow> mainWindowLifeTime = serviceProvider.GetRequiredService<IWindowLifeTime<MainWindow>>();

    [RelayCommand]
    private void ShowMainWindow()
    {
        mainWindowLifeTime.Show();
    }

    [RelayCommand]
    private async Task ExitAsync()
    {
        if (applicationLifeTime.IsExiting)
        {
            return;
        }

        await applicationLifeTime.ShowdownAsync();
    }
}
