using System.Globalization;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.GraphQL;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Library.Utils.AspNet;
using FluentValidation;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

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

    builder.Configuration
        .AddAzureConfiguration(builder.Environment.EnvironmentName)
        .AddLocalConfiguration(builder.Environment);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Warning()
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithEnvironmentName()
        .Enrich.FromLogContext()
        .WriteTo.OpenTelemetryOrConsole(context));

    builder.Services
        .AddOptions<GraphQlSettings>()
        .Bind(builder.Configuration.GetSection(GraphQlSettings.SectionName))
        .ValidateFluently()
        .ValidateOnStart();

    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IHostLifetime>(sp => new DelayedShutdownHostLifetime(
            sp.GetRequiredService<IHostApplicationLifetime>(),
            TimeSpan.FromSeconds(10)
        ));
    }

    var thisAssembly = Assembly.GetExecutingAssembly();

    // CORS allowed origins by environment in order for GraphQL streams to work from Arbeidsflate directly through APIM
    var allowedOrigins = builder.Configuration
        .GetSection(GraphQlSettings.SectionName)
        .Get<GraphQlSettings>()
        ?.Cors.AllowedOrigins.ToArray() ?? [];

    builder.Services
        // Options setup
        .AddAspNetCommon(builder.Configuration.GetSection(GraphQlSettings.SectionName)
            .GetSection(WebHostCommonSettings.SectionName))
        .ConfigureOptions<AuthorizationOptionsSetup>()

        // Clean architecture projects
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithPubCapabilities()
            .Build()
        .AddAutoMapper(Assembly.GetExecutingAssembly())
        .AddHttpContextAccessor()
        .AddScoped<IUser, ApplicationUser>()
        .AddValidatorsFromAssembly(thisAssembly, ServiceLifetime.Transient, includeInternalTypes: true)
        .AddAzureAppConfiguration()

        // CORS
        .AddCors(options =>
        {
            options.AddPolicy(GraphQlCorsOptions.PolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        })

        // Graph QL
        .AddDialogportenGraphQl()

        // Add controllers
        .AddControllers()
            .Services

        // Telemetry
        .AddDialogportenTelemetry(builder.Configuration, builder.Environment,
            additionalMetrics: x => x.AddAspNetCoreInstrumentation(),
            additionalTracing: x => x
                .AddSource("Dialogporten.GraphQL")
                .AddFusionCacheInstrumentation()
                .AddHotChocolateInstrumentation()
                .AddAspNetCoreInstrumentationExcludingHealthPaths(o =>
                {
                    o.EnrichWithHttpResponse = (activity, _) =>
                    {
                        DialogportenGqlActivityEnricher.RenameOperationName(activity);
                    };
                }))

        // Add health checks with the well-known URLs
        .AddAspNetHealthChecks((x, y) => x.HealthCheckSettings.HttpGetEndpointsToCheck = y
            .GetRequiredService<IOptions<GraphQlSettings>>().Value?
            .Authentication?
            .JwtBearerTokenSchemas?
            .Select(z => z.WellKnown)
            .ToList() ?? [])

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
                predicate: localDevelopmentSettings.DisableAuth);
    }

    var app = builder.Build();

    app.UseCors();
    app.MapAspNetHealthChecks()
        .UseMaintenanceMode()
        .UseJwtSchemeSelector()
        .UseAuthentication()
        .UseAuthorization()
        .UseAzureConfiguration();

    app.MapGraphQL()
        .RequireCors(GraphQlCorsOptions.PolicyName)
        .RequireAuthorization()
        .WithOptions(new GraphQLServerOptions
        {
            EnableSchemaRequests = true,
            Tool =
            {
                Enable = true
            }
        });

    app.Run();
}
