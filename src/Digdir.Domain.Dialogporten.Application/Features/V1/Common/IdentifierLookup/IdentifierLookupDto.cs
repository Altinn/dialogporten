using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

public abstract class IdentifierLookupDto
{
    public Guid DialogId { get; set; }
    public required string InstanceRef { get; set; }
    public required string Party { get; set; }
    public required IdentifierLookupServiceResourceDto ServiceResource { get; set; }
    public required IdentifierLookupServiceOwnerDto ServiceOwner { get; set; }
}

public sealed class IdentifierLookupServiceResourceDto
{
    public required string Id { get; set; }
    public required bool IsDelegable { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
}

public sealed class IdentifierLookupServiceOwnerDto
{
    public required string OrgNumber { get; set; }
    public required string Code { get; set; }
    public List<LocalizationDto> Name { get; set; } = [];
}

public sealed class EndUserIdentifierLookupDto : IdentifierLookupDto
{
    public required IdentifierLookupAuthorizationEvidenceDto AuthorizationEvidence { get; set; }
}

public sealed class ServiceOwnerIdentifierLookupDto : IdentifierLookupDto
{
    public List<LocalizationDto> Title { get; set; } = [];
    public List<LocalizationDto>? NonSensitiveTitle { get; set; }
}
