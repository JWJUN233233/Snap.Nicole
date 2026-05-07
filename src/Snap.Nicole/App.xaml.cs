using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.UI.Shell;
using Snap.Nicole.UI.Xaml.Windows;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole;

public partial class App : Application
{
    public App()
    {
        SynchronizationContext = SynchronizationContext.Current!;
        TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
        InitializeComponent();
    }

    public static new App Current { get => (App)Application.Current; }

    public static bool IsHostInitialized { get; set; }

    [NotNull]
    public static IHost? Host
    {
        get
        {
            Debug.Assert(IsHostInitialized);
            return field!;
        }
        set;
    }

    public SynchronizationContext SynchronizationContext { get; }

    public TaskScheduler TaskScheduler { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!IsHostInitialized)
        {
            return;
        }

        _ = Host.StartAsync().ContinueWith(static task =>
        {
            IServiceProvider serviceProvider = Host.Services;
            serviceProvider.GetRequiredService<INotifyIcon>().Create();
            serviceProvider.GetRequiredService<IWindowLifeTime<MainWindow>>().Show();
        }, TaskScheduler);
    }
}
