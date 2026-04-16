using Microsoft.Extensions.Hosting;
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

        Environment.Exit(0);
    }
}