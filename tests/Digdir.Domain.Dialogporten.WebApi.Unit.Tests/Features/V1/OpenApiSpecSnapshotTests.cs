#if DEBUG
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
#endif // DEBUG

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1;

public class OpenApiSpecSnapshotTests
{
    public static TheoryData<string, string> GeneratedSpecs => new()
    {
        { "openapi.enduser.json", "openapi.enduser" },
        { "openapi.serviceowner.json", "openapi.serviceowner" }
    };

    [Theory]
    [MemberData(nameof(GeneratedSpecs))]
    public async Task FailIfOpenApiSnapshotDoesNotMatch(string generatedSpecFileName, string snapshotFileName)
    {
#if RELEASE
        // Arrange
        var rootPath = Utils.GetSolutionRootFolder();
        var snapshotPath = Path.Combine(rootPath!, "docs/schema/V1");

#if NET10_0
        var generatedSpecPath = Path.Combine(
            rootPath!,
            "src/Digdir.Domain.Dialogporten.WebApi/bin/Release/net10.0",
            generatedSpecFileName);
#endif // NET10_0

        Assert.True(
            File.Exists(generatedSpecPath),
            $"OpenAPI spec file not found at {generatedSpecPath}. Make sure you have built the project in RELEASE mode.");

        // Act
        var generatedSpec = await File.ReadAllTextAsync(generatedSpecPath, TestContext.Current.CancellationToken);
        var orderedSpec = SortJson(generatedSpec);

        // Assert
        await Verify(orderedSpec, extension: "json")
            .UseFileName(snapshotFileName)
            .UseDirectory(snapshotPath);
#else // RELEASE
        Assert.Fail(
            "OpenAPI snapshot tests are not supported in DEBUG mode. OpenAPI specs are snapshot-tested from RELEASE builds. Run in RELEASE mode to enable.");
#endif // RELEASE
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static string SortJson(string jsonString)
    {
        using var document = JsonDocument.Parse(jsonString);
        var sortedElement = SortElement(document.RootElement);
        return JsonSerializer.Serialize(sortedElement, SerializerOptions);
    }

    [SuppressMessage("Style", "IDE0010:Add missing cases")]
    private static JsonElement SortElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var sortedProperties = new SortedDictionary<string, JsonElement>();
                    foreach (var property in element.EnumerateObject())
                    {
                        sortedProperties[property.Name] = SortElement(property.Value);
                    }

                    var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(sortedProperties));
                    return jsonDocument.RootElement;
                }
            case JsonValueKind.Array:
                {
                    var sortedArray = element
                        .EnumerateArray()
                        .Select(SortElement)
                        .ToList();
                    var arrayDocument = JsonDocument.Parse(JsonSerializer.Serialize(sortedArray));
                    return arrayDocument.RootElement;
                }
            default:
                return element;
        }
    }
}
