namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public class CopyTaskDto<T> where T : class
{
    public Func<IEnumerable<DialogTimestamp>, IEnumerable<T>>? Generator { get; set; }
    public string? EntityName { get; set; }
    public bool SingleLinePerTimestamp { get; set; }
    public int NumberOfTasks { get; set; } = 1;

    public CopyTaskDto() { }
    public CopyTaskDto(Func<IEnumerable<DialogTimestamp>, IEnumerable<T>>? generator, string? entityName, bool singleLinePerTimestamp = false, int numberOfTasks = 1)
    {
        Generator = generator;
        EntityName = entityName;
        SingleLinePerTimestamp = singleLinePerTimestamp;
        NumberOfTasks = numberOfTasks;
    }
}
