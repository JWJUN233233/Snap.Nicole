using Microsoft.Extensions.DependencyInjection.Extensions;
using Snap.Nicole.Core;
using System.ComponentModel;

namespace Snap.Nicole.Services.Settings;

internal static class SettingsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJsonSettings<T>(string fileNameWithoutExtension)
            where T : class, INotifyPropertyChanged, ICopyFrom<T>, new()
        {
            services.TryAddSingleton<IOptionsProvider<T>>(sp => new JsonSettingsOptionsProvider<T>(fileNameWithoutExtension));

            return services;
        }
    }
}
