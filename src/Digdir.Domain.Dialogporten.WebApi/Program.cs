using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.WebApi;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;
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
using NSwag.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
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

    builder.WebHost.ConfigureKestrel(kestrelOptions =>
    {
        kestrelOptions.Limits.MaxRequestBodySize = Constants.MaxRequestBodySizeInBytes;
    });

    builder.Configuration
        .AddAzureConfiguration(builder.Environment.EnvironmentName)
        .AddLocalConfiguration(builder.Environment);

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

    var openApiSettings = builder.Configuration
        .GetRequiredSection(DialogportenOpenApiSettings.ConfigurationSectionName)
        .Get<DialogportenOpenApiSettings>()!;

    builder.Services
        .AddDialogportenTelemetry(builder.Configuration, builder.Environment,
            additionalMetrics: x => x.AddAspNetCoreInstrumentation(),
            additionalTracing: x => x
                .AddFusionCacheInstrumentation()
                .AddAspNetCoreInstrumentationExcludingHealthPaths(),
            httpUrlTemplates: DependencyTelemetryUrlTemplates.Defaults)
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
        .AddDialogportenResponseCompression()
        .AddFastEndpoints()
        .SwaggerDocument(x =>
        {
            ConfigureOpenApiV1Document(
                options: x,
                postProcess: document =>
                {

                    document.RemoveDefaultSecurityScheme();
                    document.AddMaskinportenSecurityScheme(openApiSettings, GetWellKnown(x.Services, "Maskinporten"));
                    document.AddIdportenSecurityScheme(openApiSettings, GetWellKnown(x.Services, "Idporten"));
                },
                documentName: "v1",
                title: "Dialogporten"
            );
        })
        .SwaggerDocument(x =>
        {
            ConfigureOpenApiV1Document(
                options: x,
                postProcess: document =>
                {
                    document.RemoveDefaultSecurityScheme();
                    document.AddMaskinportenSecurityScheme(openApiSettings, GetWellKnown(x.Services, "Maskinporten"));
                    document.AddIdportenSecurityScheme(openApiSettings, GetWellKnown(x.Services, "Idporten"));
                },
                documentName: "v1.enduser",
                title: "Dialogporten EndUser",
                audience: "enduser"
            );
        })
        .SwaggerDocument(x =>
        {
            ConfigureOpenApiV1Document(
                options: x,
                postProcess: document =>
                {
                    document.RemoveDefaultSecurityScheme();
                    document.AddMaskinportenSecurityScheme(openApiSettings, GetWellKnown(x.Services, "Maskinporten"));
                },
                documentName: "v1.serviceowner",
                title: "Dialogporten ServiceOwner",
                audience: "serviceowner"
            );
        })
        .AddControllers(options => options.InputFormatters.Insert(0, JsonPatchInputFormatter.Get()))
            .AddNewtonsoftJson()
            .Services
        // Add health checks with configured endpoints and well-known auth metadata endpoints
        .AddAspNetHealthChecks((x, y) =>
        {
            var settings = y.GetRequiredService<IOptions<WebApiSettings>>().Value;
            var altinnBaseUri = y.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Altinn.BaseUri;

            x.HealthCheckSettings.HttpGetEndpointsToCheck = AspNetUtilitiesExtensions.ResolveHttpGetEndpointsToCheck(
                settings.HealthCheckSettings.HttpGetEndpointsToCheck,
                altinnBaseUri,
                settings.Authentication.JwtBearerTokenSchemas.Select(schema => new HttpGetEndpointToCheck
                {
                    Name = schema.Name,
                    Url = schema.WellKnown,
                    HardDependency = false
                }));
        })
        // Auth
        .AddDialogportenAuthentication(builder.Configuration)
        .AddAuthorization();

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

    var dialogPrefix = builder.Environment.IsDevelopment() ? "" : "/dialogporten";

    app.UseStaticFiles();

    app.MapScalarApiReference("/scalar", options =>
    {
        options
            .WithTitle("Dialogporten API")
            // Unlike the Swagger UI, Scalar resolves a relative document URL against the origin plus
            // the base path it auto-detects (window.location.pathname minus the server request path).
            // Behind APIM that base path is already "/dialogporten", so the route pattern must NOT
            // include the prefix here, or it would be applied twice (e.g. /dialogporten/dialogporten/...).
            .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
            .AddDocument("v1", "(Legacy) Dialogporten - EndUser/ServiceOwner combined")
            .AddDocument("v1.enduser", "Dialogporten EndUser")
            .AddDocument("v1.serviceowner", "Dialogporten ServiceOwner", isDefault: true)
            .DisableAgent();

        options.HideTestRequestButton = !openApiSettings.EnableTryItOut;
        options.AddAuthorizationCodeFlow(SecurityRequirementsOperationProcessor.IdportenSecurityScheme,
            authOptions => authOptions
                .WithClientId(openApiSettings.IdportenClientId)
                .WithAuthorizationUrl(openApiSettings.IdportenAuthorizationUrl + "?prompt=login")
        );
    });

    app.UseHttpsRedirection();
    // Wraps the response body before any downstream middleware writes. Must precede
    // UseDefaultExceptionHandler so problem+json error bodies on opted-in endpoints are compressed too.
    app.UseResponseCompression();
    app.UseDefaultExceptionHandler()
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
                if (NonBodyRequestBinder.ShouldUseFor(endpointDefinition))
                {
                    endpointDefinition.RequestBinder(typeof(NonBodyRequestBinder<>));
                }

                endpointDefinition.Description(routeHandlerBuilder =>
                {
                    routeHandlerBuilder.Add(endpointBuilder =>
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
                    });

                    routeHandlerBuilder.AddResponseCompressionHintIfMarked(endpointDefinition.EndpointType);
                });
            };
            x.Serializer.Options.RespectNullableAnnotations = true;
            x.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            x.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
            x.Serializer.Options.Converters.Add(new UtcDateTimeOffsetConverter());
            x.Serializer.Options.Converters.Add(new DateTimeNotSupportedConverter());
            x.Errors.ResponseBuilder = ErrorResponseBuilderExtensions.ResponseBuilder;
        })
        .UseAddSwaggerCorsHeader()
        .UseSwaggerGen(config: config =>
        {
            config.Path = "/swagger/{documentName}/swagger.json";
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
            uiConfig.DocumentPath = dialogPrefix + "/swagger/{documentName}/swagger.json";
            uiConfig.CustomJavaScriptPath = dialogPrefix + "/swagger-oidc-workaround.js";
            uiConfig.OAuth2Client = new OAuth2ClientSettings
            {
                ClientId = openApiSettings.IdportenClientId,
                ClientSecret = null,
                UsePkceWithAuthorizationCodeGrant = true,
                Scopes = { "openid", "profile", "digdir:dialogporten" },
            };
            uiConfig.EnableTryItOut = false; // Don't open try-it-out by default (this does not remove the button)
            uiConfig.AdditionalSettings["SWAGGER_IDPORTEN_LOGOUT_URL"] = openApiSettings.IdportenLogoutUrl;
            if (!openApiSettings.EnableTryItOut)
            {
                uiConfig.AdditionalSettings["supportedSubmitMethods"] = new List<string>();
            }
        })
        .UseFeatureMetrics();

    app.Run();
}


