using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Nicole.Core.Hosting;
using System;
using System.Diagnostics;
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

    [RelayCommand]
    private static void RestartExplorer()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "taskkill",
            Arguments = "/f /im explorer.exe",
            CreateNoWindow = true,
        })?.WaitForExit();
        Process.Start("explorer.exe");
    }
}
