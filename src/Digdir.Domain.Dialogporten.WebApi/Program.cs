using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.WebApi;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Library.Utils.AspNet;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using Serilog;
using Microsoft.Extensions.Options;

// Using two-stage initialization to catch startup errors.
var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.WithEnvironmentName()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
    .CreateBootstrapLogger();

try
{
    BuildAndRun(args, telemetryConfiguration);
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

static void BuildAndRun(string[] args, TelemetryConfiguration telemetryConfiguration)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(kestrelOptions =>
    {
        kestrelOptions.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
    });

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Warning()
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithEnvironmentName()
        .Enrich.FromLogContext()
        .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces));

    builder.Configuration
        .AddAzureConfiguration(builder.Environment.EnvironmentName)
        .AddLocalConfiguration(builder.Environment);

    builder.Services
        .AddOptions<WebApiSettings>()
        .Bind(builder.Configuration.GetSection(WebApiSettings.SectionName))
        .ValidateFluently()
        .ValidateOnStart();

    var thisAssembly = Assembly.GetExecutingAssembly();

    builder.ConfigureTelemetry();

    builder.Services
        // Options setup
        .ConfigureOptions<AuthorizationOptionsSetup>()

        // Clean architecture projects
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithPubCapabilities()
            .Build()

        // Asp infrastructure
        .AddExceptionHandler<GlobalExceptionHandler>()
        .AddAutoMapper(Assembly.GetExecutingAssembly())
        .AddScoped<IUser, ApplicationUser>()
        .AddHttpContextAccessor()
        .AddValidatorsFromAssembly(thisAssembly, ServiceLifetime.Transient, includeInternalTypes: true)
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
                s.Title = "Dialogporten";
                s.DocumentName = "v1";
                s.Version = "v1";

                // Fix invalid types generated by NSwag for the ContinuationToken and OrderBy parameters
                s.CleanupPaginatedLists();

                // We get duplicate names for some schemas that have the same name but live within enduser or serviceowner
                // namespaces. We do not want to use the full namespace path in swagger, but also do not want the
                // generic "2" suffix duplicate names get, so we add a "SO" suffix to the serviceowner specific schemas.
                // This should match the operationIds used for service owners.
                s.AddServiceOwnerSuffixToSchemas();
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

    app.UseHttpsRedirection()
        .UseSerilogRequestLogging()
        .UseDefaultExceptionHandler()
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
                    => routeHandlerBuilder.Add(endpointBuilder
                        => endpointBuilder.Metadata.Add(
                            new EndpointNameMetadata(
                                TypeNameConverter.ToShortName(endpointDefinition.EndpointType)))));
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
        .UseSwaggerGen(config =>
        {
            config.PostProcess = (document, _) =>
            {
                var dialogportenBaseUri = builder.Configuration
                    .GetSection(ApplicationSettings.ConfigurationSectionName)
                    .Get<ApplicationSettings>()!
                    .Dialogporten
                    .BaseUri
                    .ToString();

                document.Servers.Clear();
                document.Servers.Add(new OpenApiServer { Url = dialogportenBaseUri });
                document.Generator = null;
                document.ReplaceProblemDetailsDescriptions();
                document.MakeCollectionsNullable();
                document.FixJwtBearerCasing();
            };
        }, uiConfig =>
        {
            // Hide schemas view
            uiConfig.DefaultModelsExpandDepth = -1;
            // We have to add dialogporten here to get the correct base url for swagger.json in the APIM. Should not be done for development
            var dialogPrefix = builder.Environment.IsDevelopment() ? "" : "/dialogporten";
            uiConfig.DocumentPath = dialogPrefix + "/swagger/{documentName}/swagger.json";
        });

    app.Run();
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
public sealed partial class Program;
