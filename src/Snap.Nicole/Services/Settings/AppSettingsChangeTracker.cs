using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Snap.Nicole.Resources;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.Settings;

internal sealed class AppSettingsChangeTracker(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<AppSettings> monitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
    private IDisposable? changeRegistration;

    public void Dispose()
    {
        changeRegistration?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        HandleChange(monitor.CurrentValue);
        changeRegistration = monitor.OnChange(HandleChange);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void HandleChange(AppSettings settings)
    {
        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is not AppSettings current)
            {
                return;
            }

            StringResourceProxy.Default.CurrentCulture = CultureInfo.GetCultureInfo(current.Language);
        }, settings);
    }
}
