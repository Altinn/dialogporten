using System.Globalization;
using System.Reflection;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;
using Digdir.Domain.Dialogporten.GraphQL.Common.Extensions;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using Digdir.Domain.Dialogporten.GraphQL;
using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using FluentValidation;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authorization;

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
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));

    builder.Configuration.AddAzureConfiguration(builder.Environment.EnvironmentName);

    builder.Services
        .AddOptions<GraphQlSettings>()
        .Bind(builder.Configuration.GetSection(GraphQlSettings.SectionName))
        .ValidateFluently()
        .ValidateOnStart();

    if (builder.Environment.IsDevelopment())
    {
        var localDevelopmentSettings = builder.Configuration.GetLocalDevelopmentSettings();
        builder.Services
            .ReplaceSingleton<IUser, LocalDevelopmentUser>(predicate: localDevelopmentSettings.UseLocalDevelopmentUser)
            .ReplaceSingleton<IAuthorizationHandler, AllowAnonymousHandler>(
                predicate: localDevelopmentSettings.DisableAuth);
    }

    var thisAssembly = Assembly.GetExecutingAssembly();

    builder.Services
        // Options setup
        .ConfigureOptions<AuthorizationOptionsSetup>()

        // Clean architecture projects
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)

        .AddAutoMapper(Assembly.GetExecutingAssembly())
        .AddApplicationInsightsTelemetry()
        .AddScoped<IUser, LocalDevelopmentUser>()
        .AddValidatorsFromAssembly(thisAssembly, ServiceLifetime.Transient, includeInternalTypes: true)
        .AddAzureAppConfiguration()

        // Graph QL
        .AddDialogportenGraphQl()

        // Auth
        .AddDialogportenAuthentication(builder.Configuration)
        .AddAuthorization()
        .AddHealthChecks();

    var app = builder.Build();

    app.UseJwtSchemeSelector()
        .UseAuthentication()
        .UseAuthorization()
        .UseAzureConfiguration();

    app.MapGraphQL()
    .RequireAuthorization()
    .WithOptions(new GraphQLServerOptions
    {
        EnableSchemaRequests = true,
        Tool =
        {
            Enable = true
        }
    });

    app.MapHealthChecks("/healthz");

    app.Run();
}
