namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class AuthorizedPartiesResult
{
    public List<AuthorizedParty> AuthorizedParties { get; set; } = [];

    public Dictionary<string, string> GetNameAsParty()
    {
        return AuthorizedParties
            .DistinctBy(x => x.Party)
            .Select(x => (x.Party, x.Name))
            .ToDictionary();
    }

    public AuthorizedPartiesResult Flatten()
    {
        var topLevelCount = AuthorizedParties.Count;

        var totalCapacity = topLevelCount;
        for (var i = 0; i < topLevelCount; i++)
        {
            var party = AuthorizedParties[i];
            if (party.SubParties != null)
            {
                totalCapacity += party.SubParties.Count;
            }
        }

        // Preallocate the exact size needed
        var flattenedList = new List<AuthorizedParty>(totalCapacity);

        for (var i = 0; i < topLevelCount; i++)
        {
            var party = AuthorizedParties[i].Clone();

            flattenedList.Add(party);

            if (!(party.SubParties?.Count > 0)) continue;
            var subCount = party.SubParties.Count;
            for (var j = 0; j < subCount; j++)
            {
                var subParty = party.SubParties[j];
                flattenedList.Add(subParty);
            }
            party.SubParties = [];
        }

        return new AuthorizedPartiesResult
        {
            AuthorizedParties = flattenedList
        };
    }
}

public sealed class AuthorizedParty
{
    public required string Party { get; init; } = null!;
    public required Guid PartyUuid { get; init; }
    public required int PartyId { get; init; }
    public required string Name { get; init; } = null!;
    public required string? DateOfBirth { get; init; }
    public required AuthorizedPartyType PartyType { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool HasKeyRole { get; init; }
    public required bool IsCurrentEndUser { get; set; }
    public required bool IsMainAdministrator { get; init; }
    public required bool IsAccessManager { get; init; }
    public required bool HasOnlyAccessToSubParties { get; init; }
    public required List<string> AuthorizedResources { get; init; } = [];
    public required List<string> AuthorizedRolesAndAccessPackages { get; init; } = [];
    public required List<AuthorizedResource> AuthorizedInstances { get; init; } = [];

    // Only populated in case of flatten = false
    public List<AuthorizedParty>? SubParties { get; set; }

    // Only populated in case of flatten = true
    public string? ParentParty { get; set; }

    public AuthorizedParty Clone()
    {
        return new AuthorizedParty
        {
            Party = Party,
            PartyUuid = PartyUuid,
            PartyId = PartyId,
            Name = Name,
            DateOfBirth = DateOfBirth,
            PartyType = PartyType,
            IsDeleted = IsDeleted,
            HasKeyRole = HasKeyRole,
            IsCurrentEndUser = IsCurrentEndUser,
            IsMainAdministrator = IsMainAdministrator,
            IsAccessManager = IsAccessManager,
            HasOnlyAccessToSubParties = HasOnlyAccessToSubParties,
            AuthorizedResources = AuthorizedResources.Count > 0
                ? [.. AuthorizedResources]
                : [],
            AuthorizedRolesAndAccessPackages = AuthorizedRolesAndAccessPackages.Count > 0
                ? [.. AuthorizedRolesAndAccessPackages]
                : [],
            AuthorizedInstances = AuthorizedInstances.Count > 0
                ? AuthorizedInstances.Select(x => x.Clone()).ToList()
                : [],
            SubParties = SubParties?.Count > 0
                ? SubParties.Select(x => x.Clone()).ToList()
                : SubParties,
            ParentParty = ParentParty
        };
    }
}

public sealed class AuthorizedResource
{
    public required string ResourceId { get; set; } = null!;
    public required string InstanceId { get; set; } = null!;
    public required string? InstanceRef { get; set; }

    public AuthorizedResource Clone()
    {
        return new AuthorizedResource
        {
            ResourceId = ResourceId,
            InstanceId = InstanceId,
            InstanceRef = InstanceRef
        };
    }
}

public enum AuthorizedPartyType
{
    Person,
    Organization,
    SelfIdentified
}
