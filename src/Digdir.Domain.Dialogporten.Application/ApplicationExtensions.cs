using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureToggle;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchNew;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SearchDialogQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogQuery;
using SearchDialogResult = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogResult;

namespace Digdir.Domain.Dialogporten.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var thisAssembly = Assembly.GetExecutingAssembly();

        services.AddOptions<ApplicationSettings>()
            .Bind(configuration.GetSection(ApplicationSettings.ConfigurationSectionName))
            .ValidateFluently()
            .ValidateOnStart();

        // Configures FluentValidation to use property names as
        // display names without an added space.
        // 'CreatedAt', not 'Created At'.
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name;

        // Disable FluentValidation localization
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services
            // Framework
            .AddAutoMapper(thisAssembly)
            .AddMediatR(x =>
            {
                x.RegisterServicesFromAssembly(thisAssembly);
                x.TypeEvaluator = type => !type.IsAssignableTo(typeof(IIgnoreOnAssemblyScan));
                x.NotificationPublisherType = typeof(TaskWhenAllPublisher);
            })
            .AddValidatorsFromAssembly(thisAssembly, ServiceLifetime.Transient, includeInternalTypes: true,
                filter: type => !type.ValidatorType.IsAssignableTo(typeof(IIgnoreOnAssemblyScan)))

            // Singleton
            .AddSingleton<ICompactJwsGenerator, Ed25519Generator>()

            // Scoped
            .AddScoped<IApplicationContext, ApplicationContext>()
            .AddScoped<IDomainContext, DomainContext>()
            .AddScoped<ITransactionTime, TransactionTime>()
            .AddScoped<IDialogTokenGenerator, DialogTokenGenerator>()

            // Transient
            .AddTransient<IServiceResourceAuthorizer, ServiceResourceAuthorizer>()
            .AddTransient<IUserResourceRegistry, UserResourceRegistry>()
            .AddTransient<IUserRegistry, UserRegistry>()
            .AddTransient<IUserParties, UserParties>()
            .AddTransient<IClock, Clock>()
            .AddTransient<IApplicationFeatureToggle<SearchDialogQuery, SearchDialogResult>, OptimizedEndUserDialogSearchFeatureToggle>()
            .AddDataLoaders()
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DialogportenMaintenanceModeBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ApplicationFeatureToggleBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(FeatureMetricBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DataLoaderBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainContextBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(SilentUpdateBehaviour<,>))
            .AddTransient<IFeatureMetricDeliveryContext, LoggingFeatureMetricDeliveryContext>()
            .AddScoped<FeatureMetricRecorder>()
            .AddServiceResourceResolvers();

        if (!environment.IsDevelopment())
        {
            return services;
        }

        var localDeveloperSettings = configuration.GetLocalDevelopmentSettings();
        services.Decorate<IUserResourceRegistry, LocalDevelopmentUserResourceRegistryDecorator>(
            predicate:
            localDeveloperSettings.UseLocalDevelopmentUser ||
            localDeveloperSettings.UseLocalDevelopmentResourceRegister);

        services.Decorate<IUserRegistry, LocalDevelopmentUserRegistryDecorator>(
            predicate:
            localDeveloperSettings.UseLocalDevelopmentUser ||
            localDeveloperSettings.UseLocalDevelopmentNameRegister);

        services.Decorate<ICompactJwsGenerator, LocalDevelopmentCompactJwsGeneratorDecorator>(
            predicate: localDeveloperSettings.UseLocalDevelopmentCompactJwsGenerator);

        return services;
    }

    private static IServiceCollection AddServiceResourceResolvers(
        this IServiceCollection services,
        params IEnumerable<Assembly> assemblies)
    {
        var openResolverType = typeof(IFeatureMetricServiceResourceResolver<>);

        // Get all non-abstract, non-interface types from the provided assemblies (or the calling assembly
        // if none are provided)
        var concreteTypes = assemblies
            .DefaultIfEmpty(Assembly.GetCallingAssembly())
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .ToList();

        // Find all types that implement IFeatureMetricsServiceResourceResolver<T> and map them to their corresponding T
        var resolverMaps = concreteTypes
            .SelectMany(x => x.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openResolverType),
                (c, i) => new { Implementation = c, Inner = i.GetGenericArguments()[0] })
            .ToList();

        // For each type that implements IBaseRequest, find the corresponding resolver implementation
        // based on the mapping created above
        var requestResolverMap = concreteTypes
            .Where(x => x.IsAssignableTo(typeof(IBaseRequest)))
            .Select(x => (Request: x, Resolver: resolverMaps
                .SingleOrDefault(m => x.IsAssignableTo(m.Inner))
                ?.Implementation))
            .ToList();

        // If any request types do not have a corresponding resolver, throw an exception
        // to ensure all requests are properly handled
        var missingResolvers = requestResolverMap
            .Where(x => x.Resolver is null)
            .Select(x => x.Request.FullName)
            .ToList();

        if (missingResolvers is { Count: > 0 })
        {
            var requestList = string.Join(Environment.NewLine, missingResolvers.Select(x => $"  • {x}"));
            throw new InvalidOperationException(
                $"Missing feature metric resolvers for {missingResolvers.Count} request type(s):{Environment.NewLine}{requestList}{Environment.NewLine}{Environment.NewLine}" +
                $"To fix this, implement one of these interfaces on each request:{Environment.NewLine}" +
                $"  • {nameof(IFeatureMetricServiceResourceThroughDialogIdRequest)} - for requests with DialogId{Environment.NewLine}" +
                $"  • {nameof(IFeatureMetricServiceResourceRequest)} - for requests with no DialogId but with ServiceResource property{Environment.NewLine}" +
                $"  • {nameof(IFeatureMetricServiceResourceIgnoreRequest)} - for requests that don't need feature metrics");
        }

        // Register each request type with its corresponding resolver implementation
        foreach (var (request, resolver) in requestResolverMap)
        {
            services.TryAddTransient(openResolverType.MakeGenericType(request), resolver!);
        }

        return services;
    }
}