static string GetWellKnown(IServiceProvider services, string name)
{
    var settings = services.GetRequiredService<IOptions<WebApiSettings>>().Value;
    var schema = settings.Authentication.JwtBearerTokenSchemas.Find(x => x.Name == name);

    return schema is null
        ? throw new InvalidOperationException($"Unable to find authentication schema '{name}'")
        : schema.WellKnown;
}


static void ConfigureOpenApiV1Document(
    DocumentOptions options,
    Action<OpenApiDocument> postProcess,
    string documentName,
    string title,
    string? audience = null
)
{
    options.MaxEndpointVersion = 1;
    options.ShortSchemaNames = true;
    options.RemoveEmptyRequestSchema = true;
    options.DocumentSettings = s =>
    {
        s.PostProcess = document =>
        {
            document.Generator = null;
            document.MakeCollectionsNullable();
            document.AddTagDescriptions();
            document.FixJwtBearerCasing();
            document.RemoveSystemStringHeaderTitles();
            document.AddServiceUnavailableResponse();
            document.RemoveUnusedPaginationSchemas();
            document.RemoveRequiredPropertiesFromSchemas();
            postProcess.Invoke(document);
        };
        s.Title = title;
        s.Description = Constants.SwaggerSummary.GlobalDescription;
        s.DocumentName = documentName;
        s.Version = "v1";

        // Fix invalid types generated by NSwag for the ContinuationToken and OrderBy parameters
        s.CleanupPaginatedLists();
        s.EnsureJsonPatchConsumes();

        s.SchemaSettings.SchemaNameGenerator = new ShortNameGenerator(documentName);

        if (audience is not null)
        {
            s.OperationProcessors.Insert(0, new OpenApiAudienceFilterOperationProcessor(audience));
            s.OperationProcessors.Add(new OpenApiOperationIdOverrideProcessor(documentName));
        }

        // Adding ResponseHeaders for PATCH MVC controller
        s.OperationProcessors.Add(new ProducesResponseHeaderOperationProcessor());

        // Adding required scopes to security definitions
        s.OperationProcessors.Add(new SecurityRequirementsOperationProcessor());
    };
}

// ReSharper disable once ClassNeverInstantiated.Global
namespace Digdir.Domain.Dialogporten.WebApi
{
    public sealed partial class Program;
}
