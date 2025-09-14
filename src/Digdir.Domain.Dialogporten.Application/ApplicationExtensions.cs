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
using Digdir.Domain.Dialogporten.Application.Common.Context;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            .AddTransient<IUserOrganizationRegistry, UserOrganizationRegistry>()
            .AddTransient<IUserResourceRegistry, UserResourceRegistry>()
            .AddTransient<IUserRegistry, UserRegistry>()
            .AddTransient<IUserParties, UserParties>()
            .AddTransient<IClock, Clock>()
            .AddDataLoaders()
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(FeatureMetricBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DataLoaderBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainContextBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(SilentUpdateBehaviour<,>))
            .AddScoped<FeatureMetricRecorder>()
            .AddServiceResourceResolvers();

        var otelEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrEmpty(otelEndpoint) || !Uri.IsWellFormedUriString(otelEndpoint, UriKind.Absolute))
        {
            // No OpenTelemetry endpoint configured - use console logging
            services.AddScoped<IFeatureMetricDeliveryContext, LoggingFeatureMetricDeliveryContext>();
        }
        else
        {
            // OpenTelemetry endpoint configured - use OpenTelemetry logging
            services.AddScoped<IFeatureMetricDeliveryContext, OtelFeatureMetricLoggingDeliveryContext>();
        }

        if (!environment.IsDevelopment())
        {
            return services;
        }

        var localDeveloperSettings = configuration.GetLocalDevelopmentSettings();
        services.Decorate<IUserResourceRegistry, LocalDevelopmentUserResourceRegistryDecorator>(
            predicate:
            localDeveloperSettings.UseLocalDevelopmentUser ||
            localDeveloperSettings.UseLocalDevelopmentResourceRegister);

        services.Decorate<IUserOrganizationRegistry, LocalDevelopmentUserOrganizationRegistryDecorator>(
            predicate:
            localDeveloperSettings.UseLocalDevelopmentUser ||
            localDeveloperSettings.UseLocalDevelopmentOrganizationRegister);

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
        var openResolverType = typeof(IServiceResourceResolver<>);

        // Get all non-abstract, non-interface types from the provided assemblies (or the calling assembly if none are provided)
        var concreteTypes = assemblies
            .DefaultIfEmpty(Assembly.GetCallingAssembly())
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .ToList();

        // Find all types that implement IServiceResourceResolver<T> and map them to their corresponding T
        var resolverMaps = concreteTypes
            .SelectMany(x => x.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openResolverType),
                (c, i) => new { Implementation = c, Interface = i, Inner = i.GetGenericArguments()[0] })
            .ToList();

        // For each type that implements IBaseRequest, find the corresponding resolver implementation
        // based on the mapping created above
        var requestResolverMap = concreteTypes
            .Where(x => x.IsAssignableTo(typeof(IBaseRequest)))
            .Select(x => (Request: x, Resolver: resolverMaps
                .FirstOrDefault(m => x.IsAssignableTo(m.Inner))
                ?.Implementation))
            .ToList();

        // If any request types do not have a corresponding resolver, throw an exception
        // to ensure all requests are properly handled
        var errorMessage = string.Join(Environment.NewLine, requestResolverMap
            .Where(x => x.Resolver is null)
            .Select(x => $"- {x.Request.FullName}"));
        if (errorMessage != string.Empty)
        {
            throw new InvalidOperationException(
                $"All requests are expected to have an associated {nameof(IServiceResourceResolver<object>)}. Could " +
                $"not find resolvers for the following requests. If a request cannot be associated with a service " +
                $"resource or tracking service resource information is irrelevant for the request, mark the it " +
                $"with {nameof(IDoNotCareAboutServiceResource)}.{Environment.NewLine}{errorMessage}");
        }

        // Register each request type with its corresponding resolver implementation
        foreach (var (request, resolver) in requestResolverMap)
        {
            services.TryAddTransient(openResolverType.MakeGenericType(request), resolver!);
        }

        return services;
    }
}
