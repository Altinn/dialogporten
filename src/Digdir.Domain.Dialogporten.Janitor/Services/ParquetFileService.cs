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
            new DataField<string>("Miljø"),
            new DataField<string>("Tjeneste"),
            new DataField<string>("Tjenesteeierkode"),
            new DataField<string>("Transaksjonstype"),
            new DataField<string>("Feilet"),
            new DataField<long>("Antall"),
            new DataField<decimal>("RelativRessursbruk")
        );

        using var memoryStream = new MemoryStream();

        // Extract data arrays
        var environments = records.Select(r => r.Miljø).ToArray();
        var services = records.Select(r => r.Tjeneste).ToArray();
        var serviceOwnerCodes = records.Select(r => r.Tjenesteeierkode).ToArray();
        var transactionTypes = records.Select(r => r.Transaksjonstype).ToArray();
        var failedFlags = records.Select(r => r.Feilet).ToArray();
        var counts = records.Select(r => r.Antall).ToArray();
        var relativeResourceUsage = records.Select(r => r.RelativRessursbruk).ToArray();

#pragma warning disable CA2016 // ParquetWriter.CreateAsync doesn't support cancellation tokens
        using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream))
        {
            parquetWriter.CompressionMethod = CompressionMethod.Snappy;
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

    public static string GetFileName(DateOnly targetDate, List<string> environments)
    {
        var envString = string.Join("-", environments);
        return $"Dialogporten_metrics_{envString}_{targetDate:yyyy-MM-dd}.parquet";
    }
}
