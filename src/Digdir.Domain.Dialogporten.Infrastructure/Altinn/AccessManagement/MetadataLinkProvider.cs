using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.AccessManagement;

internal sealed class MetadataLinkProvider : IMetadataLinkProvider
{
    private readonly Uri _baseUri;

    public MetadataLinkProvider(IOptions<InfrastructureSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _baseUri = settings.Value.Altinn.BaseUri;
    }

    public string GetServiceResourceMetadataLink(string resourceId) =>
        new Uri(_baseUri, $"resourceregistry/api/v1/resource/{Uri.EscapeDataString(resourceId)}").ToString();

    public string GetRoleMetadataLink(Guid roleId) =>
        new Uri(_baseUri, $"accessmanagement/api/v1/meta/info/roles/{roleId}").ToString();

    public string GetAccessPackageMetadataLink(string urn) =>
        new Uri(_baseUri, $"accessmanagement/api/v1/meta/info/accesspackages/package/urn/{Uri.EscapeDataString(urn)}").ToString();
}
