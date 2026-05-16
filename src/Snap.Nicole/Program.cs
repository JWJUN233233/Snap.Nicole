global using Microsoft.Extensions.DependencyInjection;
global using System;

using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.DependencyInjection;
using Snap.Nicole.Services.Settings;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.UI.Shell;
using Snap.Nicole.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Windows;
using Snap.Nicole.Services.AI;
using Snap.Nicole.ViewModels;
using Snap.Nicole.ViewModels.NotifyIcon;
using System.Runtime.CompilerServices;
using System.Threading;
using WinRT;
using CommunityToolkit.Mvvm.Messaging;

[assembly: DisableRuntimeMarshalling]

namespace Snap.Nicole;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        AppContext.SetSwitch("System.Net.Http.EnableActivityPropagation", true);

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.ConfigureServices(static (context, services) =>
        {
            services
                .AddJsonSettings<AppSettings>("AppSettings")
                .AddXamlApplication<App>()
                .AddXamlWindows(static builder =>
                {
                    builder
                        .AddXamlWindow<MainWindow>()
                        .AddXamlWindow<NotifyIconXamlHostWindow>();
                })
                .AddSingleton<IMessenger, WeakReferenceMessenger>()
                .AddSingleton<INavigationService, NavigationService>()
                .AddSingleton<INotifyIcon, NotifyIcon>()
                .AddSingleton<IAgentService, AgentService>()
                .AddTransient<NotifyIconContextMenuFlyoutViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<HomeViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddTransient<ChatViewModel>();
        });

        App.Host = hostBuilder.Build();
        App.IsHostInitialized = true;

        Application.Start(static ignored =>
        {
            SynchronizationContextPolyfill context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            App app = App.Host.Services.GetRequiredService<App>();
        });
    }
}
