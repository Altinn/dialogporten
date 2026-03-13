using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;

public abstract partial class ErrorResponseSnapshotTestBase(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    protected static Task VerifyErrorResponse(string errorResponseJson,
        [CallerFilePath] string sourceFile = "")
    {
        var scrubbed = GuidRegex().Replace(errorResponseJson, "00000000-0000-0000-0000-000000000000");
        scrubbed = TraceIdRegex().Replace(scrubbed, "\"traceId\": \"00-00000000000000000000000000000000-0000000000000000-00\"");
        var prettyJson = JsonSerializer.Serialize(JsonDocument.Parse(scrubbed), IndentedJson);
        return Verify(prettyJson, extension: "json", sourceFile: sourceFile)
            .UseDirectory("Snapshots");
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"""traceId"":\s*""[^""]+""")]
    private static partial Regex TraceIdRegex();
}
