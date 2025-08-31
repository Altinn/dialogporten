using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
internal sealed class PostgresCopyWriter<T> : IAsyncDisposable, IDisposable where T : class
{
    public static readonly string?[] Headers;
    private static readonly string CopyCommand;

    private readonly NpgsqlConnection _connection;
    private readonly CsvWriter _csvWriter;

    private PostgresCopyWriter(NpgsqlConnection connection, CsvWriter csvWriter)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _csvWriter = csvWriter ?? throw new ArgumentNullException(nameof(csvWriter));
    }

    static PostgresCopyWriter()
    {
        using var dummyWriter = new CsvWriter(TextWriter.Null, CultureInfo.InvariantCulture);
        dummyWriter.WriteHeader<T>();
        Headers = dummyWriter.HeaderRecord ?? throw new InvalidOperationException("Header record is null");
        CopyCommand = $"COPY \"{typeof(T).Name}\" ({string.Join(',', Headers.Select(x => $"\"{x}\""))}) FROM STDIN (FORMAT csv, HEADER MATCH, NULL '')";
    }

    public static async Task<PostgresCopyWriter<T>> Create(NpgsqlDataSource dataSource)
    {
        var connection = await dataSource.OpenConnectionAsync();
        // Disable synchronous_commit for this connection
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET synchronous_commit = OFF;";
            await cmd.ExecuteNonQueryAsync();
        }

        var writer = await connection.BeginTextImportAsync(CopyCommand);
        var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.TypeConverterCache.AddConverter<Enum>(EnumAsIntConverter.Instance);
        csvWriter.Context.TypeConverterCache.AddConverter<DateTimeOffset?>(DateTimeOffsetConverter.Instance);
        csvWriter.WriteHeader<T>();
        await csvWriter.NextRecordAsync();
        return new PostgresCopyWriter<T>(connection, csvWriter);
    }

    public Task WriteRecords(IEnumerable<T> data, CancellationToken cancellationToken = default) =>
        _csvWriter.WriteRecordsAsync(data, cancellationToken);
    public Task WriteRecords(IAsyncEnumerable<T> data, CancellationToken cancellationToken = default) =>
        _csvWriter.WriteRecordsAsync(data, cancellationToken);

    public void Dispose()
    {
        _csvWriter.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _csvWriter.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

internal sealed class EnumAsIntConverter : DefaultTypeConverter
{
    public static readonly EnumAsIntConverter Instance = new();
    private EnumAsIntConverter() { }
    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
        value is Enum enumValue ? $"{Convert.ToInt32(enumValue)}" : base.ConvertToString(value, row, memberMapData);
}

internal sealed class DateTimeOffsetConverter : DefaultTypeConverter
{
    public static readonly DateTimeOffsetConverter Instance = new();
    private DateTimeOffsetConverter() { }
    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
        value is DateTimeOffset offset ? offset.ToString("O") : base.ConvertToString(value, row, memberMapData);
}
