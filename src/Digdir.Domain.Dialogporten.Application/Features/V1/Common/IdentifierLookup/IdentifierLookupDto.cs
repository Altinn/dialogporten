using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

public abstract class IdentifierLookupDto
{
    public Guid DialogId { get; set; }
    public string InstanceUrn { get; set; } = null!;
    public IdentifierLookupServiceResourceDto ServiceResource { get; set; } = null!;
    public IdentifierLookupServiceOwnerDto ServiceOwner { get; set; } = null!;
}

public sealed class IdentifierLookupServiceResourceDto
{
    public string Id { get; set; } = null!;
    public List<LocalizationDto> Name { get; set; } = [];
}

public sealed class IdentifierLookupServiceOwnerDto
{
    public string OrgNumber { get; set; } = null!;
    public string Code { get; set; } = null!;
    public List<LocalizationDto> Name { get; set; } = [];
}

public sealed class EndUserIdentifierLookupDto : IdentifierLookupDto
{
    public IdentifierLookupAuthorizationEvidenceDto AuthorizationEvidence { get; set; } = null!;
}

public sealed class ServiceOwnerIdentifierLookupDto : IdentifierLookupDto
{
    public List<LocalizationDto> Title { get; set; } = [];
    public List<LocalizationDto>? NonSensitiveTitle { get; set; }
}
