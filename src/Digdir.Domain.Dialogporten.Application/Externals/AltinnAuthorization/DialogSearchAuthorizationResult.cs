namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogSearchAuthorizationResult
{
    // Resources here are "main" resources, eg. something that represents an entry in the Resource Registry
    // eg. "urn:altinn:resource:some-service" and referred to by "ServiceResource" in DialogEntity
    public Dictionary<string, HashSet<string>> ResourcesByParties { get; init; } = new();

    // These are the dialog IDs that the user has direct access to, which is compiled from AltinnAppInstanceIds
    // and potentially other sources in the future.
    public List<Guid> DialogIds { get; set; } = [];

    // NOTE: AltinnAppInstanceIds currently carries delegated instance references represented as
    // service owner labels (urn:altinn:integration:storage:<partyId>/<instanceId>).
    // See https://github.com/Altinn/dialogporten/issues/3358 for the planned generic handling.
    // Consumers of this result usually should not use these values directly, but instead rely on DialogIds.
    public List<string> AltinnAppInstanceIds { get; set; } = [];

    public bool HasNoAuthorizations =>
        ResourcesByParties.Count == 0
        && DialogIds.Count == 0;
}
