namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogSearchAuthorizationResult
{
    // Resources here are "main" resources, eg. something that represents an entry in the Resource Registry
    // eg. "urn:altinn:resource:some-service" and referred to by "ServiceResource" in DialogEntity
    public Dictionary<string, HashSet<string>> ResourcesByParties { get; init; } = new();

    // These are the dialog IDs that the user has direct access to, which is compiled from AltinnAppInstanceIds
    // and potentially other sources in the future.
    public List<Guid> DialogIds { get; set; } = [];

    // AltinnAppInstanceIds are the IDs of Altinn App Instances that the user has access to.
    // Consumers of this result usually should not use these IDs directly, but instead rely on the DialogIds
    public List<string> AltinnAppInstanceIds { get; set; } = [];

    public bool HasNoAuthorizations =>
        ResourcesByParties.Count == 0
        && DialogIds.Count == 0;
}
