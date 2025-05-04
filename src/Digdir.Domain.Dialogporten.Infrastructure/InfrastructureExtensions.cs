using System.Data;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Services;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly.Extensions.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Digdir.Domain.Dialogporten.Infrastructure.Common;
using FluentValidation;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.OrganizationRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.GraphQl;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using HotChocolate.Subscriptions;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;
using Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;
using MassTransit;
using MediatR;

namespace Digdir.Domain.Dialogporten.Infrastructure;

public static class InfrastructureExtensions
{
    public static IPubSubInfrastructureChoice AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
        => new InfrastructureBuilder(services, configuration, environment);

    internal static void AddInfrastructure_Internal(InfrastructureBuilderContext builderContext)
    {
        ArgumentNullException.ThrowIfNull(builderContext);
        var (services, configuration, environment, infrastructureSettings, _) = builderContext;

        services

            // Framework
            .AddDbContext<DialogDbContext>((services, options) =>
            {
                var connectionString = services.GetRequiredService<IOptions<InfrastructureSettings>>()
                    .Value.DialogDbConnectionString;
                options.UseNpgsql(connectionString, o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .AddInterceptors(
                    services.GetRequiredService<PopulateActorNameInterceptor>(),
                    services.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>()
                );
            })
            .AddHostedService<DevelopmentMigratorHostedService>()
            .AddHostedService<DevelopmentCleanupOutboxHostedService>()
            .AddHostedService<DevelopmentSubjectResourceSyncHostedService>()
            .AddHostedService<DevelopmentResourcePolicyInformationSyncHostedService>()
            .AddValidatorsFromAssembly(InfrastructureAssemblyMarker.Assembly, ServiceLifetime.Transient, includeInternalTypes: true)
            .AddPolicyRegistry((_, registry) =>
            {
                registry.Add(PollyPolicy.DefaultHttpRetryPolicy, HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3)));
            })
            .AddCustomHealthChecks()

            // Scoped
            .AddScoped<IDialogDbContext>(x => x.GetRequiredService<DialogDbContext>())
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<ConvertDomainEventsToOutboxMessagesInterceptor>()
            .AddScoped<PopulateActorNameInterceptor>()

            // Transient
            .AddTransient<ISubjectResourceRepository, SubjectResourceRepository>()
            .AddTransient<IResourcePolicyInformationRepository, ResourcePolicyInformationRepository>()
            .AddTransient<Lazy<IPublishEndpoint>>(x =>
                new Lazy<IPublishEndpoint>(x.GetRequiredService<IPublishEndpoint>))
            .AddTransient<Lazy<ITopicEventSender>>(x =>
                new Lazy<ITopicEventSender>(x.GetRequiredService<ITopicEventSender>))

            // Singleton
            .AddSingleton<INotificationProcessingContextFactory, NotificationProcessingContextFactory>()

            // HttpClient
            .AddHttpClients(infrastructureSettings)

            // Decorators
            .Decorate(typeof(INotificationHandler<>), typeof(IdempotentNotificationHandler<>));

        services.AddFusionCacheNeueccMessagePackSerializer();
        services.AddStackExchangeRedisCache(opt => opt.Configuration = infrastructureSettings.Redis.ConnectionString);
        services.AddFusionCacheStackExchangeRedisBackplane(opt => opt.Configuration = infrastructureSettings.Redis.ConnectionString);

