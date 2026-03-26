using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ApiResponseAssertionExtensions
{
    private static readonly JsonSerializerOptions PrettyPrintJsonOptions = new() { WriteIndented = true };

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
    }
}
