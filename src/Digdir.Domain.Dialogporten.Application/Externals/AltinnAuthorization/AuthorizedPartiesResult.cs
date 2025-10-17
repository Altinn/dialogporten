namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class AuthorizedPartiesResult
{
    public List<AuthorizedParty> AuthorizedParties { get; set; } = [];
}

public sealed class AuthorizedParty
{
    public string Party { get; init; } = null!;
    public Guid PartyUuid { get; init; }
    public int PartyId { get; init; }
    public string Name { get; init; } = null!;
    public AuthorizedPartyType PartyType { get; init; }
    public bool IsDeleted { get; init; }
    public bool HasKeyRole { get; init; }
    public bool IsCurrentEndUser { get; set; }
    public bool IsMainAdministrator { get; init; }
    public bool IsAccessManager { get; init; }
    public bool HasOnlyAccessToSubParties { get; init; }
    public List<string> AuthorizedResources { get; init; } = [];
    public List<string> AuthorizedRolesAndAccessPackages { get; init; } = [];
    public List<AuthorizedResource> AuthorizedInstances { get; init; } = [];

    // Only populated in case of flatten = false
    public List<AuthorizedParty>? SubParties { get; set; }

    // Only populated in case of flatten = true
    public string? ParentParty { get; set; }

}

public sealed class AuthorizedResource
{
    public string ResourceId { get; set; } = null!;
    public string InstanceId { get; set; } = null!;
}

public enum AuthorizedPartyType
{
    Person,
    Organization,
    SelfIdentified
}
