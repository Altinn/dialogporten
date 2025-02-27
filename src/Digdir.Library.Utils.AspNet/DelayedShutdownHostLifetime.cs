using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;

namespace Digdir.Library.Utils.AspNet;

public sealed class DelayedShutdownHostLifetime : IHostLifetime, IDisposable
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TimeSpan _delay;
    private IEnumerable<IDisposable>? _disposables;

    public DelayedShutdownHostLifetime(IHostApplicationLifetime applicationLifetime, TimeSpan delay)
    {
        _applicationLifetime = applicationLifetime;
        _delay = delay;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _disposables =
        [
            PosixSignalRegistration.Create(PosixSignal.SIGINT, HandleSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandleSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleSignal)
        ];
        return Task.CompletedTask;
    }

    private void HandleSignal(PosixSignalContext ctx)
    {
        ctx.Cancel = true;
        Task.Delay(_delay).ContinueWith(t => _applicationLifetime.StopApplication());
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables ?? [])
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
