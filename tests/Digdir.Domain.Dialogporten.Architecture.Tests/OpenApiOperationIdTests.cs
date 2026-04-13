using System.Reflection;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebApi;
using Digdir.Domain.Dialogporten.WebApi.Common;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class OpenApiOperationIdTests
{
    [Fact]
    public void OpenApiOperationId_Values_Should_Be_Unique_Per_Endpoint_Group()
    {
        var endpointTypes = WebApiAssemblyMarker.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(IsFastEndpoint)
            .Where(t => t.GetCustomAttribute<OpenApiOperationIdAttribute>() is not null)
            .ToList();

        // Group by audience (EndUser vs ServiceOwner) since they are separate OpenAPI documents
        var groups = endpointTypes.GroupBy(GetDocumentGroup);

        foreach (var group in groups)
        {
            var duplicates = group
                .GroupBy(t => t.GetCustomAttribute<OpenApiOperationIdAttribute>()!.OperationId)
                .Where(g => g.Count() > 1)
                .ToList();

            duplicates.Should().BeEmpty(
                $"Duplicate [OpenApiOperationId] values in {group.Key}: " +
                $"{string.Join(", ", duplicates.Select(d => $"'{d.Key}' on [{string.Join(", ", d.Select(t => t.Name))}]"))}");
        }
    }

    private static string GetDocumentGroup(Type type)
    {
        var ns = type.Namespace ?? "";
        if (ns.Contains(".EndUser.", StringComparison.OrdinalIgnoreCase)) return "EndUser";
        if (ns.Contains(".ServiceOwner.", StringComparison.OrdinalIgnoreCase)) return "ServiceOwner";
        return "Other";
    }

    private static bool IsFastEndpoint(Type type)
    {
        // Exclude auto-generated Summary classes
        if (type.Name.EndsWith("Summary", StringComparison.Ordinal))
            return false;

        var current = type.BaseType;
        while (current is not null)
        {
            if (current.Namespace == "FastEndpoints" &&
                current.Name.StartsWith("Endpoint", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
