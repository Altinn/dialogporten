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

    public async Task<byte[]> GenerateParquetFileAsync(List<AggregatedCostMetricsRecord> records, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Parquet file for {RecordCount} aggregated records", records.Count);

        using var memoryStream = new MemoryStream();

        var recordCount = records.Count;
        var environment = new string[recordCount];
        var admin = new string[recordCount];
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
            admin[i] = records[i].HasAdminScope;
            consumerOrgNumber[i] = records[i].ConsumerOrgNumber;
            ownerOrgNumber[i] = records[i].OwnerOrgNumber;
            transactionType[i] = records[i].TransactionType;
            failed[i] = records[i].Failed;
            count[i] = records[i].Count;
            relativeResourceUsage[i] = records[i].RelativeResourceUsage;
        }

        // Ordering here must match the column data below
        var schema = new ParquetSchema(
            new DataField<string>("Miljø"),
            new DataField<string>("Tjeneste"),
            new DataField<string>("Admin"),
            new DataField<string>("Konsumentorgnr"),
            new DataField<string>("Tjenesteeierorgnr"),
            new DataField<string>("Transaksjonstype"),
            new DataField<string>("Feilet"),
            new DataField<long>("Antall"),
            new DataField<decimal>("RelativRessursbruk")
        );

        // Ordering here must match the above ParquetSchema
        var columnData = new Array[]
        {
            environment,
            service,
            admin,
            consumerOrgNumber,
            ownerOrgNumber,
            transactionType,
            failed,
            count,
            relativeResourceUsage
        };

        using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream, cancellationToken: cancellationToken))
        {
            parquetWriter.CompressionMethod = CompressionMethod.Snappy;
            using var rowGroup = parquetWriter.CreateRowGroup();

            for (var i = 0; i < columnData.Length; i++)
            {
                await rowGroup.WriteColumnAsync(
                    new DataColumn(schema.DataFields[i], columnData[i]), cancellationToken);
            }
        }

        _logger.LogInformation("Generated Parquet file with {FileSize} bytes", memoryStream.Length);
        return memoryStream.ToArray();
    }

    public static string GetFileName(DateOnly targetDate, string environment) =>
        $"Dialogporten_metrics_{environment}_{targetDate:yyyy-MM-dd}.parquet";
}
