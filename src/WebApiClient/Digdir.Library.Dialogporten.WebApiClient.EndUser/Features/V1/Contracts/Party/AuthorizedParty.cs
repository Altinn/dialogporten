using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Party;

public class Parties
{
    [JsonPropertyName("authorizedParties")]
    public ICollection<AuthorizedParty>? AuthorizedParties { get; set; }
}

public class AuthorizedParty
{
    /// <summary>
    /// The party identifier in URN format
    /// </summary>
    [JsonPropertyName("party")]
    public string Party { get; set; } = null!;

    /// <summary>
    /// The UUID for the party.
    /// </summary>
    [JsonPropertyName("partyUuid")]
    public Guid PartyUuid { get; set; }

    /// <summary>
    /// The numeric identifier for the party.
    /// </summary>
    [JsonPropertyName("partyId")]
    public int PartyId { get; set; }

    /// <summary>
    /// The name of the party (verbatim from CCR, usually in all caps)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The date of birth of the party, if a person.
    /// </summary>
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    /// <summary>
    /// The type of the party, either "Organization" or "Person".
    /// </summary>
    [JsonPropertyName("partyType")]
    public string PartyType { get; set; } = null!;

    /// <summary>
    /// Whether the party is deleted or not
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Whether the authenticated user has a key role in the party.
    /// <br/>
    /// <br/>Read more about key roles (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_er/#nøkkelroller
    /// </summary>
    [JsonPropertyName("hasKeyRole")]
    public bool HasKeyRole { get; set; }

    /// <summary>
    /// Whether this party represents the authenticated user.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; }

    /// <summary>
    /// Whether the authenticated user is the main administrator of the party
    /// <br/>
    /// <br/>Read more about main administrator (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#hovedadministrator
    /// </summary>
    [JsonPropertyName("isMainAdministrator")]
    public bool IsMainAdministrator { get; set; }

    /// <summary>
    /// Whether the authenticated user is an access manager of the party.
    /// <br/>
    /// <br/>Read more about access managers (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#tilgangsstrying
    /// </summary>
    [JsonPropertyName("isAccessManager")]
    public bool IsAccessManager { get; set; }

    /// <summary>
    /// If the authenticated user has only access to sub parties of this party, and not this party itself.
    /// </summary>
    [JsonPropertyName("hasOnlyAccessToSubParties")]
    public bool HasOnlyAccessToSubParties { get; set; }

    /// <summary>
    /// The sub parties of this party, if any. The sub party uses the same data model.
    /// </summary>
    [JsonPropertyName("subParties")]
    public ICollection<AuthorizedParty>? SubParties { get; set; }
}
