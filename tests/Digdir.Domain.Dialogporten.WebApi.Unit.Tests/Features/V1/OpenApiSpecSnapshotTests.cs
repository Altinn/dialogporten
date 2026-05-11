#if DEBUG
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters.
#endif // DEBUG

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1;

public class OpenApiSpecSnapshotTests
{
    // The snapshot files are located at /docs/schema/V1 on the solution root.
    // Committing a change to these files will trigger a build and publish
    // of the npm package located in the same folder.
    public static TheoryData<string, string> GeneratedSpecs => new()
    {
        { "swagger.json", "swagger" },
        { "openapi.v1.enduser.json", "openapi.v1.enduser" },
        { "openapi.v1.serviceowner.json", "openapi.v1.serviceowner" }
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

                    using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(sortedProperties));
                    return jsonDocument.RootElement.Clone();
                }
            case JsonValueKind.Array:
                {
                    var sortedArray = element
                        .EnumerateArray()
                        .Select(SortElement)
                        .ToList();
                    using var arrayDocument = JsonDocument.Parse(JsonSerializer.Serialize(sortedArray));
                    return arrayDocument.RootElement.Clone();
                }
            default:
                return element;
        }
    }
}
