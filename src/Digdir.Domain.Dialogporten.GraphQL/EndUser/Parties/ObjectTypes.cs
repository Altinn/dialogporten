namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;

public class AuthorizedPartyBase
{
    public string Party { get; init; } = null!;
    public Guid PartyUuid { get; init; }
    public int PartyId { get; init; }
    public string Name { get; init; } = null!;
    public string PartyType { get; init; } = null!;
    public bool IsDeleted { get; init; }
    public bool HasKeyRole { get; init; }
    public bool IsCurrentEndUser { get; init; }
    public bool IsMainAdministrator { get; init; }
    public bool IsAccessManager { get; init; }
}

public sealed class AuthorizedParty : AuthorizedPartyBase
{
    public bool HasOnlyAccessToSubParties { get; init; }
    public List<AuthorizedSubParty>? SubParties { get; init; }
}

public sealed class AuthorizedSubParty : AuthorizedPartyBase;
