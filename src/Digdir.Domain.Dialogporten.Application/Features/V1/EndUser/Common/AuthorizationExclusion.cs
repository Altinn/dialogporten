using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

internal static class AuthorizationExclusion
{
    /// <summary>
    /// Splits a mapped collection into the elements to return and stubs for the ones to withhold. An element
    /// is withheld when the user is not authorized for it and its authorization context asks for exclusion
    /// rather than disabling.
    ///
    /// Removing rather than blanking in place keeps isAuthorized=false meaning exactly one thing (present but
    /// not usable), and keeps every collection a shape the write API would accept — an in-place redaction
    /// emitted empty titles, urls and endpoints, which the create validators reject.
    /// </summary>
    /// <param name="dtos">The mapped elements, in the same order as <paramref name="entities"/>.</param>
    /// <param name="entities">The entities the elements were mapped from.</param>
    /// <param name="isAuthorized">Reads the decorated authorization result off an element.</param>
    /// <returns>
    /// The elements to keep, and the stubs for the excluded ones — null when nothing was excluded, so the
    /// property is omitted from the response entirely, which is the overwhelmingly common case.
    /// </returns>
    internal static (List<TDto> Retained, List<ExcludedElementDto>? Excluded) PartitionExcluded<TDto, TEntity>(
        List<TDto> dtos,
        IEnumerable<TEntity> entities,
        Func<TDto, bool> isAuthorized)
        where TEntity : IIdentifiableEntity, ICreatableEntity, IAuthorizationContextCarrier
    {
        var partitioned = dtos
            .Zip(entities, (dto, entity) => (Dto: dto, Entity: entity))
            .ToLookup(x => !isAuthorized(x.Dto) && x.Entity.ShouldExcludeWhenUnauthorized());

        var excluded = partitioned[true]
            .Select(x => new ExcludedElementDto { Id = x.Entity.Id, CreatedAt = x.Entity.CreatedAt })
            .ToList();

        return excluded.Count == 0
            ? (dtos, null)
            : (partitioned[false].Select(x => x.Dto).ToList(), excluded);
    }
}