        services.ConfigureFusionCache(nameof(Altinn.NameRegistry), new()
        {
            Duration = TimeSpan.FromHours(24),
            FailSafeMaxDuration = TimeSpan.FromHours(26)
        })
        .ConfigureFusionCache(nameof(Altinn.ResourceRegistry), new()
        {
            Duration = TimeSpan.FromMinutes(20),
            // The resource list is several megabytes and might take a while to process
            FactoryHardTimeout = TimeSpan.FromSeconds(10)
        })
        .ConfigureFusionCache(nameof(Altinn.OrganizationRegistry), new()
        {
            Duration = TimeSpan.FromHours(24),
            FailSafeMaxDuration = TimeSpan.FromHours(26)
        })
        .ConfigureFusionCache(nameof(Altinn.Authorization), new()
        {
            // This cache has high cardinality, and will create several keys per user:
            // - One entry per user per dialog search and combination of party and service resource parameters
            // - One entry per dialog details
            // EU systems iterating over several parties in search view and fetching details for each dialog will
            // potentially create hundreds over even thousands of cache entries within the cache TTL. To avoid
            // memory exhaustion, we therefore disable the memory cache for authorization results, and rely solely on
            // the distributed cache.
            SkipMemoryCache = true,
            // In normal operations, 15 minutes delay is deemed acceptable for authorization data
            Duration = TimeSpan.FromMinutes(15),
            // In case Altinn Authorization is down/overloaded, we allow the re-usage of stale authorization data
            // for an additional 15 minutes. Using default FailSafeThrottleDuration.
            FailSafeMaxDuration = TimeSpan.FromMinutes(30),
            // If the request to Altinn Authorization takes too long, we allow the cache to return stale data
            // temporarily whilst updating the cache in the background. Note that we are also using eager refresh
            // and a backplane.
            FactorySoftTimeout = TimeSpan.FromSeconds(2),
            // Timeout for the cache to wait for the factory to complete, which when reached without fail-safe data
            // will cause an exception to be thrown
            FactoryHardTimeout = TimeSpan.FromSeconds(10)
        })
        .ConfigureFusionCache(nameof(AuthorizedPartiesResult), new()
        {
            // We keep authorized parties in a separate cache key, as this originates from a different API
            // and has lees cardinality than the dialog authorization cache (only one per user). We therefore
            // allow a memory cache for this.
            Duration = TimeSpan.FromMinutes(15),
            // In case Altinn Access Management is down/overloaded, we allow the re-usage of stale authorization data
            // for an additional 15 minutes. Using default FailSafeThrottleDuration.
            FailSafeMaxDuration = TimeSpan.FromMinutes(30),
            // If the request to Altinn AccessManagement takes too long, we allow the cache to return stale data
            // temporarily whilst updating the cache in the background. Note that we are also using eager refresh
            // and a backplane.
            FactorySoftTimeout = TimeSpan.FromSeconds(2),
            // Timeout for the cache to wait for the factory to complete, which when reached without fail-safe data
            // will cause an exception to be thrown
            FactoryHardTimeout = TimeSpan.FromSeconds(10)
        })
        .ConfigureFusionCache(nameof(SubjectResource), new()
        {
            Duration = TimeSpan.FromMinutes(20)
        });

        if (environment.IsEnvironment("yt01"))
        {
            services.ReplaceTransient<ICloudEventBus, ConsoleLogEventBus>();
        }

        if (!environment.IsDevelopment())
        {
            return;
        }

