using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyNameRegistry
{
    Task<GetPartyNameResult> GetName(string externalIdWithPrefix, CancellationToken cancellationToken);

    void CacheName(string actorId, string name);
}

[GenerateOneOf]
public sealed partial class GetPartyNameResult : OneOfBase<PartyNameFound, PartyNameNotFound, PartyNameUnsupported, PartyNameUnavailable>;

public sealed record PartyNameFound(string Name);

/// <summary>
/// The registry responded successfully, but has no name for the party. Permanent for persons and organizations,
/// but may be transient for system users, as they are synced to the registry asynchronously.
/// </summary>
public sealed record PartyNameNotFound(string ExternalIdWithPrefix);

/// <summary>
/// The identifier is malformed, or of a type the registry does not support. Retrying will not help.
/// </summary>
public sealed record PartyNameUnsupported(string ExternalIdWithPrefix, string Reason);

/// <summary>
/// The registry could not be reached, or responded with an unexpected result. Retrying may help.
/// </summary>
public sealed record PartyNameUnavailable(string ExternalIdWithPrefix, Exception Exception);
