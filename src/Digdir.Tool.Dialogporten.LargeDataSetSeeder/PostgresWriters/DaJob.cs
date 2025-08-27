using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal static class DaJob
{
    public static async Task Execute(NpgsqlDataSource dataSource, DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var writerByTask = new Dictionary<Task, IPostgresCopyWriterPool>();

        foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
        {
            var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
            var writer = await PostgresCopyWriterPoolFactory.Create(targetType, dataSource);
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
