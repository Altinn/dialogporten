using System.Security.Claims;

namespace Digdir.Domain.Dialogporten.Application.Externals.Presentation;

public static class AmbientUserPrincipal
{
    // Allows non-HTTP execution paths, such as hosted warmup services, to run application queries
    // that require a user principal without coupling those paths to ASP.NET HttpContext.
    private static readonly AsyncLocal<ClaimsPrincipal?> CurrentPrincipal = new();

    public static ClaimsPrincipal? Current => CurrentPrincipal.Value;

    public static IDisposable Use(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var previousPrincipal = CurrentPrincipal.Value;
        CurrentPrincipal.Value = principal;
        return new ResetDisposable(previousPrincipal);
    }

    private sealed class ResetDisposable : IDisposable
    {
        private readonly ClaimsPrincipal? _previousPrincipal;

        public ResetDisposable(ClaimsPrincipal? previousPrincipal)
        {
            _previousPrincipal = previousPrincipal;
        }

        public void Dispose()
        {
            CurrentPrincipal.Value = _previousPrincipal;
        }
    }
}
