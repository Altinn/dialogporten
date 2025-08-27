using System.Text;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public static class Utils
{
    // public static string CreateCopyCommand(string tableName, params string[] props) =>
    //     string.Format(null, CopyCommandBase, tableName, string.Join(", ", props));
    //
    // private static readonly CompositeFormat CopyCommandBase = CompositeFormat
    //     .Parse("""COPY "{0}" FROM STDIN (FORMAT csv, HEADER MATCH, NULL '')""");

    public static string BuildCsv(Action<StringBuilder> buildAction)
    {
        var sb = new StringBuilder();
        buildAction(sb);
        return sb.ToString();
    }

    public static List<T> BuildDtoList<T>(Action<List<T>> buildAction)
    {
        var list = new List<T>();
        buildAction(list);
        return list;
    }

    public static string Null => string.Empty;
}
