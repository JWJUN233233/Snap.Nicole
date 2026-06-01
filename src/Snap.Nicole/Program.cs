global using Microsoft.Extensions.DependencyInjection;
global using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.DependencyInjection;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.Threading;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.Git;
using Snap.Nicole.Services.Settings;
using Snap.Nicole.UI.Shell;
using Snap.Nicole.UI.Xaml.Navigation;
using Snap.Nicole.UI.Xaml.Windows;
using Snap.Nicole.ViewModels;
using Snap.Nicole.ViewModels.NotifyIcon;
using Snap.Nicole.ViewModels.Settings;
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
        ComWrappersSupport.InitializeComWrappers();

        AppContext.SetSwitch("System.Net.Http.EnableActivityPropagation", true);

        using IDisposable sentry = SentrySdkInitializationSupport.Initialize();
        using SentryDiagnosticSpan startupSpan = SentryDiagnostics.StartSpan("app.startup", "Initialize Snap.Nicole host");

        try
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureLogging(static logging =>
            {
                logging.AddSentry(SentrySdkInitializationSupport.ConfigureLogging);
            });

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
                    .AddSingleton<IAgentConversationStore, AgentConversationFileStore>()
                    .AddSingleton<IModelProfileService, ModelProfileService>()
                    .AddSingleton<ISettingsGitSyncService, SettingsGitSyncService>()
                    .AddTransient<NotifyIconContextMenuFlyoutViewModel>()
                    .AddTransient<MainViewModel>()
                    .AddTransient<HomeViewModel>()
                    .AddTransient<SettingsGitSyncViewModel>()
                    .AddTransient<SettingsViewModel>()
                    .AddTransient<AgentViewModel>();

                services
                    .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                    .AddSingleton(serviceProvider =>
                    {
                        ObjectPoolProvider poolProvider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
                        return poolProvider.CreateStringBuilderPool(initialCapacity: 256, maximumRetainedCapacity: 4096);
                    });

            });

            App.Host = hostBuilder.Build();
            App.IsHostInitialized = true;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, startupSpan, "app.startup");
            throw;
        }

        Application.Start(static ignored =>
        {
            using SentryDiagnosticSpan xamlSpan = SentryDiagnostics.StartSpan("app.xaml.start", "Start XAML application");

            SynchronizationContextPolyfill context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            try
            {
                App app = App.Host.Services.GetRequiredService<App>();
            }
            catch (Exception ex)
            {
                SentryDiagnostics.CaptureException(ex, xamlSpan, "app.xaml.start");
                throw;
            }
        });
    }
}
