namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class AuthorizedPartiesResultDto
{
    public required string Name { get; set; }
    public required string OrganizationNumber { get; set; }
    public string? PersonId { get; set; }
    public string? DateOfBirth { get; set; }
    public required int PartyId { get; set; }
    public required Guid PartyUuid { get; set; }
    public required string Type { get; set; }
    public required bool IsDeleted { get; set; }
    public required bool OnlyHierarchyElementWithNoAccess { get; set; }
    public required List<string> AuthorizedAccessPackages { get; set; }
    public required List<string> AuthorizedResources { get; set; }

    public required List<string> AuthorizedRoles { get; set; }
    public required List<AuthorizedResourceDto> AuthorizedInstances { get; set; }
    public required List<AuthorizedPartiesResultDto> Subunits { get; set; }
}

internal sealed class AuthorizedResourceDto
{
    public string ResourceId { get; set; } = null!;
    public string InstanceId { get; set; } = null!;
}
