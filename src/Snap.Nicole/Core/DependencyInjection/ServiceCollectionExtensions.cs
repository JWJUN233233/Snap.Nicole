using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.UI.Xaml;
using Snap.Nicole.Core.Hosting;
using System;

namespace Snap.Nicole.Core.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddXamlWindows(Action<IXamlWindowBuilder> configure)
        {
            XamlWindowBuilder xamlWindowBuilder = new(services);
            configure(xamlWindowBuilder);

            services.TryAddSingleton(typeof(IWindowLifeTime<>), typeof(WindowLifeTime<>));
            return services;
        }

        public IServiceCollection AddXamlApplication<TApplication>()
            where TApplication : Application
        {
            return services
                .AddSingleton<TApplication>()
                .AddSingleton<IApplicationLifeTime, ApplicationLifeTime>();
        }
    }
}
