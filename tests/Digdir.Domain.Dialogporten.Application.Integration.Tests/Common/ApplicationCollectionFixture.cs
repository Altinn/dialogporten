namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public abstract class ApplicationCollectionFixture(DialogApplication application) : IAsyncLifetime
{
    protected DialogApplication Application { get; } = application;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public ValueTask InitializeAsync() => Application.ResetState();
}
