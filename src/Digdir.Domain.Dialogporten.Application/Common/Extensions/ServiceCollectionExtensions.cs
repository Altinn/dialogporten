using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ReplaceTransient<TService, TImplementation>(bool predicate = true)
            where TService : class
            where TImplementation : class, TService =>
            services.Replace<TService, TImplementation>(ServiceLifetime.Transient, predicate);

        public IServiceCollection ReplaceScoped<TService, TImplementation>(bool predicate = true)
            where TService : class
            where TImplementation : class, TService =>
            services.Replace<TService, TImplementation>(ServiceLifetime.Scoped, predicate);

        public IServiceCollection ReplaceSingleton<TService, TImplementation>(bool predicate = true)
            where TService : class
            where TImplementation : class, TService =>
            services.Replace<TService, TImplementation>(ServiceLifetime.Singleton, predicate);

        private IServiceCollection Replace<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Scoped,
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

        public IServiceCollection Decorate<TService, TDecorator>(bool predicate)
            where TDecorator : TService =>
            predicate
                ? services.Decorate<TService, TDecorator>()
                : services;

        public IServiceCollection AddHostedService<THostedService>(bool predicate)
            where THostedService : class, IHostedService =>
            predicate
                ? services.AddHostedService<THostedService>()
                : services;
    }
}
