using System.Threading.Channels;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal sealed class PostgresCopyConsumerPool<T> : IAsyncDisposable where T : class
{
    private readonly Channel<T> _channel;
    private readonly List<ConsumerState> _consumers = [];
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    private PostgresCopyConsumerPool(NpgsqlDataSource dataSource, int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<T>(capacity);
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public static async Task<PostgresCopyConsumerPool<T>> Create(NpgsqlDataSource dataSource, int initialConsumers = 1, int capacity = 10_000)
    {
        var manager = new PostgresCopyConsumerPool<T>(dataSource, capacity);
        initialConsumers = Math.Max(1, initialConsumers);
        await Task.WhenAll(Enumerable
            .Range(0, initialConsumers)
            .Select(_ => manager.ScaleUp()));
        return manager;
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

    public ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
    {
        EnsuredNotDisposed();
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _channel.Writer.Complete();
        await Task.WhenAll(_consumers.Select(x => x.DisposeAsync().AsTask()));
        _consumers.Clear();
        _disposed = true;
    }

    private void EnsuredNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
}

// internal class DaJob
// {
//     public async Task Execute(NpgsqlDataSource dataSource, DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
//     {
//         foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
//         {
//             var factoryMethod = typeof(PostgresCopyConsumerPool<>)
//                 .MakeGenericType(targetType)
//                 .GetMethod(nameof(PostgresCopyConsumerPool<object>.Create))
//                 ?? throw new InvalidOperationException("Could not find Create method");
//             
//             var task = (Task
//         }
//     }
//
//     public static async Task Something<T>(NpgsqlDataSource dataSource, IEnumerable<T> values)
//         where T : class
//     {
//         var consumerPool = await PostgresCopyConsumerPool<T>.Create(dataSource);
//         foreach (var value in values)
//         {
//             await consumerPool.WriteAsync(value);
//         }
//     }
//
//     public static async Task<object> CreateConsumerPool<T>(NpgsqlDataSource dataSource)
//         where T : class
//     {
//         return await PostgresCopyConsumerPool<T>.Create(dataSource);
//     }
// }