        var localDeveloperSettings = configuration.GetLocalDevelopmentSettings();
        services
            .ReplaceTransient<ICloudEventBus, ConsoleLogEventBus>(predicate: localDeveloperSettings.UseLocalDevelopmentCloudEventBus)
            .ReplaceTransient<IResourceRegistry, LocalDevelopmentResourceRegistry>(predicate: localDeveloperSettings.UseLocalDevelopmentResourceRegister)
            .ReplaceTransient<IAltinnAuthorization, LocalDevelopmentAltinnAuthorization>(predicate: localDeveloperSettings.UseLocalDevelopmentAltinnAuthorization)
            .ReplaceSingleton<IFusionCache, NullFusionCache>(predicate: localDeveloperSettings.DisableCache);
    }

    internal static void AddPubSubCapabilities(InfrastructureBuilderContext builderContext, List<Action<IBusRegistrationConfigurator>> customConfigurations)
    {
        // ATTENTION: If you need to add custom configurations to the bus, you should
        // consider adding equivalent config to AddPubCapabilities method as well
        builderContext.Services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<DialogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();

                // This lowers the isolation level from Serializable to ReadCommitted. This avoids contention issues
                // and provides sufficient guarantees for the outbox bus using polling with FOR UPDATE SKIP LOCKED,
                // where MassTransit only perform message forwarding to Azure Service Bus and isn't performing any
                // mutation of the outbox besides deleting rows after successful passing to ASB.
                o.IsolationLevel = IsolationLevel.ReadCommitted;
            });

            foreach (var customConfiguration in customConfigurations)
            {
                customConfiguration(x);
            }

            if (builderContext.Environment.IsDevelopment() && builderContext.DevSettings.UseInMemoryServiceBusTransport)
            {
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                return;
            }

            x.ConfigureHealthCheckOptions(options => options.Tags.Add("dependencies"));
            x.AddConfigureEndpointsCallback((_, cfg) =>
            {
                if (cfg is IServiceBusReceiveEndpointConfigurator sb)
                {
                    sb.ConfigureDeadLetterQueueDeadLetterTransport();
                    sb.ConfigureDeadLetterQueueErrorTransport();
                }
            });
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(builderContext.InfraSettings.MassTransit.Host);
                cfg.ConfigureEndpoints(context);
            });
        });

        new DummyRequestExecutorBuilder { Services = builderContext.Services }
            .AddRedisSubscriptions(_ => ConnectionMultiplexer.Connect(builderContext.InfraSettings.Redis.ConnectionString),
                new SubscriptionOptions
                {
                    TopicPrefix = GraphQlSubscriptionConstants.SubscriptionTopicPrefix
                });
    }

    internal static void AddPubCapabilities(InfrastructureBuilderContext builderContext)
    {
        // ATTENTION: If you need to add custom configurations to the bus, you should
        // consider adding equivalent config to AddPubSubCapabilities method as well
        builderContext.Services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<DialogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox(y => y.DisableDeliveryService());
                o.DisableInboxCleanupService();
            });

            if (builderContext.Environment.IsDevelopment() && builderContext.DevSettings.UseInMemoryServiceBusTransport)
            {
                x.UsingInMemory();
                return;
            }

            x.UsingAzureServiceBus();
        });

        new DummyRequestExecutorBuilder { Services = builderContext.Services }
            .AddRedisSubscriptions(_ => ConnectionMultiplexer.Connect(builderContext.InfraSettings.Redis.ConnectionString),
                new SubscriptionOptions
                {
                    TopicPrefix = GraphQlSubscriptionConstants.SubscriptionTopicPrefix
                });
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services,
        InfrastructureSettings infrastructureSettings)
    {
        services.
            AddMaskinportenHttpClient<ICloudEventBus, AltinnEventsClient, SettingsJwkClientDefinition>(
                infrastructureSettings,
                x => x.ClientSettings.ExhangeToAltinnToken = true)
            .ConfigureHttpClient((services, client) =>
            {
                client.BaseAddress = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Altinn.EventsBaseUri;
            });
        services.AddHttpClient<IResourceRegistry, ResourceRegistryClient>((services, client) =>
                client.BaseAddress = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Altinn.BaseUri)
            .AddPolicyHandlerFromRegistry(PollyPolicy.DefaultHttpRetryPolicy);

        services.AddHttpClient<IServiceOwnerNameRegistry, ServiceOwnerNameRegistryClient>((services, client) =>
                client.BaseAddress = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.AltinnCdn.BaseUri)
            .AddPolicyHandlerFromRegistry(PollyPolicy.DefaultHttpRetryPolicy);

        services.AddMaskinportenHttpClient<IPartyNameRegistry, PartyNameRegistryClient, SettingsJwkClientDefinition>(
                infrastructureSettings,
                x => x.ClientSettings.ExhangeToAltinnToken = true)
            .ConfigureHttpClient((services, client) =>
            {
                var altinnSettings = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Altinn;
                client.BaseAddress = altinnSettings.BaseUri;
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", altinnSettings.SubscriptionKey);
            })
            .AddPolicyHandlerFromRegistry(PollyPolicy.DefaultHttpRetryPolicy);

        services.AddMaskinportenHttpClient<IAltinnAuthorization, AltinnAuthorizationClient, SettingsJwkClientDefinition>(
                infrastructureSettings,
                x => x.ClientSettings.ExhangeToAltinnToken = true)
            .ConfigureHttpClient((services, client) =>
            {
                var altinnSettings = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Altinn;
                client.BaseAddress = altinnSettings.BaseUri;
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", altinnSettings.SubscriptionKey);
            })
            .AddPolicyHandlerFromRegistry(PollyPolicy.DefaultHttpRetryPolicy);

        return services;
    }

    private static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis", tags: ["dependencies", "redis"])
            .AddDbContextCheck<DialogDbContext>("postgres", tags: ["dependencies", "critical"]);

        services.AddSingleton<RedisHealthCheck>();

        return services;
    }

    private static IServiceCollection ConfigureFusionCache(this IServiceCollection services, string cacheName, FusionCacheSettings? settings = null)
    {
        settings ??= new FusionCacheSettings();

        services.AddFusionCache(cacheName)
            .WithOptions(options =>
            {
                options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2);
            })
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = settings.Duration,

                IsFailSafeEnabled = settings.IsFailSafeEnabled,
                FailSafeMaxDuration = settings.FailSafeMaxDuration,
                FailSafeThrottleDuration = settings.FailSafeThrottleDuration,

                FactorySoftTimeout = settings.FactorySoftTimeout,
                FactoryHardTimeout = settings.FactoryHardTimeout,

                DistributedCacheSoftTimeout = settings.DistributedCacheSoftTimeout,
                DistributedCacheHardTimeout = settings.DistributedCacheHardTimeout,

                AllowBackgroundDistributedCacheOperations = settings.AllowBackgroundDistributedCacheOperations,

                JitterMaxDuration = settings.JitterMaxDuration,
                EagerRefreshThreshold = settings.EagerRefreshThreshold,

                SkipMemoryCacheWrite = settings.SkipMemoryCache,
                SkipMemoryCacheRead = settings.SkipMemoryCache,

                // This will stop deserialization exceptions to be re-thrown, which will cause the factory to run as if
                // the cache entry was not found. This avoids crashes which otherwise would happen if entities that
                // are cached are changed in a way that makes them incompatible with the cached version.
                ReThrowSerializationExceptions = false,

                EnableAutoClone = true
            })
            .WithRegisteredSerializer()
            // If Redis is disabled (eg. in local development or non-web runtimes), we must instruct FusionCache to
            // allow the use of InMemoryDistributedCache (it is by default ignored as a IDistributedCache implementation)
            // TryWithRegisteredBackplane is used to ensure that we can continue without Redis as backplane
            .WithRegisteredDistributedCache(ignoreMemoryDistributedCache: false)
            .TryWithRegisteredBackplane();

        return services;
    }

    private static IHttpClientBuilder AddMaskinportenHttpClient<TClient, TImplementation, TClientDefinition>(
        this IServiceCollection services,
        InfrastructureSettings infrastructureSettings,
        Action<TClientDefinition>? configureClientDefinition = null)
        where TClient : class
        where TImplementation : class, TClient
        where TClientDefinition : class, IClientDefinition
    {
        services.RegisterMaskinportenClientDefinition<TClientDefinition>(typeof(TClient).FullName, infrastructureSettings.Maskinporten);
        return services
            .AddHttpClient<TClient, TImplementation>()
            .AddMaskinportenHttpMessageHandler<TClientDefinition, TClient>(configureClientDefinition);
    }

    private sealed class FusionCacheSettings
    {
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromHours(2);
        public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan FactorySoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan FactoryHardTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan DistributedCacheSoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan DistributedCacheHardTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public bool AllowBackgroundDistributedCacheOperations { get; set; } = true;
        public bool IsFailSafeEnabled { get; set; } = true;
        public TimeSpan JitterMaxDuration { get; set; } = TimeSpan.FromSeconds(2);
        public float EagerRefreshThreshold { get; set; } = 0.8f;
        public bool SkipMemoryCache { get; set; }
    }
}
