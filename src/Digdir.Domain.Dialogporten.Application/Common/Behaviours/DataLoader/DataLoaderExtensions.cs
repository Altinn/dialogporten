using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

internal static class DataLoaderExtensions
{
    public static IServiceCollection AddDataLoaders(this IServiceCollection services, params Assembly[] assemblies)
    {
        var loaderTypes = assemblies
            .DefaultIfEmpty(Assembly.GetCallingAssembly())
            .SelectMany(a => a.DefinedTypes)
            .Where(t => !t.ContainsGenericParameters)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t =>
                t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataLoader<,>))
                    .Select(i => new { Implementation = t, Service = i }))
            .ToList();

        foreach (var loader in loaderTypes)
        {
            services.AddTransient(loader.Service, loader.Implementation);
        }

        services.AddScoped<IDataLoaderContext, DataLoaderContext>();
        services.AddScoped<FullDialogAggregateDataLoader>();

        return services;
    }
}
