using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.Diagnostics;
using System.Threading.Tasks;

namespace Snap.Nicole.Core.Hosting;

internal sealed class ApplicationLifeTime(IHost host) : IApplicationLifeTime
{
    public bool IsExiting { get; private set; }

    public async Task ShutdownAsync()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("app.shutdown", "Shutdown application");

        try
        {
            IsExiting = true;
            Application.Current.Exit();

            using (host)
            {
                await host.StopAsync();
            }
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "app.shutdown");
            throw;
        }
    }
}
