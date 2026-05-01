using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.UI.Shell;
using Snap.Nicole.UI.Xaml.Windows;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Snap.Nicole;

public partial class App : Application
{
    public static IHost? Host { get; set; }

    [MemberNotNullWhen(true, nameof(Host))]
    public static bool IsHostInitialized { get; set; }

    public App()
    {
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (IsHostInitialized)
        {
            _ = Host.StartAsync().ContinueWith(static task =>
            {
                IServiceProvider serviceProvider = Host.Services;
                serviceProvider.GetRequiredService<INotifyIcon>().Create();
                serviceProvider.GetRequiredService<IWindowLifeTime<MainWindow>>().Show();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
