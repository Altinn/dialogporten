using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostMetricsAggregationOrchestrator
{
    private readonly ILogger<CostMetricsAggregationOrchestrator> _logger;
    private readonly ApplicationInsightsService _applicationInsightsService;
    private readonly MetricsAggregationService _aggregationService;
    private readonly ParquetFileService _parquetService;
    private readonly AzureStorageService _storageService;

    public CostMetricsAggregationOrchestrator(
        ILogger<CostMetricsAggregationOrchestrator> logger,
        ApplicationInsightsService applicationInsightsService,
        MetricsAggregationService aggregationService,
        ParquetFileService parquetService,
        AzureStorageService storageService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationInsightsService = applicationInsightsService ?? throw new ArgumentNullException(nameof(applicationInsightsService));
        _aggregationService = aggregationService ?? throw new ArgumentNullException(nameof(aggregationService));
        _parquetService = parquetService ?? throw new ArgumentNullException(nameof(parquetService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    public async Task<AggregationResult> AggregateMetricsAsync(
        DateOnly targetDate,
        List<string> environments,
        bool skipUpload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting metrics aggregation for date {Date:dd.MM.yyyy} with environments: [{Environments}]",
            targetDate, string.Join(", ", environments));

        try
        {
            var allRecords = await CollectMetricsFromSpecifiedEnvironmentsAsync(environments, targetDate, cancellationToken);
            var aggregatedRecords = _aggregationService.AggregateFeatureMetrics(allRecords);
            var parquetData = await _parquetService.GenerateParquetFileAsync(aggregatedRecords, cancellationToken);
            var fileName = ParquetFileService.GetFileName(targetDate, environments);

            if (skipUpload)
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

    private async Task<List<FeatureMetricRecord>> CollectMetricsFromSpecifiedEnvironmentsAsync(
        List<string> environments,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var allRecords = new List<FeatureMetricRecord>();

        foreach (var env in environments)
        {
            _logger.LogInformation("Querying feature metrics from Application Insights for environment {Environment}", env);
            var records = await _applicationInsightsService.QueryFeatureMetricsAsync(targetDate, env, cancellationToken);
            allRecords.AddRange(records);
        }

        return allRecords;
    }

    private async Task SaveLocallyAsync(byte[] parquetData, string fileName, int recordCount, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        await File.WriteAllBytesAsync(outputPath, parquetData, cancellationToken);
        _logger.LogInformation("Saved parquet file to {FilePath} with {RecordCount} records ({FileSize} bytes)",
            outputPath, recordCount, parquetData.Length);
    }
}

public abstract record AggregationResult
{
    public sealed record Success(int RecordCount, string FileName) : AggregationResult;
    public sealed record Failure(string ErrorMessage) : AggregationResult;

    public int Match(Func<Success, int> onSuccess, Func<Failure, int> onFailure)
    {
        return this switch
        {
            Success success => onSuccess(success),
            Failure failure => onFailure(failure),
            _ => onFailure(new Failure("Unknown result type"))
        };
    }
}
