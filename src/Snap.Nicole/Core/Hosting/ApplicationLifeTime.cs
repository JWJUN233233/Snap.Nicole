using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Snap.Nicole.Core.Hosting;

internal sealed class ApplicationLifeTime(IHost host) : IApplicationLifeTime
{
    public async Task ShowdownAsync()
    {
        using (host)
        {
            await host.StopAsync();
        }

        Application.Current.Exit();
    }
}