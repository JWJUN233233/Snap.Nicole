using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace Snap.Nicole.Core.Hosting;

internal sealed class ApplicationLifeTime(IHost host) : IApplicationLifeTime
{
    public bool IsExiting { get; private set; }

    public async Task ShutdownAsync()
    {
        IsExiting = true;
        Application.Current.Exit();

        using (host)
        {
            await host.StopAsync();
        }
    }
}