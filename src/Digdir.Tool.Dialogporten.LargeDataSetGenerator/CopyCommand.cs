using System.Text;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

public static class CopyCommand
{
    private static readonly CompositeFormat CopyCommandBase = CompositeFormat
        .Parse("""COPY "{0}" ({1}) FROM STDIN (FORMAT csv, HEADER false, NULL '')""");

    public static string Create(string tableName, params string[] props) =>
        string.Format(null, CopyCommandBase, tableName, string.Join(", ", props));
}

public static class CsvBuilder
{
    public static string BuildCsv(Action<StringBuilder> buildAction)
    {
        var sb = new StringBuilder();
        buildAction(sb);
        return sb.ToString();
    }
}
