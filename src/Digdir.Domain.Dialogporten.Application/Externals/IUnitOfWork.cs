using System.Data;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflict;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IUnitOfWork
{
    Task<SaveChangesResult> SaveChangesAsync(CancellationToken cancellationToken = default);

    IUnitOfWork EnableConcurrencyCheck<TEntity>(
        TEntity? entity,
        Guid? revision)
        where TEntity : class, IVersionableEntity;

    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified, CancellationToken cancellationToken = default);

    IUnitOfWork DisableAggregateFilter();
    IUnitOfWork DisableVersionableFilter();
    IUnitOfWork DisableUpdatableFilter();
    IUnitOfWork DisableSoftDeletableFilter();
    IUnitOfWork DisableImmutableFilter();
}

[GenerateOneOf]
public sealed partial class SaveChangesResult : OneOfBase<Success, DomainError, ConcurrencyError, Conflict>;
