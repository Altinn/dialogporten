﻿using System.Globalization;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Service;

// TODO: Add AppConfiguration and key vault
// TODO: Configure Service bus connection settings
// TODO: Configure Postgres connection settings
// TODO: Improve exceptions thrown in this assembly

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
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Fatal)
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Conditional(
            condition: _ => builder.Environment.IsDevelopment(),
            configureSink: x => x.Console(formatProvider: CultureInfo.InvariantCulture))
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));

    builder.Configuration.AddLocalConfiguration(builder.Environment);

    builder.Services
        .AddApplicationInsightsTelemetry()
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithPubSubCapabilities<ServiceAssemblyMarker>()
            .Build()
        .AddTransient<IUser, ServiceUser>()
        .AddHealthChecks();

    var app = builder.Build();
    app.UseHttpsRedirection();
    app.MapHealthChecks("/healthz");
    app.Run();
}
