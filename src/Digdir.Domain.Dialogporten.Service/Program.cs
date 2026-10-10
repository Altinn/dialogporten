using System.Globalization;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Service;
using Digdir.Domain.Dialogporten.Service.Common;
using Digdir.Library.Utils.AspNet;
using MassTransit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OpenTelemetry.Metrics;
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
        .Filter.WithHandledPostgresExceptionFilter()
        .WriteTo.OpenTelemetryOrConsole(context));

    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IHostLifetime>(sp => new DelayedShutdownHostLifetime(
            sp.GetRequiredService<IHostApplicationLifetime>(),
            TimeSpan.FromSeconds(10)
        ));
    }

    builder.Services
        .AddDialogportenTelemetry(builder.Configuration, builder.Environment,
            additionalMetrics: x => x
                .AddAspNetCoreInstrumentation()
                .AddNpgsqlInstrumentation(),
            additionalTracing: x =>
            {
                x.AddAspNetCoreInstrumentationExcludingHealthPaths();
            },
            httpUrlTemplates: DependencyTelemetryUrlTemplates.Defaults)
        .AddAzureAppConfiguration()
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithPubSubCapabilities<ServiceAssemblyMarker>()
            .AndBusConfiguration(x =>
            {
                foreach (var map in MassTransitApplicationUtils.GetApplicationConsumerMaps())
                {
                    x.TryAddTransient(map.AppConsumerType);
                    x.AddConsumer(map.BusConsumerType, map.BusDefinitionType)
                        .Endpoint(x => x.Name = map.EndpointName);
                }
            })
            .Build()
        .AddTransient<IUser, ServiceUser>()
        // Top-level "HealthProbes", unlike WebApi/GraphQL which nest it under their own section.
        // No Service appsettings file defines it, but Azure App Configuration can inject it at
        // runtime, so the key is registered regardless.
        .AddDialogportenHealthChecks(builder.Configuration, DialogportenHealthCheckExtensions.ProbeSectionName);

    var app = builder.Build();
    app.MapDialogportenHealthChecks();
    app.UseHttpsRedirection()
        .UseAzureConfiguration();
    app.Run();
}
