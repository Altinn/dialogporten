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
using Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;
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
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddJsonFile("CostManagementAggregation/cost-coefficients.json", optional: false, reloadOnChange: true)
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
        .ValidateDataAnnotations();

    // Add cost coefficients configuration
    builder.Services
        .AddOptions<CostCoefficientsOptions>()
        .Bind(builder.Configuration.GetSection(CostCoefficientsOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<IValidateOptions<CostCoefficientsOptions>, CostCoefficientsOptionsValidator>();

    builder.Services.AddSingleton(_ =>
    {
        Azure.Core.TokenCredential credential = builder.Environment.IsDevelopment()
            ? new DefaultAzureCredential() // Tries multiple methods for local dev
            : new ManagedIdentityCredential(); // Use managed identity in Azure
        return new LogsQueryClient(credential);
    });

    builder.Services.AddSingleton(provider =>
    {
        var options = provider.GetRequiredService<IOptions<MetricsAggregationOptions>>().Value;
        var configuration = provider.GetRequiredService<IConfiguration>();

        // Use configured connection string if available
        if (!string.IsNullOrEmpty(options.StorageConnectionString))
        {
            return new BlobServiceClient(options.StorageConnectionString);
        }

        // Check for environment variable override for local development
        var localStorageConnectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(localStorageConnectionString))
        {
            return new BlobServiceClient(localStorageConnectionString);
        }

        // Fallback to development storage (Azurite)
        return new BlobServiceClient("UseDevelopmentStorage=true");
    });

    builder.Services.AddSingleton<ApplicationInsightsService>();
    builder.Services.AddSingleton<CostCoefficients>();
    builder.Services.AddSingleton<MetricsAggregationService>();
    builder.Services.AddSingleton<ParquetFileService>();
    builder.Services.AddSingleton<AzureStorageService>();
    builder.Services.AddSingleton<CostMetricsAggregationOrchestrator>();

    var app = builder.Build();

    app.AddJanitorCommands();

    app.Run();
}
