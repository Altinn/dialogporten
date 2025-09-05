using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface IPostgresCopyWriterPool : IAsyncDisposable
{
    int WorkerCount { get; }
    Type Type { get; }
    Task ScaleTo(int workers);
    Task WriteAsync(object item, CancellationToken cancellationToken = default);
    Task WriteAsync(IEnumerable<object> items, CancellationToken cancellationToken = default);
}

internal sealed class PostgresCopyWriterPool<T> : IPostgresCopyWriterPool where T : class
{
    private static readonly Type Type = typeof(T);
    private readonly Channel<T> _channel;
    private readonly List<CopyWorker> _workers = [];
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;
    private long? _started;

    public int WorkerCount => _workers.Count;
    Type IPostgresCopyWriterPool.Type => Type;

    private PostgresCopyWriterPool(NpgsqlDataSource dataSource, int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<T>(capacity);
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public static async Task<PostgresCopyWriterPool<T>> Create(
        NpgsqlDataSource dataSource,
        int initialWorkers = 1,
        int capacity = 10_000)
    {
        var pool = new PostgresCopyWriterPool<T>(dataSource, capacity);
        await pool.ScaleTo(Math.Max(1, initialWorkers));
        pool._started = Stopwatch.GetTimestamp();
        return pool;
    }

    public Task WriteAsync(object item, CancellationToken cancellationToken = default) =>
        item is T typedItem
            ? WriteAsync(typedItem, cancellationToken)
            : throw new ArgumentException($"Invalid item type, expected {typeof(T)}, got {item.GetType()}", nameof(item));

    public Task WriteAsync(IEnumerable<object> items, CancellationToken cancellationToken = default) =>
        items is IEnumerable<T> typedItems
            ? WriteAsync(typedItems, cancellationToken)
            : throw new ArgumentException($"Invalid item type, expected {typeof(IEnumerable<T>)}, got {items.GetType()}", nameof(items));

    public async Task WriteAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        foreach (var item in items)
        {
            await WriteAsync(item, cancellationToken);
        }
    }

    public Task WriteAsync(T item, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        return _channel.Writer.WriteAsync(item, cancellationToken).AsTask();
    }

    public async Task ScaleTo(int workers)
    {
        EnsureNotDisposed();
        if (workers < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(workers), "There must be at least one worker");
        }

        var diff = workers - _workers.Count;
        var scale = diff switch
        {
            < 0 => ScaleDown,
            > 0 => ScaleUp,
            _ => (Func<Task>?)null
        };

        if (scale is null)
        {
            return;
        }

        for (var i = 0; i < Math.Abs(diff); i++)
        {
            await scale();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _channel.Writer.Complete();
        await _channel.Reader.Completion;
        await Task.WhenAll(_workers.Select(x => x.DisposeAsync().AsTask()));
        Console.WriteLine($"time taken for {typeof(T).Name} writer pool: {Stopwatch.GetElapsedTime(_started!.Value)}");
        _workers.Clear();
        _disposed = true;
    }

    private Task ScaleUp()
    {
        EnsureNotDisposed();
        _workers.Add(new CopyWorker(_dataSource, _channel.Reader));
        return Task.CompletedTask;
    }

    private async Task ScaleDown()
    {
        EnsureNotDisposed();
        if (_workers.Count == 1) return;
        var worker = _workers[^1];
        _workers.Remove(worker);
        await worker.DisposeAsync();
    }

    private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private sealed class CopyWorker : IAsyncDisposable
    {
        private const int CopyBatchSize = 10_000_000;
        private readonly Task _writerTask;
        private readonly CancellationTokenSource _cancellationSource;

        public CopyWorker(NpgsqlDataSource dataSource, ChannelReader<T> reader)
        {
            _cancellationSource = new CancellationTokenSource();
            _writerTask = Task.Run(async () =>
            {
                try
                {
                    await using var enumerator = reader
                        .ReadAllAsync(_cancellationSource.Token)
                        .GetAsyncEnumerator(_cancellationSource.Token);
                    while (!_cancellationSource.IsCancellationRequested && !reader.Completion.IsCompleted)
                    {
                        await using var writer = await PostgresCopyWriter<T>.Create(dataSource);
                        var batch = GetGracefulBatchAsync(enumerator, CopyBatchSize, _cancellationSource.Token);
                        await writer.WriteRecords(batch);
                        Console.WriteLine($"{DateTimeOffset.UtcNow:O}: Wrote batch of up to {CopyBatchSize} records to {typeof(T).Name}");
                    }
                }
                catch (OperationCanceledException) { /* Ignore cancellation */ }
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _cancellationSource.CancelAsync();
            await _writerTask;
            _cancellationSource.Dispose();
        }

        private static async IAsyncEnumerable<T> GetGracefulBatchAsync(
            IAsyncEnumerator<T> enumerator,
            int batchSize,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var count = 0;
            while (count++ < batchSize && !cancellationToken.IsCancellationRequested)
            {
                if (!await enumerator.MoveNextAsync()) yield break;
                yield return enumerator.Current;
            }
        }
    }
}
