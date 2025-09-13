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
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DataLoaderBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainContextBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(SilentUpdateBehaviour<,>))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(FeatureMetricBehaviour<,>))
            .AddScoped<FeatureMetricRecorder>()
            .AddServiceResourceResolvers(thisAssembly);

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

    private static IServiceCollection AddServiceResourceResolvers(this IServiceCollection services, Assembly assembly)
    {
        var serviceResourceResolverType = typeof(IServiceResourceResolver<>);

        var implementations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == serviceResourceResolverType))
            .ToList();

        foreach (var implementation in implementations)
        {
            var serviceInterface = implementation.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == serviceResourceResolverType);

            if (serviceInterface != null)
            {
                services.AddTransient(serviceInterface, implementation);
            }
        }

        return services;
    }
}
