namespace Digdir.Library.Dialogporten.E2E.Common;

public sealed record EndUserTokenOverrides(
    string? Ssn = null,
    string? Scopes = null,
    string? TokenOverride = null);

public sealed record ServiceOwnerTokenOverrides(
    string? OrgNumber = null,
    string? OrgName = null,
    string? Scopes = null,
    string? TokenOverride = null);

public sealed record TokenOverrides(
    EndUserTokenOverrides? EndUser = null,
    ServiceOwnerTokenOverrides? ServiceOwner = null);

public interface ITokenOverridesAccessor
{
    TokenOverrides? Current { get; set; }
}

public sealed class TokenOverridesAccessor : ITokenOverridesAccessor
{
    private readonly AsyncLocal<TokenOverrides?> _current = new();

    public TokenOverrides? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

public sealed class TokenOverrideScope : IDisposable
{
    private readonly ITokenOverridesAccessor _accessor;
    private readonly TokenOverrides? _previous;

    public TokenOverrideScope(ITokenOverridesAccessor accessor, TokenOverrides? overrides)
    {
        _accessor = accessor;
        _previous = accessor.Current;
        _accessor.Current = overrides;
    }

    public void Dispose() => _accessor.Current = _previous;
}
