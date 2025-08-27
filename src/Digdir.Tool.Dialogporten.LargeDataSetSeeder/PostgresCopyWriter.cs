using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Channels;
using Npgsql;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

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
        var writer = await connection.BeginTextImportAsync(CopyCommand);
        var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csvWriter.Context.TypeConverterCache.AddConverter<Enum>(new EnumAsIntConverter());
        csvWriter.Context.TypeConverterCache.AddConverter<DateTimeOffset?>(new DateTimeOffsetConverter());
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
    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
        value is Enum enumValue ? $"{Convert.ToInt32(enumValue)}" : base.ConvertToString(value, row, memberMapData);
}

internal sealed class DateTimeOffsetConverter : DefaultTypeConverter
{
    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
        value is DateTimeOffset offset ? offset.ToString("O") : base.ConvertToString(value, row, memberMapData);
}

internal sealed class ChannelManager<T> : IAsyncDisposable where T : class
{
    private readonly Channel<T> _channel;
    private readonly List<ConsumerState> _consumers = [];
    private readonly NpgsqlDataSource _dataSource;

    private ChannelManager(NpgsqlDataSource dataSource, int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<T>(capacity);
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public static async Task<ChannelManager<T>> Create(NpgsqlDataSource dataSource, int initialConsumers = 1, int capacity = 10_000)
    {
        var manager = new ChannelManager<T>(dataSource, capacity);
        initialConsumers = Math.Max(1, initialConsumers);
        await Task.WhenAll(Enumerable
            .Range(0, initialConsumers)
            .Select(_ => manager.ScaleUp()));
        return manager;
    }

    public async Task ScaleUp()
    {
        var writer = await PostgresCopyWriter<T>.Create(_dataSource);
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = Task.Run(() => writer.WriteRecords(_channel.Reader.ReadAllAsync()
            .WIthTaskCompletionSource(completionSource), CancellationToken.None));
        _consumers.Add(new ConsumerState(writer, task, completionSource));
    }

    public async Task ScaleDown()
    {
        if (_consumers.Count == 1) return;
        var consumer = _consumers[^1];
        _consumers.Remove(consumer);
        await consumer.DisposeAsync();
    }

    public ValueTask WriteAsync(T item, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    public async ValueTask CompleteAsync()
    {
        _channel.Writer.Complete();
        await Task.WhenAll(_consumers.Select(x => x.DisposeAsync().AsTask()));
        _consumers.Clear();
    }

    private sealed class ConsumerState : IAsyncDisposable
    {
        private readonly PostgresCopyWriter<T> _writer;
        private readonly Task _runningTask;
        private readonly TaskCompletionSource _completionSource;

        public ConsumerState(PostgresCopyWriter<T> writer, Task runningTask, TaskCompletionSource completionSource)
        {
            _writer = writer;
            _runningTask = runningTask;
            _completionSource = completionSource;
        }

        public async ValueTask DisposeAsync()
        {
            // Signal it to finish gracefully
            _completionSource.TrySetResult();
            // Wait for it to complete
            await _runningTask;
            // Dispose the writer
            await _writer.DisposeAsync();
        }
    }

    public ValueTask DisposeAsync() => CompleteAsync();
}

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> WIthTaskCompletionSource<T>(this IAsyncEnumerable<T> values, TaskCompletionSource finished)
    {
        await foreach (var value in values)
        {
            yield return value;
            if (finished.Task.IsCompleted) yield break;
        }
    }
}
