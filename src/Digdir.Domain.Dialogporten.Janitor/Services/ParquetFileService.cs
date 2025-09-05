using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Parquet.Serialization;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class ParquetFileService
{
    private readonly ILogger<ParquetFileService> _logger;

    public ParquetFileService(ILogger<ParquetFileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> GenerateParquetFileAsync(List<AggregatedMetricsRecord> records, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Parquet file for {RecordCount} aggregated records", records.Count);

        var schema = new ParquetSchema(
            new DataField<string>("Environment"),
            new DataField<string>("Service"),
            new DataField<string>("ServiceOwnerCode"),
            new DataField<string>("TransactionType"),
            new DataField<string>("Failed"),
            new DataField<long>("Count"),
            new DataField<decimal>("RelativeResourceUsage")
        );

        using var memoryStream = new MemoryStream();

        // Extract data arrays
        var environments = records.Select(r => r.Environment).ToArray();
        var services = records.Select(r => r.Service).ToArray();
        var serviceOwnerCodes = records.Select(r => r.ServiceOwnerCode).ToArray();
        var transactionTypes = records.Select(r => r.TransactionType).ToArray();
        var failedFlags = records.Select(r => r.Failed).ToArray();
        var counts = records.Select(r => r.Count).ToArray();
        var relativeResourceUsage = records.Select(r => r.RelativeResourceUsage).ToArray();

#pragma warning disable CA2016 // ParquetWriter.CreateAsync doesn't support cancellation tokens
        using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream))
        {
            using var rowGroup = parquetWriter.CreateRowGroup();

            // Write columns
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[0], environments), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[1], services), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[2], serviceOwnerCodes), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[3], transactionTypes), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[4], failedFlags), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[5], counts), cancellationToken);
            await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[6], relativeResourceUsage), cancellationToken);
        }
#pragma warning restore CA2016

        _logger.LogInformation("Generated Parquet file with {FileSize} bytes", memoryStream.Length);
        return memoryStream.ToArray();
    }

    public static string GetFileName(DateOnly targetDate)
    {
        return $"Dialogporten_metrics_{targetDate:yyyy-MM-dd}.parquet";
    }
}
