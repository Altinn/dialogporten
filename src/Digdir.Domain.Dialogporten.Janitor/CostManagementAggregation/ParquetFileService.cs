using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class ParquetFileService
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
            new DataField<string>("Konsumentorgnr"),
            new DataField<string>("Tjenesteeierorgnr"),
            new DataField<string>("Transaksjonstype"),
            new DataField<string>("Feilet"),
            new DataField<long>("Antall"),
            new DataField<decimal>("RelativRessursbruk")
        );

        using var memoryStream = new MemoryStream();

        var recordCount = records.Count;
        var environment = new string[recordCount];
        var service = new string[recordCount];
        var consumerOrgNumber = new string[recordCount];
        var ownerOrgNumber = new string[recordCount];
        var transactionType = new string[recordCount];
        var failed = new string[recordCount];
        var count = new long[recordCount];
        var relativeResourceUsage = new decimal[recordCount];

        for (var i = 0; i < recordCount; i++)
        {
            environment[i] = records[i].Environment;
            service[i] = records[i].Service;
            consumerOrgNumber[i] = records[i].ConsumerOrgNumber;
            ownerOrgNumber[i] = records[i].OwnerOrgNumber;
            transactionType[i] = records[i].TransactionType;
            failed[i] = records[i].Failed;
            count[i] = records[i].Count;
            relativeResourceUsage[i] = records[i].RelativeResourceUsage;
        }

        var columnData = new Array[] { environment, service, consumerOrgNumber, ownerOrgNumber, transactionType, failed, count, relativeResourceUsage };

#pragma warning disable CA2016 // ParquetWriter.CreateAsync doesn't support cancellation tokens
        using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream))
        {
            parquetWriter.CompressionMethod = CompressionMethod.Snappy;
            using var rowGroup = parquetWriter.CreateRowGroup();

            for (var i = 0; i < columnData.Length; i++)
            {
                await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[i], columnData[i]), cancellationToken);
            }
        }
#pragma warning restore CA2016

        _logger.LogInformation("Generated Parquet file with {FileSize} bytes", memoryStream.Length);
        return memoryStream.ToArray();
    }

    public static string GetFileName(DateOnly targetDate, List<string>? environments = null)
    {
        var envString = environments != null && environments.Count > 0
            ? string.Join("-", environments) + "_"
            : string.Empty;
        return $"Dialogporten_metrics_{envString}{targetDate:yyyy-MM-dd}.parquet";
    }
}
