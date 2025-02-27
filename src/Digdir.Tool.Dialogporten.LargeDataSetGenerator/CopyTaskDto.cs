namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

public record struct CopyTaskDto(
    Func<DialogTimestamp, string> Generator,
    string EntityName,
    string CopyCommand,
    bool SingleLinePerTimestamp = false,
    int NumberOfTasks = 1);
