using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Digdir.Library.Entity.EntityFrameworkCore;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneOf.Types;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Digdir.Domain.Dialogporten.Infrastructure;

internal sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
{
    // Fetch the db revision and retry
    // https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=data-annotations#resolving-concurrency-conflicts
    private static readonly AsyncPolicy ConcurrencyRetryPolicy = Policy
        .Handle<DbUpdateConcurrencyException>()
        .WaitAndRetryAsync(
            sleepDurations: Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), 25),
            onRetryAsync: FetchCurrentRevision);

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

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction ??= await _dialogDbContext.Database.BeginTransactionAsync(cancellationToken);

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

    private async Task<SaveChangesResult> SaveChangesAsync_Internal(CancellationToken cancellationToken)
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
            await (_enableConcurrencyCheck
                ? _dialogDbContext.SaveChangesAsync(cancellationToken)
                // Attempt to save changes without a concurrency check
                : ConcurrencyRetryPolicy.ExecuteAsync(_dialogDbContext.SaveChangesAsync, cancellationToken));
        }
        catch (DbUpdateConcurrencyException) when (_enableConcurrencyCheck)
        {
            return new ConcurrencyError();
        }
        catch (UniqueConstraintException ex) when
            (ex.InnerException?.Data["Detail"] is string message &&
             ex.InnerException.Data["TableName"] is string tableName)
        {
            _domainContext.AddError(tableName, message.Replace('"', '\''));
        }
        catch (ReferenceConstraintException)
        {
            // A request triggers loading of exising data, but before it's saved,
            // another request removes it — causing the save attempt to fail.
            // On a retry, the client will get a "proper" error message
            return new ConcurrencyError();
        }

        // Interceptors can add domain errors, so check again
        return !_domainContext.IsValid
            ? new DomainError(_domainContext.Pop())
            : new Success();
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

    private static async Task FetchCurrentRevision(Exception exception, TimeSpan _)
    {
        if (exception is not DbUpdateConcurrencyException concurrencyException)
        {
            return;
        }

        foreach (var entry in concurrencyException.Entries)
        {
            if (entry.Entity is not IVersionableEntity)
            {
                continue;
            }

            var dbValues = await entry.GetDatabaseValuesAsync();
            if (dbValues == null)
            {
                continue;
            }

            var currentRevision = dbValues[nameof(IVersionableEntity.Revision)]!;
            entry.Property(nameof(IVersionableEntity.Revision)).OriginalValue = currentRevision;
        }
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
