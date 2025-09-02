using System.Globalization;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Storage.Blobs;
using Cocona;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Janitor;
using Digdir.Domain.Dialogporten.Janitor.Services;
using Digdir.Library.Utils.AspNet;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    var builder = CoconaApp.CreateBuilder(args);

    // Disable scope validation because Cocona does not create a scope for the commands.
    // This makes sense because console applications are short-lived, and the scope of
    // a command is the scope of the application.
    builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = false);

    builder.Configuration
        .AddUserSecrets<Program>()
        .AddLocalConfiguration(builder.Environment);
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .WriteTo.OpenTelemetryOrConsole(context));

    builder.Services
        .AddDialogportenTelemetry(builder.Configuration, builder.Environment,
            additionalTracing: x => x.AddFusionCacheInstrumentation())
        .AddApplication(builder.Configuration, builder.Environment)
        .AddInfrastructure(builder.Configuration, builder.Environment)
            .WithoutPubSubCapabilities()
            .Build()
        .AddScoped<IUser, ConsoleUser>()
        .AddSingleton(TelemetryConfiguration.CreateDefault());

    // Add metrics aggregation services
    builder.Services
        .AddOptions<MetricsAggregationOptions>()
        .Bind(builder.Configuration.GetSection(MetricsAggregationOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton(provider =>
    {
        var credential = new DefaultAzureCredential();
        return new LogsQueryClient(credential);
    });

    builder.Services.AddSingleton(provider =>
    {
        var options = provider.GetRequiredService<IOptions<MetricsAggregationOptions>>().Value;
        return new BlobServiceClient(options.StorageConnectionString);
    });

    builder.Services.AddSingleton<AzureMonitorService>();
    builder.Services.AddSingleton<MetricsAggregationService>();
    builder.Services.AddSingleton<ParquetFileService>();
    builder.Services.AddSingleton<AzureStorageService>();

    var app = builder.Build();

    app.AddJanitorCommands();

    app.Run();
}
