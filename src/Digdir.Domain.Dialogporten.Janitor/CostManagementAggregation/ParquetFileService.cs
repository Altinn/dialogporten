using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Schema;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed partial class ParquetFileService
{
    // Used by source-generated logging partials; analyzers don't see the generated usage.
#pragma warning disable IDE0052
    private readonly ILogger<ParquetFileService> _logger;
#pragma warning restore IDE0052

    public ParquetFileService(ILogger<ParquetFileService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public async Task<byte[]> GenerateParquetFileAsync(List<AggregatedCostMetricsRecord> records, CancellationToken cancellationToken = default)
    {
        LogGeneratingParquetFile(records.Count);

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

        var environmentField = new DataField<string>("Miljø");
        var serviceField = new DataField<string>("Tjeneste");
        var adminField = new DataField<string>("Admin");
        var consumerOrgNumberField = new DataField<string>("Konsumentorgnr");
        var ownerOrgNumberField = new DataField<string>("Tjenesteeierorgnr");
        var transactionTypeField = new DataField<string>("Transaksjonstype");
        var failedField = new DataField<string>("Feilet");
        var countField = new DataField<long>("Antall");
        var relativeResourceUsageField = new DataField<decimal>("RelativRessursbruk");

        var schema = new ParquetSchema(
            environmentField,
            serviceField,
            adminField,
            consumerOrgNumberField,
            ownerOrgNumberField,
            transactionTypeField,
            failedField,
            countField,
            relativeResourceUsageField);

        var options = new ParquetOptions { CompressionMethod = CompressionMethod.Snappy };

        await using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream, options, cancellationToken: cancellationToken))
        {
            using var rowGroup = parquetWriter.CreateRowGroup();

            await rowGroup.WriteAsync(environmentField, environment);
            await rowGroup.WriteAsync(serviceField, service);
            await rowGroup.WriteAsync(adminField, admin);
            await rowGroup.WriteAsync(consumerOrgNumberField, consumerOrgNumber);
            await rowGroup.WriteAsync(ownerOrgNumberField, ownerOrgNumber);
            await rowGroup.WriteAsync(transactionTypeField, transactionType);
            await rowGroup.WriteAsync(failedField, failed);
            await rowGroup.WriteAsync<long>(countField, count, cancellationToken: cancellationToken);
            await rowGroup.WriteAsync<decimal>(relativeResourceUsageField, relativeResourceUsage, cancellationToken: cancellationToken);
        }

        LogGeneratedParquetFile(memoryStream.Length);
        return memoryStream.ToArray();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Generating Parquet file for {RecordCount} aggregated records")]
    private partial void LogGeneratingParquetFile(int recordCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated Parquet file with {FileSize} bytes")]
    private partial void LogGeneratedParquetFile(long fileSize);
}
