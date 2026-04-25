using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.DependencyInjection;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.UI.Shell;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using WinRT;

[assembly: DisableRuntimeMarshalling]

namespace Snap.Nicole;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.ConfigureServices(static (context, services) =>
        {
            services
                .AddXamlApplication<App>()
                .AddXamlWindows(static builder =>
                {
                    builder
                        .AddXamlWindow<MainWindow>()
                        .AddXamlWindow<NotifyIconXamlHostWindow>();
                });
        });

        IHost host = hostBuilder.Build();
        App.Host = host;
        App.IsHostInitialized = true;

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(static ignored =>
        {
            SynchronizationContextPolyfill context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            App app = App.Host.Services.GetRequiredService<App>();
        });
    }
}