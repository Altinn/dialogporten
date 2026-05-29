using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;

public sealed class GetServiceResourceMetadataDto
{
    public List<ServiceResourceMetadataItemDto> Items { get; set; } = [];
}

public sealed class ServiceResourceMetadataItemDto
{
    public required ServiceResourceMetadataServiceResourceDto ServiceResource { get; set; }
    public List<ServiceResourceMetadataRoleDto> Roles { get; set; } = [];
    public List<ServiceResourceMetadataAccessPackageDto> AccessPackages { get; set; } = [];
    public required ServiceResourceMetadataServiceOwnerDto ServiceOwner { get; set; }
}

public sealed class ServiceResourceMetadataServiceResourceDto
{
    public required string Id { get; set; }
    public required bool IsDelegable { get; set; }
    public required int MinimumAuthenticationLevel { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
    public required LinkDto Links { get; set; }
}

public sealed class ServiceResourceMetadataRoleDto
{
    public required string Urn { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
    public required LinkDto Links { get; set; }
}

public sealed class ServiceResourceMetadataAccessPackageDto
{
    public required string Urn { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
    public required LinkDto Links { get; set; }
}

public sealed class ServiceResourceMetadataServiceOwnerDto
{
    public required string OrgNumber { get; set; }
    public required string Code { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
}
