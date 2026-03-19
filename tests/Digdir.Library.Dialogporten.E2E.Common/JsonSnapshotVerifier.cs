using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class JsonSnapshotVerifier
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    public static async Task VerifyJsonSnapshot(
        string json,
        string? fileNameSuffix = null,
        bool scrubGuids = true,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string sourceFile = "")
    {
        ArgumentNullException.ThrowIfNull(json);

        var scrubbed = scrubGuids
            ? GuidRegex.Regex().Replace(json, "00000000-0000-0000-0000-000000000000")
            : json;

        scrubbed = GuidRegex.TraceIdRegex()
            .Replace(scrubbed, "\"traceId\": \"00-00000000000000000000000000000000-0000000000000000-00\"");

        using var jsonDocument = JsonDocument.Parse(scrubbed);
        var prettyJson = JsonSerializer.Serialize(jsonDocument.RootElement, IndentedJson);

        var testFileName = Path.GetFileNameWithoutExtension(sourceFile);
        var verifyTask = Verify(
                prettyJson,
                extension: "json",
                sourceFile: sourceFile)
            .UseFileName($"{testFileName}.{callerMemberName}{FormatSuffix(fileNameSuffix)}")
            .UseDirectory("Snapshots");

        await verifyTask;
    }

    private static string FormatSuffix(string? suffix) =>
        string.IsNullOrWhiteSpace(suffix) ? string.Empty : $".{suffix}";

}

internal static partial class GuidRegex
{
    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    public static partial Regex Regex();

    [GeneratedRegex("""traceId":\s*"[^"]+""")]
    public static partial Regex TraceIdRegex();
}
