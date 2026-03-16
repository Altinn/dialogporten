using System.Data;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Digdir.Library.Entity.EntityFrameworkCore;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Infrastructure;

internal sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private const int ActorNameSaveRetries = 1;
    private const string ActorNameTableName = "ActorName";

    private readonly DialogDbContext _dialogDbContext;
    private readonly ITransactionTime _transactionTime;
    private readonly IDomainContext _domainContext;
    private readonly IApplicationContext _applicationContext;
    private readonly SaveChangesOptions _saveChangesOptions = new();

    private IDbContextTransaction? _transaction;

    private bool _enableConcurrencyCheck;

    public UnitOfWork(DialogDbContext dialogDbContext,
        ITransactionTime transactionTime,
        IDomainContext domainContext,
        IApplicationContext applicationContext)
    {
        _dialogDbContext = dialogDbContext ?? throw new ArgumentNullException(nameof(dialogDbContext));
        _transactionTime = transactionTime ?? throw new ArgumentNullException(nameof(transactionTime));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
    }

    public IUnitOfWork EnableConcurrencyCheck<TEntity>(
        TEntity? entity,
        Guid? revision)
        where TEntity : class, IVersionableEntity
    {
        if (_dialogDbContext.TrySetOriginalRevision(entity, revision))
        {
            _enableConcurrencyCheck = true;
        }

        return this;
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified, CancellationToken cancellationToken = default)
        => _transaction ??= await _dialogDbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    public IUnitOfWork DisableAggregateFilter()
    {
        _saveChangesOptions.EnableAggregateFilter = false;
        return this;
    }

    public IUnitOfWork DisableVersionableFilter()
    {
        _saveChangesOptions.EnableVersionableFilter = false;
        return this;
    }

    public IUnitOfWork DisableUpdatableFilter()
    {
        _saveChangesOptions.EnableUpdatableFilter = false;
        return this;
    }

    public IUnitOfWork DisableSoftDeletableFilter()
    {
        _saveChangesOptions.EnableSoftDeletableFilter = false;
        return this;
    }

    public IUnitOfWork DisableImmutableFilter()
    {
        _saveChangesOptions.EnableImmutableFilter = false;
        return this;
    }

    public async Task<SaveChangesResult> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SaveChangesAsync_Internal(cancellationToken);

            // If it is not a success, rollback the transaction
            await (result.IsT0
                ? CommitTransactionAsync(cancellationToken)
                : RollbackTransactionAsync(cancellationToken));

            return result;
        }
        catch (Exception)
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<SaveChangesResult> SaveChangesAsync_Internal(CancellationToken cancellationToken, int attempt = 0)
    {
        if (!_domainContext.IsValid)
        {
            return new DomainError(_domainContext.Pop());
        }

        if (!_dialogDbContext.ChangeTracker.HasChanges())
        {
            return new Success();
        }

        if (_applicationContext.IsSilentUpdate())
        {
            DisableUpdatableFilter();
        }

        await _dialogDbContext.ChangeTracker.HandleAuditableEntities(
            _transactionTime.Value,
            _saveChangesOptions,
            cancellationToken);

        try
        {
            await _dialogDbContext.SaveChangesAsync(cancellationToken);

            // Interceptors can add domain errors, so check again
            return !_domainContext.IsValid
                ? new DomainError(_domainContext.Pop())
                : new Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Tidligere forsøkte vi å lagre endringene på nytt når _enableConcurrencyCheck var false. Dette kunne føre
            // til korrupte data, fordi tilstanden til aggregatet ikke ble validert på nytt etter at vi forsøkte å
            // «merge» endringene fra kolliderende forespørsler. Siden vi ikke har mulighet til å validere aggregatets
            // tilstand på nytt uten omfattende omskriving, returnerer vi nå i stedet en konfliktfeil til klienten. Når
            // _enableConcurrencyCheck er true, betyr det at klienten eksplisitt har bedt om samtidighetskontroll og
            // derfor forventer å få 412 Precondition Failed ved en samtidighetskonflikt. Når flagget er false, har vi
            // oppdaget en samtidighetskonflikt internt i applikasjonen uten at klienten har bedt om slik kontroll. I
            // disse tilfellene returnerer vi 409 Conflict. I begge tilfeller signaliserer vi en konflikt til klienten,
            // men med ulik HTTP-statuskode avhengig av om klienten har bedt om samtidighetskontroll eller ikke.
            return _enableConcurrencyCheck
                ? new ConcurrencyError()
                : new Conflict("", "The request conflicted with a concurrent operation. Please try again.");
        }
        catch (Exception ex) when (ex is UniqueConstraintException or ReferenceConstraintException &&
                                   ex.InnerException?.Data["Detail"] is string message &&
                                   ex.InnerException.Data["TableName"] is string tableName)
        {
            if (attempt < ActorNameSaveRetries &&
                IsDuplicateActorNameError(tableName, message.Replace('"', '\'')) &&
                await ResolveDuplicateActorNameConflict(cancellationToken))
            {
                return await SaveChangesAsync_Internal(cancellationToken, ++attempt);
            }

            _domainContext.AddError(tableName, message.Replace('"', '\''));
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            return new Conflict("", "The request conflicted with a concurrent operation. Please try again.");
        }

        // Interceptors can add domain errors, so check again
        return !_domainContext.IsValid
            ? new DomainError(_domainContext.Pop())
            : new Success();
    }

    private async Task<bool> ResolveDuplicateActorNameConflict(CancellationToken cancellationToken)
    {
        var addedActorNames = _dialogDbContext.ChangeTracker
            .Entries<ActorName>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .Where(e => e.ActorId is not null && e.Name is not null)
            .ToList();

        if (addedActorNames.Count == 0)
        {
            return false;
        }

        var actorIdNamePairs = addedActorNames
            .Select(x => (x.ActorId, x.Name))
            .Distinct()
            .ToList();
        var actorIds = actorIdNamePairs.Select(x => x.ActorId).Distinct().ToList();
        var names = actorIdNamePairs.Select(x => x.Name).Distinct().ToList();

        var existingActorNames = await _dialogDbContext.Set<ActorName>()
            .Where(x => actorIds.Contains(x.ActorId) && names.Contains(x.Name))
            .ToListAsync(cancellationToken);
        var existingByPair = existingActorNames
            .ToDictionary(x => (x.ActorId, x.Name));

        var resolvedAny = false;
        foreach (var addedActorName in addedActorNames)
        {
            if (!existingByPair.TryGetValue((addedActorName.ActorId, addedActorName.Name), out var existingActorName))
            {
                continue;
            }

            foreach (var actor in addedActorName.ActorEntities)
            {
                actor.ActorNameEntity = existingActorName;
            }

            _dialogDbContext.Entry(addedActorName).State = EntityState.Detached;
            resolvedAny = true;
        }

        return resolvedAny;
    }

    private static bool IsDuplicateActorNameError(string tableName, string message) =>
        tableName == ActorNameTableName
     && message.Contains("(\'ActorId\', \'Name\')=", StringComparison.Ordinal)
     && message.Contains("already exists", StringComparison.Ordinal);

    private static bool IsSerializationFailure(Exception ex)
    {
        const string serializationFailureErrorCode = "40001";

        for (var currentEx = ex; currentEx != null; currentEx = currentEx.InnerException)
        {
            if (currentEx is PostgresException postgresException &&
                postgresException.SqlState == serializationFailureErrorCode)
            {
                return true;
            }
        }

        return false;
    }

    private async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    private async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
    }

    // Although Digdir.Library.Entity.EntityFrameworkCore supports all the options,
    // But we only have use cases for some of them. Therefore,
    // only some of them have setters until the day we actually
    // have a use case for them.
    private sealed class SaveChangesOptions : IEntityOptions
    {
        public bool EnableSoftDeletableFilter { get; set; } = true;
        public bool EnableImmutableFilter { get; set; } = true;
        public bool EnableVersionableFilter { get; set; } = true;
        public bool EnableUpdatableFilter { get; set; } = true;
        public bool EnableCreatableFilter { get; } = true;
        public bool EnableLookupFilter { get; } = true;
        public bool EnableIdentifiableFilter { get; } = true;
        public bool EnableAggregateFilter { get; set; } = true;
    }
}
