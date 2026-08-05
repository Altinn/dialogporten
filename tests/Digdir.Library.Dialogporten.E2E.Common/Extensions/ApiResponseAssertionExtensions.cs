using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Refit;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ApiResponseAssertionExtensions
{
    private static readonly JsonSerializerOptions PrettyPrintJsonOptions = new() { WriteIndented = true };

    private static readonly JsonSerializerOptions ProblemDetailsSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string PrettyPrintJson(string errorContent) =>
        JsonSerializer.Serialize(
            JsonSerializer.Deserialize<JsonElement>(errorContent),
            PrettyPrintJsonOptions);

    extension(IApiResponse response)
    {
        public void ShouldHaveStatusCode(HttpStatusCode expected)
        {
            if (response.StatusCode != expected && response.Error?.Content is { } errorContent)
            {
                try
                {
                    var formatted = PrettyPrintJson(errorContent);

                    TestContext.Current.TestOutputHelper?.WriteLine(
                        $"Unexpected status {response.StatusCode} (expected {expected}). Response:\n{formatted}");
                }
                catch (JsonException)
                {
                    TestContext.Current.TestOutputHelper?.WriteLine(
                        $"Unexpected status {response.StatusCode} (expected {expected}). Response:\n{errorContent}");
                }
            }

            response.StatusCode.Should().Be(expected);
        }

        public async Task VerifyProblemDetailsSnapshot<TProblemDetails>(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string sourceFile = "")
        {
            var problemDetails = await response.Error!
                .GetContentAsAsync<TProblemDetails>();

            problemDetails.Should().NotBeNull();

            var jsonProblemDetails = JsonSerializer
                .Serialize(problemDetails, ProblemDetailsSerializerOptions);

            await VerifyJsonSnapshot(
                jsonProblemDetails,
                callerMemberName: callerMemberName,
                sourceFile: sourceFile);
        }
    }
}
