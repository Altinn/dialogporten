namespace Digdir.Domain.Dialogporten.Application.Externals;

/// <summary>
/// Part of the <see cref="IDialogSearchRepository.GetDialogsAsEndUser"/> contract: thrown when an end-user
/// search exceeds the server-side statement timeout — typically a common free-text term with no narrowing
/// date range (an unbounded GIN scan), or a very broad service/party-driven search. The implementation
/// translates the underlying database cancellation into this exception so the database driver type does not
/// leak across the layer boundary. Surfaced to the caller as a 422 asking them to narrow the search (date
/// range / fewer parties / a service resource).
/// </summary>
public sealed class SearchTermTooBroadException : Exception
{
    private const string DefaultMessage = "The search matched too much to complete in time.";

    public SearchTermTooBroadException()
        : base(DefaultMessage)
    {
    }

    public SearchTermTooBroadException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
