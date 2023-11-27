﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ReplaceTransient<TService, TImplementation>(
        this IServiceCollection services,
        bool predicate = true)
        where TService : class
        where TImplementation : class, TService =>
        services.Replace<TService, TImplementation>(ServiceLifetime.Transient, predicate);

    public static IServiceCollection ReplaceScoped<TService, TImplementation>(
        this IServiceCollection services,
        bool predicate = true)
        where TService : class
        where TImplementation : class, TService =>
        services.Replace<TService, TImplementation>(ServiceLifetime.Scoped, predicate);

    public static IServiceCollection ReplaceSingleton<TService, TImplementation>(
        this IServiceCollection services,
        bool predicate = true)
        where TService : class
        where TImplementation : class, TService =>
        services.Replace<TService, TImplementation>(ServiceLifetime.Scoped, predicate);

    private static IServiceCollection Replace<TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        bool predicate = true)
        where TService : class
        where TImplementation : class, TService
    {
        if (!predicate)
        {
            return services;
        }

        var serviceType = typeof(TService);
        var implementationType = typeof(TImplementation);

        // Remove all matching service registrations
        foreach (var descriptor in services
            .Where(x => x.ServiceType == serviceType)
            .ToList())
        {
            services.Remove(descriptor);
        }

        services.Add(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));

        return services;
    }

    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services, bool predicate)
        where TDecorator : TService =>
        predicate
            ? services.Decorate<TService, TDecorator>()
            : services;

    public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, bool predicate)
        where THostedService : class, IHostedService =>
        predicate
            ? services.AddHostedService<THostedService>()
            : services;
}
