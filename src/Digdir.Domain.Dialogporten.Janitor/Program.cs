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

    builder.Services.AddSingleton(_ => new LogsQueryClient(new DefaultAzureCredential()));

    builder.Services.AddSingleton(provider =>
    {
        var options = provider.GetRequiredService<IOptions<MetricsAggregationOptions>>().Value;
        var environment = provider.GetRequiredService<IHostEnvironment>();

        var localDevelopmentSettings = builder.Configuration.GetLocalDevelopmentSettings();

        if (environment.IsDevelopment() && localDevelopmentSettings.UseLocalMetricsAggregationStorage)
        {
            return new BlobServiceClient("UseDevelopmentStorage=true");
        }

        if (string.IsNullOrEmpty(options.StorageAccountName))
        {
            throw new InvalidOperationException(
                $"{MetricsAggregationOptions.SectionName}:{nameof(MetricsAggregationOptions.StorageAccountName)}" +
                $" must be configured in non-development environments.");
        }

        if (string.IsNullOrEmpty(options.StorageContainerName))
        {
            throw new InvalidOperationException(
                $"{MetricsAggregationOptions.SectionName}:{nameof(MetricsAggregationOptions.StorageContainerName)}" +
                $" must be configured in non-development environments.");
        }

        if (string.IsNullOrEmpty(options.SubscriptionId))
        {
            throw new InvalidOperationException(
                $"{MetricsAggregationOptions.SectionName}:{nameof(MetricsAggregationOptions.SubscriptionId)}" +
                $" must be configured in non-development environments.");
        }

        var credential = new DefaultAzureCredential();
        var storageUri = new Uri($"https://{options.StorageAccountName}.blob.core.windows.net");
        return new BlobServiceClient(storageUri, credential);
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
