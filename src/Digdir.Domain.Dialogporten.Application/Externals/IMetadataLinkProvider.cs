namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IMetadataLinkProvider
{
    string GetServiceResourceMetadataLink(string resourceId);
    string GetRoleMetadataLink(Guid roleId);
    string GetAccessPackageMetadataLink(string urn);
}
