using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Snap.Nicole.Resources;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.Settings;

internal sealed class AppSettingsSynchronizer(IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<AppSettings> monitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
    private IDisposable? changeRegistration;

    public void Dispose()
    {
        changeRegistration?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Apply(monitor.CurrentValue);
        changeRegistration = monitor.OnChange(Apply);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Apply(AppSettings settings)
    {
        App.Current.SynchronizationContext.Post(static state =>
        {
            if (state is AppSettings current)
            {
                StringResourceProxy.Default.CurrentCulture = CultureInfo.GetCultureInfo(current.Language);
            }
        }, settings);
    }
}
