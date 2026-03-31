using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Digdir.Library.Dialogporten.WebApiClient.ServiceOwner.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ServiceOwnerContractsGenerator : IIncrementalGenerator
{
    private const string ContractNamespace = "Altinn.ApiClients.Dialogporten.ServiceOwner.V1";
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

    private static readonly DiagnosticDescriptor MissingRefitterFileDescriptor = new(
        id: "SO0001",
        title: "Missing Refitter input",
        messageFormat: "Could not find Refitter interface file '{0}' for ServiceOwner contract generation",
        category: "SourceGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnknownTypeDescriptor = new(
        id: "SO0002",
        title: "Missing pretty-name mapping",
        messageFormat: "No pretty-name mapping exists for transport type '{0}'. Add a mapping rule before exposing it in the public client API",
        category: "SourceGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicatePrettyNameDescriptor = new(
        id: "SO0003",
        title: "Duplicate pretty-name mapping",
        messageFormat: "Multiple transport types map to the same pretty contract name '{0}'",
        category: "SourceGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var refitterSource = context.AdditionalTextsProvider
            .Where(static file => Path.GetFileName(file.Path).Equals("RefitterInterface.cs", StringComparison.Ordinal))
            .Select(static (file, cancellationToken) => new RefitterInput(file.Path, file.GetText(cancellationToken)?.ToString()))
            .Collect();

        context.RegisterSourceOutput(refitterSource, static (productionContext, inputs) => Generate(productionContext, inputs));
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<RefitterInput> inputs)
    {
        var input = inputs.FirstOrDefault(static x => x.Content is not null);
        if (input?.Content is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingRefitterFileDescriptor, Location.None, "RefitterInterface.cs"));
            return;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(input.Content, Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.Latest, documentationMode: DocumentationMode.Parse));

        var root = syntaxTree.GetCompilationUnitRoot(context.CancellationToken);
        var transportTypes = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Where(IsSupportedTransportType)
            .Select(CreateTypeModel)
            .ToImmutableDictionary(model => model.TransportName, StringComparer.Ordinal);

        var reachableTransportTypes = CollectReachableTypes(transportTypes, context);
        if (reachableTransportTypes.IsDefaultOrEmpty)
        {
            return;
        }

        var prettyNames = BuildPrettyNames(reachableTransportTypes, context);
        if (prettyNames is null)
        {
            return;
        }

        context.AddSource("ServiceOwnerV1Contracts.g.cs", SourceText.From(GenerateContractsSource(reachableTransportTypes, prettyNames), Encoding.UTF8));
        context.AddSource("ServiceOwnerV1Mappings.g.cs", SourceText.From(GenerateMappingsSource(reachableTransportTypes, prettyNames), Encoding.UTF8));
    }

    private static bool IsSupportedTransportType(BaseTypeDeclarationSyntax declaration) =>
        declaration.Parent is BaseNamespaceDeclarationSyntax namespaceDeclaration &&
        namespaceDeclaration.Name.ToString().Equals(TransportNamespace, StringComparison.Ordinal) &&
        declaration is ClassDeclarationSyntax or EnumDeclarationSyntax;

    private static TypeModel CreateTypeModel(BaseTypeDeclarationSyntax declaration) =>
        declaration switch
        {
            ClassDeclarationSyntax classDeclaration => new(
                classDeclaration.Identifier.ValueText,
                TransportTypeKind.Class,
                [
                    ..classDeclaration.Members
                        .OfType<PropertyDeclarationSyntax>()
                        .Where(static property => property.Modifiers.Any(SyntaxKind.PublicKeyword))
                        .Select(static property => new PropertyModel(
                            property.Identifier.ValueText,
                            property.Type,
                            GetJsonPropertyName(property)))
                ],
                []),
            EnumDeclarationSyntax enumDeclaration => new(
                enumDeclaration.Identifier.ValueText,
                TransportTypeKind.Enum,
                [],
                [.. enumDeclaration.Members.Select(static member => member.Identifier.ValueText)]),
            _ => throw new InvalidOperationException($"Unsupported transport declaration '{declaration.Kind()}'.")
        };

    private static ImmutableArray<TypeModel> CollectReachableTypes(
        ImmutableDictionary<string, TypeModel> transportTypes,
        SourceProductionContext context)
    {
        var queue = new Queue<string>(RootTransportTypes);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var reachable = new List<TypeModel>();

        while (queue.Count > 0)
        {
            var transportTypeName = queue.Dequeue();
            if (!visited.Add(transportTypeName))
            {
                continue;
            }

            if (!transportTypes.TryGetValue(transportTypeName, out var typeModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(UnknownTypeDescriptor, Location.None, transportTypeName));
                return [];
            }

            reachable.Add(typeModel);

            foreach (var dependency in GetReferencedTransportTypes(typeModel, transportTypes.Keys))
            {
                queue.Enqueue(dependency);
            }
        }

        return [.. reachable.OrderBy(static model => model.TransportName, StringComparer.Ordinal)];
    }

    private static ImmutableDictionary<string, string>? BuildPrettyNames(
        ImmutableArray<TypeModel> reachableTransportTypes,
        SourceProductionContext context)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var typeModel in reachableTransportTypes)
        {
            if (!ServiceOwnerPrettyNames.TryGetPrettyName(typeModel.TransportName, out var prettyName))
            {
                context.ReportDiagnostic(Diagnostic.Create(UnknownTypeDescriptor, Location.None, typeModel.TransportName));
                return null;
            }

            builder.Add(typeModel.TransportName, prettyName!);
        }

        var duplicatePrettyNames = builder
            .GroupBy(static pair => pair.Value, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key);

        var hasDuplicates = false;
        foreach (var duplicatePrettyName in duplicatePrettyNames)
        {
            hasDuplicates = true;
            context.ReportDiagnostic(Diagnostic.Create(DuplicatePrettyNameDescriptor, Location.None, duplicatePrettyName));
        }

        return hasDuplicates ? null : builder.ToImmutable();
    }

    private static IEnumerable<string> GetReferencedTransportTypes(TypeModel typeModel, IEnumerable<string> knownTransportTypeNames)
    {
        var knownNames = knownTransportTypeNames.ToImmutableHashSet(StringComparer.Ordinal);
        return typeModel.Properties
            .SelectMany(static property => property.TypeSyntax.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            .Select(static identifier => identifier.Identifier.ValueText)
            .Where(knownNames.Contains)
            .Distinct(StringComparer.Ordinal);
    }

    private static string GenerateContractsSource(
        ImmutableArray<TypeModel> reachableTransportTypes,
        ImmutableDictionary<string, string> prettyNames)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS0618, CS8601, CS8602, CS8603, CS8604, CS8618, CS8619");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json.Serialization;");
        builder.AppendLine();
        builder.AppendLine($"namespace {ContractNamespace};");
        builder.AppendLine();

        foreach (var typeModel in reachableTransportTypes.OrderBy(static model => model.TransportName, StringComparer.Ordinal))
        {
            var prettyName = prettyNames[typeModel.TransportName];

            if (typeModel.Kind == TransportTypeKind.Enum)
            {
                builder.AppendLine($"public enum {prettyName}");
                builder.AppendLine("{");
                for (var index = 0; index < typeModel.EnumMembers.Length; index++)
                {
                    var suffix = index == typeModel.EnumMembers.Length - 1 ? string.Empty : ",";
                    builder.AppendLine($"    {typeModel.EnumMembers[index]}{suffix}");
                }

                builder.AppendLine("}");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine($"public partial class {prettyName}");
            builder.AppendLine("{");
            foreach (var property in typeModel.Properties)
            {
                if (property.JsonPropertyName is not null)
                {
                    builder.AppendLine($"    [JsonPropertyName(\"{property.JsonPropertyName}\")]");
                }

                builder.AppendLine($"    public {RewriteType(property.TypeSyntax, prettyNames)} {property.Name} {{ get; set; }}");
            }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        builder.AppendLine("#pragma warning restore CS0618, CS8601, CS8602, CS8603, CS8604, CS8618, CS8619");
        return builder.ToString();
    }

    private static string GenerateMappingsSource(
        ImmutableArray<TypeModel> reachableTransportTypes,
        ImmutableDictionary<string, string> prettyNames)
    {
        var transportKinds = reachableTransportTypes.ToImmutableDictionary(model => model.TransportName, model => model.Kind, StringComparer.Ordinal);
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS0618, CS8601, CS8602, CS8603, CS8604, CS8618, CS8619");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner;");
        builder.AppendLine();
        builder.AppendLine($"namespace {ContractNamespace};");
        builder.AppendLine();
        builder.AppendLine("internal static partial class ServiceOwnerV1TypeMappers");
        builder.AppendLine("{");

        foreach (var typeModel in reachableTransportTypes.OrderBy(static model => model.TransportName, StringComparer.Ordinal))
        {
            var prettyName = prettyNames[typeModel.TransportName];

            if (typeModel.Kind == TransportTypeKind.Enum)
            {
                builder.AppendLine($"    public static {prettyName} ToContract(this {typeModel.TransportName} source) => source switch");
                builder.AppendLine("    {");
                foreach (var enumMember in typeModel.EnumMembers)
                {
                    builder.AppendLine($"        {typeModel.TransportName}.{enumMember} => {prettyName}.{enumMember},");
                }

                builder.AppendLine("        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)");
                builder.AppendLine("    };");
                builder.AppendLine();
                builder.AppendLine($"    public static {typeModel.TransportName} ToTransport(this {prettyName} source) => source switch");
                builder.AppendLine("    {");
                foreach (var enumMember in typeModel.EnumMembers)
                {
                    builder.AppendLine($"        {prettyName}.{enumMember} => {typeModel.TransportName}.{enumMember},");
                }

                builder.AppendLine("        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)");
                builder.AppendLine("    };");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine($"    public static {prettyName} ToContract(this {typeModel.TransportName} source)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (source is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            return null!;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        return new {prettyName}");
            builder.AppendLine("        {");
            foreach (var property in typeModel.Properties)
            {
                builder.AppendLine($"            {property.Name} = {BuildMapExpression(property.TypeSyntax, $"source.{property.Name}", MappingDirection.ToContract, transportKinds)},");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine();

            builder.AppendLine($"    public static {typeModel.TransportName} ToTransport(this {prettyName} source)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (source is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            return null!;");
            builder.AppendLine("        }");
            builder.AppendLine();
            var constructorExpression = TryBuildTransportConstructorExpression(typeModel, transportKinds);
            if (constructorExpression is not null)
            {
                builder.AppendLine($"        var target = {constructorExpression};");
                foreach (var property in typeModel.Properties.Where(property => !IsConstructorBoundProperty(typeModel.TransportName, property.Name)))
                {
                    builder.AppendLine($"        target.{property.Name} = {BuildMapExpression(property.TypeSyntax, $"source.{property.Name}", MappingDirection.ToTransport, transportKinds)};");
                }

                builder.AppendLine();
                builder.AppendLine("        return target;");
                builder.AppendLine("    }");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine($"        return new {typeModel.TransportName}");
            builder.AppendLine("        {");
            foreach (var property in typeModel.Properties)
            {
                builder.AppendLine($"            {property.Name} = {BuildMapExpression(property.TypeSyntax, $"source.{property.Name}", MappingDirection.ToTransport, transportKinds)},");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        builder.AppendLine("#pragma warning restore CS0618, CS8601, CS8602, CS8603, CS8604, CS8618, CS8619");
        return builder.ToString();
    }

    private static string RewriteType(TypeSyntax typeSyntax, ImmutableDictionary<string, string> prettyNames)
    {
        var rewrittenTypeSyntax = new TransportTypeNameRewriter(prettyNames).Visit(typeSyntax);
        return rewrittenTypeSyntax?.NormalizeWhitespace().ToFullString() ?? typeSyntax.NormalizeWhitespace().ToFullString();
    }

    private static string BuildMapExpression(
        TypeSyntax typeSyntax,
        string sourceExpression,
        MappingDirection direction,
        ImmutableDictionary<string, TransportTypeKind> transportKinds)
    {
        if (typeSyntax is NullableTypeSyntax nullableTypeSyntax)
        {
            var nullableInnerExpression = BuildMapExpression(nullableTypeSyntax.ElementType, $"{sourceExpression}.Value", direction, transportKinds);
            return $"{sourceExpression}.HasValue ? {nullableInnerExpression} : null";
        }

        if (typeSyntax is ArrayTypeSyntax arrayTypeSyntax)
        {
            var mappedArrayElement = BuildMapExpression(arrayTypeSyntax.ElementType, "item", direction, transportKinds);
            if (mappedArrayElement == "item")
            {
                return sourceExpression;
            }

            return $"{sourceExpression} is null ? null : {sourceExpression}.Select(item => {mappedArrayElement}).ToArray()";
        }

        if (TryGetSingleTypeArgument(typeSyntax, out var collectionElementType))
        {
            var mappedCollectionElement = BuildMapExpression(collectionElementType, "item", direction, transportKinds);
            if (mappedCollectionElement == "item")
            {
                return $"{sourceExpression} is null ? null : {sourceExpression}.ToList()";
            }

            return $"{sourceExpression} is null ? null : {sourceExpression}.Select(item => {mappedCollectionElement}).ToList()";
        }

        var transportTypeName = GetTransportTypeName(typeSyntax);
        if (transportTypeName is null || !transportKinds.TryGetValue(transportTypeName, out var transportTypeKind))
        {
            return sourceExpression;
        }

        if (transportTypeKind == TransportTypeKind.Enum)
        {
            return $"{sourceExpression}.{(direction == MappingDirection.ToContract ? "ToContract" : "ToTransport")}()";
        }

        return $"{sourceExpression} is null ? null : {sourceExpression}.{(direction == MappingDirection.ToContract ? "ToContract" : "ToTransport")}()";
    }

    private static bool TryGetSingleTypeArgument(TypeSyntax typeSyntax, out TypeSyntax elementType)
    {
        elementType = null!;

        if (typeSyntax is not GenericNameSyntax genericNameSyntax ||
            genericNameSyntax.TypeArgumentList.Arguments.Count != 1)
        {
            return false;
        }

        if (genericNameSyntax.Identifier.ValueText is not ("ICollection" or "IEnumerable" or "IList" or "IReadOnlyCollection" or "IReadOnlyList" or "List"))
        {
            return false;
        }

        elementType = genericNameSyntax.TypeArgumentList.Arguments[0];
        return true;
    }

    private static string? GetTransportTypeName(TypeSyntax typeSyntax) =>
        typeSyntax switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
            GenericNameSyntax genericName => genericName.Identifier.ValueText,
            _ => null
        };

    private static string? TryBuildTransportConstructorExpression(TypeModel typeModel, ImmutableDictionary<string, TransportTypeKind> transportKinds) =>
        typeModel.TransportName switch
        {
            "EndusercontextQueryParams" => $"new EndusercontextQueryParams({BuildMapExpression(GetPropertyType(typeModel, "Party"), "source.Party", MappingDirection.ToTransport, transportKinds)})",
            "ShouldSendNotificationQueryParams" => $"new ShouldSendNotificationQueryParams({BuildMapExpression(GetPropertyType(typeModel, "ConditionType"), "source.ConditionType", MappingDirection.ToTransport, transportKinds)}, {BuildMapExpression(GetPropertyType(typeModel, "ActivityType"), "source.ActivityType", MappingDirection.ToTransport, transportKinds)})",
            _ => null
        };

    private static bool IsConstructorBoundProperty(string transportTypeName, string propertyName) =>
        (transportTypeName, propertyName) switch
        {
            ("EndusercontextQueryParams", "Party") => true,
            ("ShouldSendNotificationQueryParams", "ConditionType") => true,
            ("ShouldSendNotificationQueryParams", "ActivityType") => true,
            _ => false
        };

    private static TypeSyntax GetPropertyType(TypeModel typeModel, string propertyName) =>
        typeModel.Properties.First(property => property.Name.Equals(propertyName, StringComparison.Ordinal)).TypeSyntax;

    private static string? GetJsonPropertyName(PropertyDeclarationSyntax property)
    {
        foreach (var attribute in property.AttributeLists.SelectMany(static list => list.Attributes))
        {
            if (!IsJsonPropertyNameAttribute(attribute.Name))
            {
                continue;
            }

            if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.StringLiteralExpression,
                    Token.ValueText: { } value
                })
            {
                return null;
            }

            return value;
        }

        return null;
    }

    private static bool IsJsonPropertyNameAttribute(NameSyntax attributeName) =>
        attributeName switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText is "JsonPropertyName" or "JsonPropertyNameAttribute",
            QualifiedNameSyntax qualifiedName => IsJsonPropertyNameAttribute(qualifiedName.Right),
            AliasQualifiedNameSyntax aliasQualifiedName => IsJsonPropertyNameAttribute(aliasQualifiedName.Name),
            _ => false
        };

    private sealed class TransportTypeNameRewriter(ImmutableDictionary<string, string> prettyNames) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
            prettyNames.TryGetValue(node.Identifier.ValueText, out var prettyName)
                ? SyntaxFactory.IdentifierName(prettyName).WithTriviaFrom(node)
                : base.VisitIdentifierName(node);
    }

    private sealed class RefitterInput
    {
        public RefitterInput(string path, string? content)
        {
            Path = path;
            Content = content;
        }

        public string Path { get; }

        public string? Content { get; }
    }

    private sealed class TypeModel
    {
        public TypeModel(string transportName, TransportTypeKind kind, ImmutableArray<PropertyModel> properties, ImmutableArray<string> enumMembers)
        {
            TransportName = transportName;
            Kind = kind;
            Properties = properties;
            EnumMembers = enumMembers;
        }

        public string TransportName { get; }

        public TransportTypeKind Kind { get; }

        public ImmutableArray<PropertyModel> Properties { get; }

        public ImmutableArray<string> EnumMembers { get; }
    }

    private sealed class PropertyModel
    {
        public PropertyModel(string name, TypeSyntax typeSyntax, string? jsonPropertyName)
        {
            Name = name;
            TypeSyntax = typeSyntax;
            JsonPropertyName = jsonPropertyName;
        }

        public string Name { get; }

        public TypeSyntax TypeSyntax { get; }

        public string? JsonPropertyName { get; }
    }

    private enum MappingDirection
    {
        ToContract,
        ToTransport
    }

    private enum TransportTypeKind
    {
        Class,
        Enum
    }
}
