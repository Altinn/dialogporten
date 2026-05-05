namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class AuthorizedPartiesResult
{
    private static readonly List<string> EmptyStringList = [];
    private static readonly List<AuthorizedParty> EmptySubPartiesList = [];

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
            var party = AuthorizedParties[i];

            flattenedList.Add(new AuthorizedParty
            {
                Party = party.Party,
                PartyId = party.PartyId,
                DateOfBirth = party.DateOfBirth,
                ParentParty = null,
                AuthorizedResources = party.AuthorizedResources.Count > 0
                    ? [.. party.AuthorizedResources]
                    : EmptyStringList,
                AuthorizedRolesAndAccessPackages = party.AuthorizedRolesAndAccessPackages.Count > 0
                    ? [.. party.AuthorizedRolesAndAccessPackages]
                    : EmptyStringList,
                AuthorizedInstances = party.AuthorizedInstances.Count > 0
                    ? [.. party.AuthorizedInstances]
                    : [],
                SubParties = EmptySubPartiesList
            });

            if (!(party.SubParties?.Count > 0)) continue;
            var subCount = party.SubParties.Count;
            for (var j = 0; j < subCount; j++)
            {
                var subParty = party.SubParties[j];
                flattenedList.Add(new AuthorizedParty
                {
                    Party = subParty.Party,
                    PartyId = subParty.PartyId,
                    ParentParty = party.Party,
                    DateOfBirth = subParty.DateOfBirth,
                    AuthorizedResources = subParty.AuthorizedResources.Count > 0
                        ? [.. subParty.AuthorizedResources]
                        : EmptyStringList,
                    AuthorizedRolesAndAccessPackages = subParty.AuthorizedRolesAndAccessPackages.Count > 0
                        ? [.. subParty.AuthorizedRolesAndAccessPackages]
                        : EmptyStringList,
                    AuthorizedInstances = subParty.AuthorizedInstances.Count > 0
                        ? [.. subParty.AuthorizedInstances]
                        : [],
                    SubParties = EmptySubPartiesList
                });
            }
        }

        return new AuthorizedPartiesResult
        {
            AuthorizedParties = flattenedList
        };
    }
}

public sealed class AuthorizedParty
{
    public string Party { get; init; } = null!;
    public Guid PartyUuid { get; init; }
    public int PartyId { get; init; }
    public string Name { get; init; } = null!;
    public string? DateOfBirth { get; init; }
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
    public string? InstanceRef { get; set; }
}

public enum AuthorizedPartyType
{
    Person,
    Organization,
    SelfIdentified
}
