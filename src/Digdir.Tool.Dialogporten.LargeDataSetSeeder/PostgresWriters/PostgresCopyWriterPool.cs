using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Channels;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface IPostgresCopyWriterPool : IAsyncDisposable
{
    Task WriteAsync(object item, CancellationToken cancellationToken = default);
    Task WriteAsync(IEnumerable<object> items, CancellationToken cancellationToken = default);
    Task ScaleUp(int times);
    Task ScaleDown(int times);
    int ConsumerCount { get; }
}

internal sealed class PostgresCopyWriterPool<T> : IPostgresCopyWriterPool where T : class
{
    private readonly Channel<T> _channel;
    private readonly List<ConsumerState> _consumers = [];
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    public int ConsumerCount => _consumers.Count;

    private PostgresCopyWriterPool(NpgsqlDataSource dataSource, int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<T>(capacity);
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public static async Task<PostgresCopyWriterPool<T>> Create(NpgsqlDataSource dataSource, int initialConsumers = 1, int capacity = 10_000)
    {
        var manager = new PostgresCopyWriterPool<T>(dataSource, capacity);
        initialConsumers = Math.Max(1, initialConsumers);
        await Task.WhenAll(Enumerable
            .Range(0, initialConsumers)
            .Select(_ => manager.ScaleUp()));
        return manager;
    }

    public Task WriteAsync(object item, CancellationToken cancellationToken = default) =>
        item is T typedItem
            ? WriteAsync(typedItem, cancellationToken)
            : throw new ArgumentException($"Invalid item type, expected {typeof(T)}, got {item.GetType()}", nameof(item));

    public Task WriteAsync(IEnumerable<object> items, CancellationToken cancellationToken = default) =>
        items is IEnumerable<T> typedItems
            ? WriteAsync(typedItems, cancellationToken)
            : throw new ArgumentException($"Invalid item type, expected {typeof(IEnumerable<T>)}, got {items.GetType()}", nameof(items));

    public async Task ScaleUp(int times)
    {
        for (var i = 0; i < times; i++)
        {
            await ScaleUp();
        }
    }

    public async Task ScaleDown(int times)
    {
        for (var i = 0; i < times; i++)
        {
            await ScaleDown();
        }
    }

    public async Task ScaleUp()
    {
        EnsuredNotDisposed();
        var writer = await PostgresCopyWriter<T>.Create(_dataSource);
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // var task = Task
        //     .Run(async () => await writer
        //         .WriteRecords(_channel.Reader.ReadAllAsync()
        //         .WIthTaskCompletionSource(completionSource)))
        //     .ContinueWith(t => Environment.FailFast($"Fatal consumer task crashed: {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
        var task = writer.WriteRecords(_channel.Reader.ReadAllAsync().WIthTaskCompletionSource(completionSource));
        _consumers.Add(new ConsumerState(writer, task, completionSource));
    }

    public async Task ScaleDown()
    {
        EnsuredNotDisposed();
        if (_consumers.Count == 1) return;
        var consumer = _consumers[^1];
        _consumers.Remove(consumer);
        await consumer.DisposeAsync();
    }

    public Task WriteAsync(T item, CancellationToken cancellationToken = default)
    {
        EnsuredNotDisposed();
        return _channel.Writer.WriteAsync(item, cancellationToken).AsTask();
    }

    public async Task WriteAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        EnsuredNotDisposed();
        foreach (var item in items)
        {
            await WriteAsync(item, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _channel.Writer.Complete();
        await _channel.Reader.Completion;
        await Task.WhenAll(_consumers.Select(x => x.DisposeAsync().AsTask()));
        _consumers.Clear();
        _disposed = true;
    }

    private void EnsuredNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

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
}

internal static class PostgresCopyWriterPoolFactory
{
    [SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    public static async Task<IPostgresCopyWriterPool> Create(
        Type entityType,
        NpgsqlDataSource dataSource,
        int initialConsumers = 1,
        int capacity = 10_000)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(dataSource);

        // Construct generic type: PostgresCopyWriterPool<entityType>
        var poolType = typeof(PostgresCopyWriterPool<>).MakeGenericType(entityType);

        // Get static Create method
        var createMethod = poolType.GetMethod(
            nameof(PostgresCopyWriterPool<object>.Create),
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Create method not found on {poolType}");

        // Invoke Create<T>(dataSource, initialConsumers, capacity)
        var task = (Task)createMethod.Invoke(null, [dataSource, initialConsumers, capacity])!;
        await task.ConfigureAwait(false);
        // Extract Result via reflection
        return (IPostgresCopyWriterPool)task.GetType()
            .GetProperty(nameof(Task<object>.Result))!
            .GetValue(task)!;
    }
}
