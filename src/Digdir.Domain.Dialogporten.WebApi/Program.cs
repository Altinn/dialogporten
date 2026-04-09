using Altinn.ApiClients.Maskinporten.Config;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.WebApi;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Patch;
using Digdir.Library.Utils.AspNet;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NSwag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

// Using two-stage initialization to catch startup errors.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.WithEnvironmentName()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .TryWriteToOpenTelemetry()
    .CreateBootstrapLogger();

try
{
    BuildAndRun(args);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void BuildAndRun(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    var isOpenApiDocGen = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    builder.WebHost.ConfigureKestrel(kestrelOptions =>
    {
        kestrelOptions.Limits.MaxRequestBodySize = Constants.MaxRequestBodySizeInBytes;
    });

    builder.Configuration
        .AddAzureConfiguration(builder.Environment.EnvironmentName)
        .AddLocalConfiguration(builder.Environment);

    if (isOpenApiDocGen)
    {
        // The build-time OpenAPI generator boots the full host. Overlay safe dummy values
        // so the normal startup path can run without local secrets or external dependencies.
        builder.Configuration.AddInMemoryCollection(GetOpenApiDocumentGenerationOverrides());
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Warning()
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithEnvironmentName()
        .Enrich.FromLogContext()
        .Filter.WithHandledPostgresExceptionFilter()
        .WriteTo.OpenTelemetryOrConsole(context));

    builder.Services
        .AddOptions<WebApiSettings>()
        .Bind(builder.Configuration.GetSection(WebApiSettings.SectionName))
        .ValidateFluently()
        .ValidateOnStart();

    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IHostLifetime>(sp => new DelayedShutdownHostLifetime(
            sp.GetRequiredService<IHostApplicationLifetime>(),
            TimeSpan.FromSeconds(10)
        ));
    }

    builder.Services
        .AddDialogportenTelemetry(builder.Configuration, builder.Environment,
            additionalMetrics: x => x.AddAspNetCoreInstrumentation(),
            additionalTracing: x => x
                .AddFusionCacheInstrumentation()
                .AddAspNetCoreInstrumentationExcludingHealthPaths())
        // Options setup
        .AddAspNetCommon(builder.Configuration.GetSection(WebApiSettings.SectionName)
            .GetSection(WebHostCommonSettings.SectionName))
        .ConfigureOptions<AuthorizationOptionsSetup>()
        .Configure<FeatureMetricOptions>(builder.Configuration.GetSection("FeatureMetrics"))

        // Clean architecture projects
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithPubCapabilities()
            .Build()

        // Asp infrastructure
        .AddExceptionHandler<GlobalExceptionHandler>()
        .AddAutoMapper(WebApiAssemblyMarker.Assembly)
        .AddScoped<IUser, ApplicationUser>()
        .AddHttpContextAccessor()
        .AddValidatorsFromAssembly(WebApiAssemblyMarker.Assembly,
            ServiceLifetime.Transient, includeInternalTypes: true)
        .AddAzureAppConfiguration()
        .AddEndpointsApiExplorer()
        .AddFastEndpoints()
        .SwaggerDocument(x =>
        {
            x.MaxEndpointVersion = 1;
            x.ShortSchemaNames = true;
            x.RemoveEmptyRequestSchema = true;
            x.DocumentSettings = s =>
            {
                s.PostProcess = document =>
                {
                    document.Generator = null;
                    document.ReplaceProblemDetailsDescriptions();
                    document.MakeCollectionsNullable();
                    document.FixJwtBearerCasing();
                    document.RemoveSystemStringHeaderTitles();
                    document.AddServiceUnavailableResponse();
                };
                s.Title = "Dialogporten";
                s.Description = Constants.SwaggerSummary.GlobalDescription;
                s.DocumentName = "v1";
                s.Version = "v1";

                // Fix invalid types generated by NSwag for the ContinuationToken and OrderBy parameters
                s.CleanupPaginatedLists();
                s.EnsureJsonPatchConsumes();

                s.SchemaSettings.SchemaNameGenerator = new ShortNameGenerator();

                // Adding ResponseHeaders for PATCH MVC controller
                s.OperationProcessors.Add(new ProducesResponseHeaderOperationProcessor());

                // Adding required scopes to security definitions
                s.OperationProcessors.Add(new SecurityRequirementsOperationProcessor());
            };
        })
        .AddControllers(options => options.InputFormatters.Insert(0, JsonPatchInputFormatter.Get()))
            .AddNewtonsoftJson()
            .Services
        // Add health checks with the retrieved URLs
        .AddAspNetHealthChecks((x, y) => x.HealthCheckSettings.HttpGetEndpointsToCheck = y
            .GetRequiredService<IOptions<WebApiSettings>>().Value?
            .Authentication?
            .JwtBearerTokenSchemas?
            .Select(z => z.WellKnown)
            .ToList() ?? [])
        // Auth
        .AddDialogportenAuthentication(builder.Configuration)
        .AddAuthorization();

    // Built-in ASP.NET Core OpenAPI document generation (alongside existing FastEndpoints/NSwag)
    builder.Services.AddOpenApi("enduser", options =>
    {
        options.ShouldInclude = (description) =>
            description.RelativePath?.Contains("/enduser/", StringComparison.OrdinalIgnoreCase) == true;
        options.AddOperationTransformer((operation, context, _) =>
        {
            var attr = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<OpenApiOperationIdAttribute>().FirstOrDefault();
            if (attr is not null)
                operation.OperationId = attr.OperationId;
            return Task.CompletedTask;
        });
    });
    builder.Services.AddOpenApi("serviceowner", options =>
    {
        options.ShouldInclude = (description) =>
            description.RelativePath?.Contains("/serviceowner/", StringComparison.OrdinalIgnoreCase) == true;
        options.AddOperationTransformer((operation, context, _) =>
        {
            var attr = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<OpenApiOperationIdAttribute>().FirstOrDefault();
            if (attr is not null)
                operation.OperationId = attr.OperationId;
            return Task.CompletedTask;
        });
    });

    if (builder.Environment.IsDevelopment())
    {
        var localDevelopmentSettings = builder.Configuration.GetLocalDevelopmentSettings();
        builder.Services
            .ReplaceSingleton<IUser, LocalDevelopmentUser>(predicate: localDevelopmentSettings.UseLocalDevelopmentUser)
            .ReplaceSingleton<IAuthorizationHandler, AllowAnonymousHandler>(
                predicate: localDevelopmentSettings.DisableAuth)
            .ReplaceSingleton<ITokenIssuerCache, DevelopmentTokenIssuerCache>(
                predicate: localDevelopmentSettings.DisableAuth);
    }

    var app = builder.Build();
    app.MapAspNetHealthChecks()
        .MapControllers();
    app.MapOpenApi().AllowAnonymous();

    app.UseHttpsRedirection()
        .UseDefaultExceptionHandler()
        .UseMaintenanceMode()
        .UseJwtSchemeSelector()
        .UseAuthentication()
        .UseAuthorization()
        .UseServiceOwnerOnBehalfOfPerson()
        .UseUserTypeValidation()
        .UseAzureConfiguration()
        .UseFastEndpoints(x =>
        {
            x.Endpoints.RoutePrefix = "api";
            x.Versioning.Prefix = "v";
            x.Versioning.PrependToRoute = true;
            x.Versioning.DefaultVersion = 1;
            x.Endpoints.Configurator = endpointDefinition =>
            {
                endpointDefinition.Description(routeHandlerBuilder
                    => routeHandlerBuilder.Add(endpointBuilder =>
                    {
                        endpointBuilder.Metadata.Add(
                            new EndpointNameMetadata(
                                TypeNameConverter.ToShortName(endpointDefinition.EndpointType)));

                        var operationIdAttr = endpointDefinition.EndpointType
                            .GetCustomAttribute<OpenApiOperationIdAttribute>();
                        if (operationIdAttr is not null)
                        {
                            endpointBuilder.Metadata.Add(operationIdAttr);
                        }
                    }));
            };
            x.Serializer.Options.RespectNullableAnnotations = true;
            x.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            // Do not serialize empty collections
            x.Serializer.Options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { IgnoreEmptyCollections }
            };
            x.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
            x.Serializer.Options.Converters.Add(new UtcDateTimeOffsetConverter());
            x.Serializer.Options.Converters.Add(new DateTimeNotSupportedConverter());
            x.Errors.ResponseBuilder = ErrorResponseBuilderExtensions.ResponseBuilder;
        })
        .UseAddSwaggerCorsHeader()
        .UseSwaggerGen(config: config =>
        {
            config.PostProcess = (document, _) =>
            {
                var dialogportenBaseUri = builder.Configuration
                    .GetSection(ApplicationSettings.ConfigurationSectionName)
                    .Get<ApplicationSettings>()!
                    .Dialogporten
                    .BaseUri
                    .ToString();

                document.ChangeDialogStatusExample();

                document.Servers.Clear();
                document.Servers.Add(new OpenApiServer
                {
                    Url = dialogportenBaseUri
                });
            };
        }, uiConfig: uiConfig =>
        {
            // Hide schemas view
            uiConfig.DefaultModelsExpandDepth = -1;
            // We have to add dialogporten here to get the correct base url for swagger.json in the APIM. Should not be done for development
            var dialogPrefix = builder.Environment.IsDevelopment() ? "" : "/dialogporten";
            uiConfig.DocumentPath = dialogPrefix + "/swagger/{documentName}/swagger.json";
        })
        .UseFeatureMetrics();

    app.Run();
}

