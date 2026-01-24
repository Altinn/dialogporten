#if DEBUG
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
#endif // DEBUG

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1;

public class SwaggerSnapshotTests
{

    [Fact]
    public async Task FailIfSwaggerSnapshotDoesNotMatch()
    {
#if RELEASE
        // Arrange
        // This test checks for changes against the published version of the swagger.verified.json file
        // The file is located at /docs/schema/ on the solution root
        // Commiting a change to this file will trigger a build and publish
        // of the npm package located in the same folder
        var rootPath = Utils.GetSolutionRootFolder();
        var swaggerPath = Path.Combine(rootPath!, "docs/schema/V1");

#if NET9_0
        var newSwaggerPath = Path.Combine(rootPath!, "src/Digdir.Domain.Dialogporten.WebApi/bin/Release/net9.0/swagger.json");
#endif // NET9_0

        Assert.True(File.Exists(newSwaggerPath), $"Swagger file not found at {newSwaggerPath}. Make sure you have built the project in RELEASE mode.");
        // Act
        var newSwagger = await File.ReadAllTextAsync(newSwaggerPath, TestContext.Current.CancellationToken);

        // The order of the properties in the swagger.json file is not cross-platform deterministic.
        // Running Nswag on Windows and Mac will produce
        // different ordering of the results (although the content is the same). So we force an
        // alphabetical ordering of the properties to make the test deterministic.
        // Ref: https://github.com/altinn/dialogporten/issues/996
        var orderedSwagger = SortJson(newSwagger);

        // Assert

        await Verify(orderedSwagger, extension: "json")
            .UseFileName("swagger")
            .UseDirectory(swaggerPath);
#else // RELEASE
        Assert.Fail(
            "Swagger snapshot tests are not supported in DEBUG mode. Swagger is NOT generated in DEBUG mode, this is to keep build times low. Therefore, this test will always fail. Run in RELEASE mode to enable.");

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
