namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    Task<Guid> EnsureActorName(
        string actorId,
        string? actorName,
        CancellationToken cancellationToken
    );
}
