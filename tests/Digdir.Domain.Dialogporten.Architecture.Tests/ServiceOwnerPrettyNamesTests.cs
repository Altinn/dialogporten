using System.Collections.Immutable;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.WebApiClient.ServiceOwner.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class ServiceOwnerPrettyNamesTests
{
    private const string TransportNamespace = "Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner";
    private static readonly ImmutableHashSet<string> RootTransportTypes =
    [
        "DialogsGetQueryParams",
        "EndusercontextQueryParams",
        "JsonPatchOperations_Operation",
        "PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog",
        "PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem",
        "ShouldSendNotificationQueryParams",
        "V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup",
        "V1EndUserCommon_AcceptedLanguages",
        "V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest",
        "V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest",
        "V1ServiceOwnerDialogsCommandsCreate_Dialog",
        "V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest",
        "V1ServiceOwnerDialogsCommandsUpdate_Dialog",
        "V1ServiceOwnerDialogsQueriesGetActivity_Activity",
        "V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog",
        "V1ServiceOwnerDialogsQueriesGetTransmission_Transmission",
        "V1ServiceOwnerDialogsQueriesGet_Dialog",
        "V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition",
        "V1ServiceOwnerDialogsQueriesSearchActivities_Activity",
        "V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog",
        "V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission",
        "V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel",
        "V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest",
        "V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label"
    ];

    [Fact]
    public void Reachable_Transport_Types_Should_Have_Unique_Pretty_Names()
    {
        var transportTypes = ParseTransportTypes();
        var reachableTransportTypes = CollectReachableTransportTypes(transportTypes);
        var prettyNames = reachableTransportTypes
            .Select(GetPrettyName)
            .ToList();

        prettyNames.Should().OnlyHaveUniqueItems();
    }

    private static Dictionary<string, TransportType> ParseTransportTypes()
    {
        var refitterFilePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Digdir.Library.Dialogporten.WebApiClient.ServiceOwner",
            "Features",
            "V1",
            "ServiceOwner",
            "RefitterInterface.cs");

        var source = File.ReadAllText(refitterFilePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Where(static declaration =>
                declaration.Parent is BaseNamespaceDeclarationSyntax namespaceDeclaration &&
                namespaceDeclaration.Name.ToString().Equals(TransportNamespace, StringComparison.Ordinal) &&
                declaration is ClassDeclarationSyntax or EnumDeclarationSyntax)
            .Select(static declaration => declaration switch
            {
                ClassDeclarationSyntax classDeclaration => new TransportType(
                    classDeclaration.Identifier.ValueText,
                    [
                        ..classDeclaration.Members
                            .OfType<PropertyDeclarationSyntax>()
                            .Where(static property => property.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                            .Select(static property => property.Type)
                    ]),
                EnumDeclarationSyntax enumDeclaration => new TransportType(
                    enumDeclaration.Identifier.ValueText,
                    []),
                _ => throw new InvalidOperationException("Unsupported declaration type.")
            })
            .ToDictionary(static type => type.Name, StringComparer.Ordinal);
    }

    private static List<string> CollectReachableTransportTypes(Dictionary<string, TransportType> transportTypes)
    {
        var queue = new Queue<string>(RootTransportTypes);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var reachable = new List<string>();
        var knownNames = transportTypes.Keys.ToHashSet(StringComparer.Ordinal);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            transportTypes.ContainsKey(current).Should().BeTrue($"'{current}' should exist in RefitterInterface.cs");
            reachable.Add(current);

            foreach (var dependency in transportTypes[current].PropertyTypes
                         .SelectMany(static type => type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                         .Select(static identifier => identifier.Identifier.ValueText)
                         .Where(knownNames.Contains)
                         .Distinct(StringComparer.Ordinal))
            {
                queue.Enqueue(dependency);
            }
        }

        return reachable;
    }

    private static string GetPrettyName(string transportTypeName)
    {
        var success = ServiceOwnerPrettyNames.TryGetPrettyName(transportTypeName, out var prettyName);

        success.Should().BeTrue($"'{transportTypeName}' should have a pretty-name mapping");
        prettyName.Should().BeOfType<string>();

        return prettyName;
    }

    private sealed record TransportType(string Name, ImmutableArray<TypeSyntax> PropertyTypes);
}
