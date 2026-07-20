namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class AuthorizedPartiesResult
{
    public List<AuthorizedParty> AuthorizedParties { get; set; } = [];

    /// <summary>
    /// - Moves all depth 1 SubParties to the root AuthorizedParties.
    /// - Sets all the SubParties references to null
    /// - Sets the ParentParty of all flattened SubParties to a reference of its parent AuthorizedParty.
    ///
    /// Limitation: Only considers the first level of SubParties
    /// </summary>
    /// <returns>A new instance with all depth 1 SubParties flattened to AuthorizedParties</returns>
    /// <summary>
    /// The number of parties <see cref="Flatten"/> would produce: top-level parties plus their depth-1
    /// sub-parties (the only level <see cref="Flatten"/> promotes). Use this when only the count is needed, to
    /// avoid allocating the flattened list, and to keep the depth-1 rule in one place.
    /// </summary>
    public int FlattenedCount()
    {
        var count = AuthorizedParties.Count;
        foreach (var party in AuthorizedParties)
        {
            count += party.SubParties?.Count ?? 0;
        }

        return count;
    }

    public AuthorizedPartiesResult Flatten()
    {
        var topLevelCount = AuthorizedParties.Count;

        // Preallocate the exact size needed
        var flattenedList = new List<AuthorizedParty>(FlattenedCount());

        for (var i = 0; i < topLevelCount; i++)
        {
            var party = AuthorizedParties[i].Clone(maxDepth: 1);

            flattenedList.Add(party);

            if (!(party.SubParties?.Count > 0)) continue;
            var subCount = party.SubParties.Count;
            for (var j = 0; j < subCount; j++)
            {
                var subParty = party.SubParties[j];
                subParty.SubParties?.Clear();
                subParty.ParentParty = party.Party;
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

    /// <summary>
    /// Deep clone this instance
    /// </summary>
    /// <param name="maxDepth">
    /// The maximum recursion level for SubParties.
    /// If we reach maxDepth, SubParties will be set to an empty list.
    /// If maxDepth is 0, children will be removed
    /// </param>
    /// <returns>A deep copy of this instance.</returns>
    public AuthorizedParty Clone(int maxDepth = 100)
    {
        var nextMaxDepth = maxDepth - 1;
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
                    ? maxDepth <= 0 ? [] : SubParties.Select(x => x.Clone(nextMaxDepth)).ToList()
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
