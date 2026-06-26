namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public sealed class DialogSearchAuthorizationResult
{
    // Resources here are "main" resources, eg. something that represents an entry in the Resource Registry
    // eg. "urn:altinn:resource:some-service" and referred to by "ServiceResource" in DialogEntity.
    // The value sets are exposed as IReadOnlySet because ResolveDialogSearchAuthorization may share a single
    // set instance across several parties (role-derived memoization); the interface enforces read-only access,
    // since mutating one party's set would corrupt every other party sharing that instance.
    public Dictionary<string, IReadOnlySet<string>> ResourcesByParties { get; init; } = new();

    // These are the dialog IDs that the user has direct access to
    public List<Guid> DialogIds { get; set; } = [];

    public bool HasNoAuthorizations =>
        ResourcesByParties.Count == 0
        && DialogIds.Count == 0;
}
