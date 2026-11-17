namespace Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Common;

public abstract class DatabaseCollectionFixture(PostgresDatabaseFixture database) : IAsyncLifetime
{
    protected PostgresDatabaseFixture Database { get; } = database;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public ValueTask InitializeAsync() => Database.ResetState();
}

[CollectionDefinition(nameof(DatabaseCollectionDefinition))]
public sealed class DatabaseCollectionDefinition : ICollectionFixture<PostgresDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
