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

        var count = records.Count;
        var miljø = new string[count];
        var tjeneste = new string[count];
        var konsumentorgnr = new string[count];
        var tjenesteeierorgnr = new string[count];
        var transaksjonstype = new string[count];
        var feilet = new string[count];
        var antall = new long[count];
        var relativRessursbruk = new decimal[count];

        for (var i = 0; i < count; i++)
        {
            miljø[i] = records[i].Miljø;
            tjeneste[i] = records[i].Tjeneste;
            konsumentorgnr[i] = records[i].Konsumentorgnr;
            tjenesteeierorgnr[i] = records[i].Tjenesteeierorgnr;
            transaksjonstype[i] = records[i].Transaksjonstype;
            feilet[i] = records[i].Feilet;
            antall[i] = records[i].Antall;
            relativRessursbruk[i] = records[i].RelativRessursbruk;
        }

        var columnData = new Array[] { miljø, tjeneste, konsumentorgnr, tjenesteeierorgnr, transaksjonstype, feilet, antall, relativRessursbruk };

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
