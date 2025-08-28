using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    // private readonly int _maxConnextions;

    public PostgresCopyWriterCoordinator(
        NpgsqlDataSource dataSource
        // int maxConnextions
        )
    {
        // ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnextions);
        // _maxConnextions = maxConnextions;
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task Handle(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        int dialogAmount)
    {
        var writerByTask = new Dictionary<Task, IPostgresCopyWriterPool>();

        foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
        {
            var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
            var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource);
            var task = writer.WriteAsync(entityEnumerable);
            writerByTask[task] = writer;
        }

        await foreach (var task in Task.WhenEach(writerByTask.Keys))
        {
            await task;
            await writerByTask[task].DisposeAsync();
        }
    }
}


