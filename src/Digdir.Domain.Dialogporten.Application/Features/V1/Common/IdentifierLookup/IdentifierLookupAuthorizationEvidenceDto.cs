using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

public sealed class IdentifierLookupAuthorizationEvidenceDto
{
    public int CurrentAuthenticationLevel { get; set; }
    public bool ViaRole { get; set; }
    public bool ViaAccessPackage { get; set; }
    public bool ViaResourceDelegation { get; set; }
    public bool ViaInstanceDelegation { get; set; }
    public List<IdentifierLookupAuthorizationEvidenceItemDto> Evidence { get; set; } = [];
}

public sealed class IdentifierLookupAuthorizationEvidenceItemDto
{
    public IdentifierLookupGrantType GrantType { get; set; }
    public required string Subject { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
    public LinkDto? Links { get; set; }
}

public enum IdentifierLookupGrantType
{
    Role = 1,
    AccessPackage = 2,
    ResourceDelegation = 3,
    InstanceDelegation = 4
}
