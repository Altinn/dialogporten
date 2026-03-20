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
        bool scrubDateTimeOffsets = true,
        bool scrubIdentifierEphemeralActors = true,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string sourceFile = "")
    {
        ArgumentNullException.ThrowIfNull(json);

        var scrubbed = scrubGuids
            ? SnapshotScrubbing.GuidRegex().Replace(json, "00000000-0000-0000-0000-000000000000")
            : json;

        scrubbed = scrubDateTimeOffsets
            ? SnapshotScrubbing.DateTimeOffsetRegex().Replace(scrubbed, "\"0000-00-00T00:00:00.000000+00:00\"")
            : scrubbed;

        scrubbed = scrubIdentifierEphemeralActors
            ? SnapshotScrubbing.IdentifierEphemeralActorRegex().Replace(
                scrubbed,
                "\"urn:altinn:person:identifier-ephemeral:**********\"")
            : scrubbed;

        scrubbed = SnapshotScrubbing.TraceIdRegex()
            .Replace(scrubbed, "\"traceId\": \"00-00000000000000000000000000000000-0000000000000000-00\"");

        scrubbed = SnapshotScrubbing.DialogTokenRegex()
            .Replace(scrubbed, "\"dialogToken\": \"***\"");

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

internal static partial class SnapshotScrubbing
{
    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    public static partial Regex GuidRegex();

    [GeneratedRegex("\"traceId\":\\s*\"[^\"]+\"")]
    public static partial Regex TraceIdRegex();

    [GeneratedRegex("\"dialogToken\":\\s*\"[^\"]+\"")]
    public static partial Regex DialogTokenRegex();

    [GeneratedRegex("\"\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{1,7})?[+-]\\d{2}:\\d{2}\"")]
    public static partial Regex DateTimeOffsetRegex();

    [GeneratedRegex("\"urn:altinn:person:identifier-ephemeral:[a-zA-Z0-9]{10}\"")]
    public static partial Regex IdentifierEphemeralActorRegex();
}