static Dictionary<string, string?> GetOpenApiDocumentGenerationOverrides()
{
    const string altinnExampleUri = "https://altinn.example/";
    const string ed25519PrivateComponent = "ns9Mgams90E5bCNGg9iSXONvRvASFcWF_Nb_JJ8oAEA";
    const string ed25519PublicComponent = "qIn67qFQUBiwW2kv7J-5CdUCdR67CzOSnwXPBunh0d0";
    const string encodedJwk = "eyJrdHkiOiJPS1AiLCJjcnYiOiJFZDI1NTE5Iiwia2lkIjoib3BlbmFwaS1kb2NnZW4tbWFza2lucG9ydGVuIiwieCI6InFJbjY3cUZRVUJpd1cya3Y3Si01Q2RVQ2RSNjdDek9TbndYUEJ1bmgwZDAiLCJkIjoibnM5TWdhbXM5MEU1YkNOR2c5aVNYT052UnZBU0ZjV0ZfTmJfSko4b0FFQSJ9";
    const string infrastructure = InfrastructureSettings.ConfigurationSectionName;
    const string application = ApplicationSettings.ConfigurationSectionName;
    const string webApi = WebApiSettings.SectionName;
    const string dialogporten = nameof(ApplicationSettings.Dialogporten);
    const string ed25519KeyPairs = nameof(DialogportenSettings.Ed25519KeyPairs);
    const string authentication = nameof(WebApiSettings.Authentication);
    const string jwtBearerTokenSchemas = nameof(AuthenticationOptions.JwtBearerTokenSchemas);

    return new Dictionary<string, string?>
    {
        [$"{infrastructure}:{nameof(InfrastructureSettings.DialogDbConnectionString)}"] =
            "Host=localhost;Port=5432;Database=dialogporten;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1;Pooling=false",
        [$"{infrastructure}:{nameof(InfrastructureSettings.Redis)}:{nameof(RedisSettings.ConnectionString)}"] =
            "localhost:6379,abortConnect=false,connectTimeout=1000",
        [$"{infrastructure}:{nameof(InfrastructureSettings.Altinn)}:{nameof(AltinnPlatformSettings.BaseUri)}"] = altinnExampleUri,
        [$"{infrastructure}:{nameof(InfrastructureSettings.Altinn)}:{nameof(AltinnPlatformSettings.EventsBaseUri)}"] = altinnExampleUri,
        [$"{infrastructure}:{nameof(InfrastructureSettings.Altinn)}:{nameof(AltinnPlatformSettings.SubscriptionKey)}"] = "openapi-docgen",
        [$"{infrastructure}:{nameof(InfrastructureSettings.AltinnCdn)}:{nameof(AltinnCdnPlatformSettings.BaseUri)}"] = altinnExampleUri,
        [$"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}:{nameof(MaskinportenSettings.Environment)}"] = "test",
        [$"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}:{nameof(MaskinportenSettings.ClientId)}"] = "openapi-docgen",
        [$"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}:{nameof(MaskinportenSettings.Scope)}"] = "altinn:events.publish",
        [$"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}:{nameof(MaskinportenSettings.EncodedJwk)}"] = encodedJwk,
        [$"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}:{nameof(MaskinportenSettings.TokenExchangeEnvironment)}"] = "at23",
        [$"{infrastructure}:{nameof(InfrastructureSettings.MassTransit)}:{nameof(MassTransitSettings.Host)}"] =
            "Endpoint=sb://localhost/;SharedAccessKeyName=openapi-docgen;SharedAccessKey=openapi-docgen",
        [$"{application}:{dialogporten}:{nameof(DialogportenSettings.BaseUri)}"] = altinnExampleUri,
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Primary)}:{nameof(Ed25519KeyPair.Kid)}"] = "openapi-docgen-primary",
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Primary)}:{nameof(Ed25519KeyPair.PrivateComponent)}"] = ed25519PrivateComponent,
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Primary)}:{nameof(Ed25519KeyPair.PublicComponent)}"] = ed25519PublicComponent,
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Secondary)}:{nameof(Ed25519KeyPair.Kid)}"] = "openapi-docgen-secondary",
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Secondary)}:{nameof(Ed25519KeyPair.PrivateComponent)}"] = ed25519PrivateComponent,
        [$"{application}:{dialogporten}:{ed25519KeyPairs}:{nameof(Ed25519KeyPairs.Secondary)}:{nameof(Ed25519KeyPair.PublicComponent)}"] = ed25519PublicComponent,
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:0:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Maskinporten",
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:0:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri,
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:1:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Altinn",
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:1:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri,
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:2:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Idporten",
        [$"{webApi}:{authentication}:{jwtBearerTokenSchemas}:2:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri
    };
}

static void IgnoreEmptyCollections(JsonTypeInfo typeInfo)
{
    foreach (var property in typeInfo.Properties)
    {
        if (property.PropertyType.IsAssignableTo(typeof(ICollection)))
        {
            property.ShouldSerialize = (_, val) => val is ICollection collection && collection.Count > 0;
        }
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
namespace Digdir.Domain.Dialogporten.WebApi
{
    public sealed partial class Program;
}
