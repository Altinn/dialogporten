using Digdir.Domain.Dialogporten.Domain.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.SaveChangesTestInterceptors;

/// <summary>
/// Provokes a race condition (DbConflict) by Inserting all actor-names right before EF saves said actor-names.
/// </summary>
internal sealed class ProvokeActorNameRaceConditionSaveInterceptor : SaveChangesInterceptor, ISaveChangesTestInterceptor
{
    private const string InsertActorNameSql =
        """
        INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
        VALUES (@id, @actorId, @name, @createdAt)
        """;

    private readonly NpgsqlDataSource _dataSource;

    public ProvokeActorNameRaceConditionSaveInterceptor(NpgsqlDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        _dataSource = dataSource;
    }

    public bool HasRaced { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (HasRaced || eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var pendingActorNames = eventData.Context.ChangeTracker
            .Entries<ActorName>()
            .Where(x => x.State is EntityState.Added)
            .Select(x => x.Entity)
            .ToList();

        if (pendingActorNames.Count == 0)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var pendingActorName in pendingActorNames)
        {
            await InsertActorName(pendingActorName.ActorId!, pendingActorName.Name, cancellationToken);
        }

        HasRaced = true;
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task InsertActorName(string actorId, string? name, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertActorNameSql;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("actorId", actorId);
        if (name is null)
        {
            command.Parameters.AddWithValue("name", DBNull.Value);
        }
        else
        {
            command.Parameters.AddWithValue("name", name);
        }
        command.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
