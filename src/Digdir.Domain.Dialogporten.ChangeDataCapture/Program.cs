﻿using System.Globalization;
using Digdir.Domain.Dialogporten.ChangeDataCapture;
using Digdir.Domain.Dialogporten.ChangeDataCapture.ChangeDataCapture.ReplicationMapper;
using Digdir.Domain.Dialogporten.ChangeDataCapture.ChangeDataCapture.Snapshot;
using Digdir.Domain.Dialogporten.ChangeDataCapture.ChangeDataCapture.Subscription;
using Digdir.Domain.Dialogporten.ChangeDataCapture.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Outboxes;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Replication;
using Serilog;

// TODO: Configure Azure Service Bus connection settings and endpoint exchange
// TODO: Configure Postgres connection settings
// TODO: Improve exceptions thrown in this assembly

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
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Conditional(
            condition: _ => builder.Environment.IsDevelopment(),
            configureSink: x => x.Console(formatProvider: CultureInfo.InvariantCulture))
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));

    builder.Configuration.AddAzureConfiguration(builder.Environment.EnvironmentName);

    builder.Services
        .AddAzureAppConfiguration()
        .AddApplicationInsightsTelemetry()
        .AddHostedService<CdcBackgroundHandler>()
        .AddMassTransit(x =>
        {
            var useInMemoryTransport = builder.Configuration.GetValue<bool>("MassTransit:UseInMemoryTransport");

            if (useInMemoryTransport)
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // todo: Configure for using Azure Service Bus
            }
        })
        .AddOptions<PostgresOutboxCdcSSubscriptionOptions>()
            .BindConfiguration(PostgresOutboxCdcSSubscriptionOptions.SectionName)
            .Configure<IConfiguration>((option, conf) =>
                option = option.ConnectionString is null
                    ? option with { ConnectionString = conf["Infrastructure:DialogDbConnectionString"]! }
                    : option)
            .Services
        .AddSingleton(x => NpgsqlDataSource.Create(x.GetRequiredService<IOptions<PostgresOutboxCdcSSubscriptionOptions>>().Value.ConnectionString))
        .AddTransient<ISnapshotCheckpointRepository, SnapshotCheckpointRepository>()
        .AddTransient(typeof(IReplicationDataMapper<>), typeof(DynamicReplicationDataMapper<>))
        .AddTransient<ICdcSubscription<OutboxMessage>, PostgresOutboxCdcSubscription>()
        .AddTransient<ICdcSink<OutboxMessage>, MassTransitSink>()
        .AddHealthChecks();

    var app = builder.Build();

    app.UseHttpsRedirection()
        .UseSerilogRequestLogging();

    app.UseHealthChecks("/healthz");
    app.Run();
}
