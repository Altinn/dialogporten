namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common;

/// <summary>
/// Marks an API contract type or property as belonging to an experimental feature that is subject to
/// breaking changes without a major version bump. Every OpenAPI document flags the schema for a marked
/// type - and every property referencing it - with an experimental notice; a marked property is flagged
/// directly, which is how members with no contract type of their own (a token string, a boolean flag)
/// are covered. See ExperimentalFeatureSchemaProcessor in the WebApi project.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property)]
public sealed class ExperimentalFeatureAttribute(string documentationUrl) : Attribute
{
    /// <summary>
    /// URL to the issue or documentation tracking the feature, included in the experimental notice.
    /// </summary>
    public string DocumentationUrl { get; } = documentationUrl;
}

/// <summary>
/// Documentation references for the currently experimental features.
/// </summary>
public static class ExperimentalFeatures
{
    public const string AuthorizationContext = "https://github.com/Altinn/dialogporten/issues/3978";
}
