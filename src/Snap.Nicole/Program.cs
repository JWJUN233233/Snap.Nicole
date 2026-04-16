using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Core.Threading;
using System;
using System.Threading;
using WinRT;

namespace Snap.Nicole;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {

        IHostBuilder builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureServices((context, services) =>
        {
            services.AddTransient<MainWindow>();
            services.AddSingleton(typeof(IWindowLifeTime<>), typeof(WindowLifeTime<>));

            services.AddSingleton<App>();
            services.AddSingleton<IApplicationLifeTime, ApplicationLifeTime>();
        });

        IHost host = builder.Build();
        App.Host = host;
        App.IsHostInitialized = true;

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(ignored =>
        {
            SynchronizationContextPolyfill context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            App app = host.Services.GetRequiredService<App>();
        });
    }
}
