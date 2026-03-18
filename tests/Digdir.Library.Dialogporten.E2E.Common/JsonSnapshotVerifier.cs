using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static partial class JsonSnapshotVerifier
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    public static Task VerifyJsonSnapshot(
        string json,
        string? outputFileText = null,
        bool scrubGuids = true,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string sourceFile = "")
    {
        var scrubbed = scrubGuids
            ? GuidRegex().Replace(json, "00000000-0000-0000-0000-000000000000")
            : json;

        scrubbed = TraceIdRegex()
            .Replace(scrubbed, "\"traceId\": \"00-00000000000000000000000000000000-0000000000000000-00\"");

        using var jsonDocument = JsonDocument.Parse(scrubbed);
        var prettyJson = JsonSerializer.Serialize(jsonDocument.RootElement, IndentedJson);
        var settings = Verify(
                prettyJson,
                extension: "json",
                sourceFile: sourceFile)
            .UseDirectory("Snapshots");

        if (!string.IsNullOrWhiteSpace(outputFileText))
        {
            var methodName = Path.GetFileNameWithoutExtension(sourceFile);
            settings.UseFileName($"{methodName}.{callerMemberName}.{outputFileText}");
        }

        return settings;
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"""traceId"":\s*""[^""]+""")]
    private static partial Regex TraceIdRegex();
}
