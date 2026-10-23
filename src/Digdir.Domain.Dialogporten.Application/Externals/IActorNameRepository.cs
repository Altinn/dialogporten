namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    Task<Guid> GetOrCreateActorName(
        string actorId,
        string? actorName,
        CancellationToken cancellationToken
    );
}
