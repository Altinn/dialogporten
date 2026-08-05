using AwesomeAssertions;
using Digdir.Library.Utils.AspNet;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

/// <summary>
/// Locks in the URL→template matching used to collapse high-cardinality outgoing
/// HTTP dependency names. The regexes here are what keeps Application Insights'
/// AppDependencies.Name from exploding to one row per resource id.
/// </summary>
public class DependencyTelemetryUrlTemplatesTests
{
    private static (string Template, bool Matched) FirstMatch(string path)
    {
        foreach (var t in DependencyTelemetryUrlTemplates.Defaults)
        {
            if (t.PathPattern.IsMatch(path))
            {
                return (t.Template, true);
            }
        }
        return (string.Empty, false);
    }

    [Theory]
    [InlineData("/resourceregistry/api/v1/resource/aks-prod/policy", "resourceregistry/api/v1/resource/{resourceId}/policy")]
    [InlineData("/resourceregistry/api/v1/resource/acn-migratedcorrespondence-3008-102/policy", "resourceregistry/api/v1/resource/{resourceId}/policy")]
    [InlineData("/apim/resourceregistry/api/v1/resource/aks-prod/policy", "resourceregistry/api/v1/resource/{resourceId}/policy")]
    [InlineData("/resourceregistry/api/v1/resource/aks-prod", "resourceregistry/api/v1/resource/{resourceId}")]
    public void Templates_collapse_per_resource_paths(string path, string expectedTemplate)
    {
        var (template, matched) = FirstMatch(path);
        matched.Should().BeTrue();
        template.Should().Be(expectedTemplate);
    }

    [Theory]
    [InlineData("/resourceregistry/api/v1/resource/updated")]
    [InlineData("/resourceregistry/api/v1/resource/resourcelist")]
    [InlineData("/resourceregistry/api/v1/resource/")]
    [InlineData("/authentication/api/v1/openid/.well-known/openid-configuration")]
    [InlineData("/events/api/v1/events")]
    public void Templates_do_not_match_unrelated_or_sibling_literal_paths(string path)
    {
        var (_, matched) = FirstMatch(path);
        matched.Should().BeFalse();
    }
}
