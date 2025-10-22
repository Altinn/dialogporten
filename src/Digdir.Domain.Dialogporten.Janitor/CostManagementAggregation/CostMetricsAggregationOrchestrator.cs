using Digdir.Domain.Dialogporten.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostMetricsAggregationOrchestrator
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<CostMetricsAggregationOrchestrator> _logger;
    private readonly ApplicationInsightsService _applicationInsightsService;
    private readonly MetricsAggregationService _aggregationService;
    private readonly ParquetFileService _parquetService;
    private readonly AzureStorageService _storageService;

    public CostMetricsAggregationOrchestrator(
        IConfiguration config,
        IHostEnvironment hostEnvironment,
        ILogger<CostMetricsAggregationOrchestrator> logger,
        ApplicationInsightsService applicationInsightsService,
        MetricsAggregationService aggregationService,
        ParquetFileService parquetService,
        AzureStorageService storageService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationInsightsService = applicationInsightsService ?? throw new ArgumentNullException(nameof(applicationInsightsService));
        _aggregationService = aggregationService ?? throw new ArgumentNullException(nameof(aggregationService));
        _parquetService = parquetService ?? throw new ArgumentNullException(nameof(parquetService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    public async Task<AggregationResult> AggregateCostMetricsForDateOnlyAsync(
        DateOnly targetDate,
        bool skipUpload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting metrics aggregation for date {Date:dd.MM.yyyy}", targetDate);

        try
        {
            var (startTime, endTime) = NorwegianTimeConverter.GetDayRangeInUtc(targetDate);

            var allRecords = await _applicationInsightsService.QueryFeatureMetricsAsync(startTime, endTime, _hostEnvironment.EnvironmentName, cancellationToken);
            var aggregatedRecords = _aggregationService.AggregateFeatureMetrics(allRecords);
            var parquetData = await _parquetService.GenerateParquetFileAsync(aggregatedRecords, cancellationToken);
            var fileName = GetDateOnlyFileName(targetDate, _hostEnvironment.EnvironmentName);

            var localDevSettings = _config.GetLocalDevelopmentSettings();

            if (skipUpload || (_hostEnvironment.IsDevelopment() && localDevSettings.UseLocalMetricsAggregationStorage))
            {
                await SaveLocallyAsync(parquetData, fileName, aggregatedRecords.Count, cancellationToken);
            }
            else
            {
                await _storageService.UploadParquetFileAsync(parquetData, fileName, cancellationToken);
            }

            return new AggregationResult.Success(aggregatedRecords.Count, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate metrics for date {Date}", targetDate);
            return new AggregationResult.Failure(ex.Message);
        }
    }

    private async Task SaveLocallyAsync(byte[] parquetData, string fileName, int recordCount, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        await File.WriteAllBytesAsync(outputPath, parquetData, cancellationToken);
        _logger.LogInformation("Saved parquet file to {FilePath} with {RecordCount} records ({FileSize} bytes)",
            outputPath, recordCount, parquetData.Length);
    }

    public static string GetDateOnlyFileName(DateOnly date, string environment) =>
        $"Dialogporten_metrics_{environment}_{date:yyyy-MM-dd}.parquet";
}

public abstract record AggregationResult
{
    public sealed record Success(int RecordCount, string FileName) : AggregationResult;
    public sealed record Failure(string ErrorMessage) : AggregationResult;

    public int Match(Func<Success, int> onSuccess, Func<Failure, int> onFailure) =>
        this switch
        {
            Success success => onSuccess(success),
            Failure failure => onFailure(failure),
            _ => onFailure(new Failure("Unknown result type"))
        };
}
