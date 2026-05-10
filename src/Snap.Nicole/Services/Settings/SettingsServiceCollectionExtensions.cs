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
            services.TryAddSingleton<IOptionsProvider<T>>(sp => new JsonSettingsOptionsProvider<T>(fileNameWithoutExtension));
            services.TryAddSingleton(sp => (IOptionsMonitor<T>)sp.GetRequiredService<IOptionsProvider<T>>());
            services.TryAddSingleton(sp => (IOptionsWriter<T>)sp.GetRequiredService<IOptionsProvider<T>>());

            return services;
        }
    }
}
