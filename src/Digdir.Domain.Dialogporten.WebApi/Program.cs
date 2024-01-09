using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Infrastructure.DomainEvents.Outbox.Dispatcher;
using Digdir.Domain.Dialogporten.WebApi.Common;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Collections;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using FluentValidation;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using NSwag;

// Using two-stage initialization to catch startup errors.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.ApplicationInsights(
        TelemetryConfiguration.CreateDefault(),
        TelemetryConverter.Traces)
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

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Warning()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Fatal)
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));

    builder.Configuration.AddAzureConfiguration(builder.Environment.EnvironmentName);

    builder.Services
        .AddOptions<WebApiSettings>()
        .Bind(builder.Configuration.GetSection(WebApiSettings.SectionName))
        .ValidateFluently()
        .ValidateOnStart();

    if (!builder.Environment.IsDevelopment())
    {
        // Temporary configuration for outbox through Web api
        var shouldUseOutbox = builder.Configuration.GetValue("RUN_OUTBOX_SCHEDULER", false);
        if (shouldUseOutbox)
        {
            builder.Services.AddHostedService<OutboxScheduler>();
        }
    }

    var thisAssembly = Assembly.GetExecutingAssembly();

    builder.Services
        // Options setup
        .ConfigureOptions<AuthorizationOptionsSetup>()

        // Clean architecture projects
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)

        // Asp infrastructure
        .AddAutoMapper(Assembly.GetExecutingAssembly())
        .AddScoped<IUser, ApplicationUser>()
        .AddHttpContextAccessor()
        .AddValidatorsFromAssembly(thisAssembly, ServiceLifetime.Transient, includeInternalTypes: true)
        .AddAzureAppConfiguration()
        .AddApplicationInsightsTelemetry()
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
                s.DocumentName = "V0.1";
                s.Version = "v0.1";

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

        // Auth
        .AddDialogportenAuthentication(builder.Configuration)
        .AddAuthorization()
        .AddHealthChecks();

    if (builder.Environment.IsDevelopment())
    {
        var localDevelopmentSettings = builder.Configuration.GetLocalDevelopmentSettings();
        builder.Services
            .ReplaceSingleton<IUser, LocalDevelopmentUser>(predicate: localDevelopmentSettings.UseLocalDevelopmentUser)
            .ReplaceSingleton<IAuthorizationHandler, AllowAnonymousHandler>(
                predicate: localDevelopmentSettings.DisableAuth)
            .AddHostedService<
                OutboxScheduler>(predicate: !localDevelopmentSettings.DisableShortCircuitOutboxDispatcher);
    }

    var app = builder.Build();

    app.UseHttpsRedirection()
        .UseSerilogRequestLogging()
        .UseProblemDetailsExceptionHandler()
        .UseJwtSchemeSelector()
        .UseAuthentication()
        .UseAuthorization()
        .UseAzureConfiguration()
        .UseFastEndpoints(x =>
        {
            x.Endpoints.RoutePrefix = "api";
            x.Versioning.Prefix = "v";
            x.Versioning.PrependToRoute = true;
            x.Versioning.DefaultVersion = 1;
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
            };
        }, uiConfig =>
        {
            // Hide schemas view
            uiConfig.DefaultModelsExpandDepth = -1;
        });
    app.MapControllers();
    app.MapHealthChecks("/healthz");
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
