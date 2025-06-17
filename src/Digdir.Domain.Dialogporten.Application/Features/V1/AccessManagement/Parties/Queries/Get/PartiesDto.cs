namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Parties.Queries.Get;

public sealed class PartiesDto
{
    public List<AuthorizedPartyDto> AuthorizedParties { get; init; } = [];
}

public sealed class AuthorizedPartyDto
{
    /// <summary>
    /// The party identifier in URN format
    /// </summary>
    /// <example>urn:altinn:organization:identifier-no:912345678</example>
    public string Party { get; init; } = null!;

    /// <summary>
    /// The UUID for the party.
    /// </summary>
    public Guid PartyUuid { get; init; }

    /// <summary>
    /// The name of the party (verbatim from CCR, usually in all caps)
    /// </summary>
    /// <example>CONTOSO REAL ESTATE AS</example>
    public string Name { get; init; } = null!;

    /// <summary>
    /// The type of the party, either "Organization" or "Person".
    /// </summary>
    /// <example>Organization</example>
    public string PartyType { get; init; } = null!;

    /// <summary>
    /// Whether the party is deleted or not
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// Whether the authenticated user has a key role in the party.
    ///
    /// Read more about key roles (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_er/#nøkkelroller
    /// </summary>
    public bool HasKeyRole { get; init; }

    /// <summary>
    /// Whether this party represents the authenticated user.
    /// </summary>
    public bool IsCurrentEndUser { get; init; }

    /// <summary>
    /// Whether the authenticated user is the main administrator of the party
    ///
    /// Read more about main administrator (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#hovedadministrator
    /// </summary>
    public bool IsMainAdministrator { get; init; }

    /// <summary>
    /// Whether the authenticated user is an access manager of the party.
    ///
    /// Read more about access managers (norwegian) at https://docs.altinn.studio/nb/altinn-studio/reference/configuration/authorization/guidelines_authorization/roles_and_rights/roles_altinn/altinn_roles_administration/#tilgangsstrying
    /// </summary>
    public bool IsAccessManager { get; init; }

    /// <summary>
    /// If the authenticated user has only access to sub parties of this party, and not this party itself.
    /// </summary>
    public bool HasOnlyAccessToSubParties { get; init; }

    /// <summary>
    /// The sub parties of this party, if any. The sub party uses the same data model.
    /// </summary>
    public List<AuthorizedPartyDto>? SubParties { get; init; }
}
