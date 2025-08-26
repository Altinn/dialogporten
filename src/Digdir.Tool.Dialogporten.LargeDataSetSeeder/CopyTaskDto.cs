namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public record struct CopyTaskDto<T>(
    Func<IEnumerable<DialogTimestamp>, IEnumerable<T>> Generator,
    string EntityName,
    bool SingleLinePerTimestamp = false,
    int NumberOfTasks = 1) where T : class;
