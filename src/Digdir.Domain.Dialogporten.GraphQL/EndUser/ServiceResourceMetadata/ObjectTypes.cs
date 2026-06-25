using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.ServiceResourceMetadata;

public sealed class ServiceResourceMetadata
{
    public List<ServiceResourceMetadataItem> Items { get; set; } = [];
}

public sealed class ServiceResourceMetadataItem
{
    public ServiceResourceMetadataServiceResource ServiceResource { get; set; } = null!;
    public List<ServiceResourceMetadataRole> Roles { get; set; } = [];
    public List<ServiceResourceMetadataAccessPackage> AccessPackages { get; set; } = [];
    public ServiceResourceMetadataServiceOwner ServiceOwner { get; set; } = null!;
}

public sealed class ServiceResourceMetadataServiceResource
{
    public string Id { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsDelegable { get; set; }
    public int MinimumAuthenticationLevel { get; set; }
    public List<Localization> Name { get; set; } = [];
    public ServiceResourceMetadataLinks Links { get; set; } = null!;
}

public sealed class ServiceResourceMetadataRole
{
    public string Urn { get; set; } = null!;
    public List<Localization> Name { get; set; } = [];
    public ServiceResourceMetadataLinks Links { get; set; } = null!;
}

public sealed class ServiceResourceMetadataAccessPackage
{
    public string Urn { get; set; } = null!;
    public List<Localization> Name { get; set; } = [];
    public ServiceResourceMetadataLinks Links { get; set; } = null!;
}

public sealed class ServiceResourceMetadataServiceOwner
{
    public string OrgNumber { get; set; } = null!;
    public string Code { get; set; } = null!;
    public List<Localization> Name { get; set; } = [];
}

public sealed class ServiceResourceMetadataLinks
{
    public string Metadata { get; set; } = null!;
}
