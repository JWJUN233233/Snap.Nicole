using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Snap.Nicole.Services.Settings;

internal static class SettingsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJsonSettings<T>(string fileNameWithoutExtension)
            where T : class, new()
        {
            services.TryAddSingleton<IOptionsMonitor<T>>(sp => new JsonSettingsOptionsProvider<T>(fileNameWithoutExtension));
            services.TryAddSingleton(sp => (IOptionsWriter<T>)sp.GetRequiredService<IOptionsMonitor<T>>());
            return services;
        }
    }
}
